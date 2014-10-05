using Kazyx.DeviceDiscovery;
using Kazyx.ImageStream;
using Kazyx.RemoteApi;
using Kazyx.RemoteApi.AvContent;
using Kazyx.RemoteApi.Camera;
using Kazyx.RemoteApi.System;
using Kazyx.WPPMM.DataModel;
using Kazyx.WPPMM.PlaybackMode;
using Kazyx.WPPMM.Utils;
using Microsoft.Phone.Reactive;
using Microsoft.Xna.Framework.Media;
using NtImageProcessor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Windows.Devices.Geolocation;

namespace Kazyx.WPPMM.CameraManager
{
    public class CameraManager
    {
        // singleton instance
        private static CameraManager cameraManager = new CameraManager();

        private const int TIMEOUT = 10;

        private readonly SsdpDiscovery deviceFinder = new SsdpDiscovery();

        public CameraApiClient CameraApi
        {
            private set;
            get;
        }

        public SystemApiClient SystemApi
        {
            private set;
            get;
        }

        public AvContentApiClient AvContentApi
        {
            private set;
            get;
        }

        private readonly StreamProcessor lvProcessor = new StreamProcessor();

        public readonly Downloader Downloader = new Downloader();

        public SonyCameraDeviceInfo CurrentDeviceInfo
        {
            private set;
            get;
        }

        private readonly CameraStatus _cameraStatus = new CameraStatus();
        public CameraStatus Status { get { return _cameraStatus; } }

        internal event Action<CameraStatus> WifiInfoUpdated;
        internal event Action<int> PictureFetchStatusUpdated;
        internal event Action<ImageDLError> PictureFetchFailed;
        internal event Action<Geoposition> PictureFetchSucceed;
        internal event Action<StatusCode> OnRemoteClientError;
        internal event Action OnTakePictureSucceed;
        internal event Action<string> OnExposureModeChanged;
        internal event Action<FocusFramePacket> OnFocusFrameRetrived;

        internal event Action OnDisconnected;
        internal event Action<CameraStatus> OnAfStatusChanged;

        internal event Action<int[], int[], int[]> OnHistogramUpdated;
        internal HistogramCreator histogramCreator;

        internal event Action<ServerVersion> VersionDetected;

        protected void OnVersionDetected()
        {
            if (VersionDetected != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    VersionDetected.Invoke(Status.Version);
                });
            }
        }

        private Stopwatch watch;

        private EventObserver observer;

        private LiveviewData _LiveviewImage = new LiveviewData();
        public LiveviewData LiveviewImage
        {
            get { return _LiveviewImage; }
            private set
            {
                _LiveviewImage = value;
            }
        }

        public Action<String> ShowToast
        {
            get;
            set;
        }

        private void OnShowToast(string message)
        {
            if (ShowToast != null)
            {
                ShowToast(message);
            }
        }

        private bool IsRendering = false;

        internal readonly LocalIntervalShootingManager IntervalManager = new LocalIntervalShootingManager(AppStatus.GetInstance());

        private CameraManager()
        {
            Refresh();

            Status.LiveviewAvailabilityNotifier += (available) =>
            {
                DebugUtil.Log("Liveview Availability changed:" + available);

                if (!available)
                {
                    CloseLiveviewConnection();
                }
                else if (lvProcessor.ConnectionState == ConnectionState.Closed && AppStatus.GetInstance().IsInShootingDisplay)
                {
                    OpenLiveviewConnection();
                }
            };
            Status.CurrentShootModeNotifier += (mode) =>
            {
                DebugUtil.Log("Current shoot mode updated: " + mode);

                if (lvProcessor.ConnectionState == ConnectionState.Closed && Status.IsAvailable("startLiveview") && AppStatus.GetInstance().IsInShootingDisplay)
                {
                    OpenLiveviewConnection();
                }

                if (lvProcessor.ConnectionState != ConnectionState.Closed && mode == ShootModeParam.Audio)
                {
                    CloseLiveviewConnection();
                }
            };
            Status.FocusFrameAvailablityNotifier += FocusFrameAvailablityChanged;
            Status.PropertyChanged += cameraStatus_PropertyChanged;
            lvProcessor.JpegRetrieved += OnJpegRetrieved;
            lvProcessor.Closed += OnLvClosed;
            deviceFinder.SonyCameraDeviceDiscovered += deviceFinder_Discovered;
            deviceFinder.Finished += deviceFinder_Finished;
            lvProcessor.FocusFrameRetrieved += lvProcessor_FocusFrameRetrieved;
            PictureSyncManager.Instance.Fetched += OnPictureFetched;
            PictureSyncManager.Instance.Failed += OnFetchFailed;
            PictureSyncManager.Instance.Message += OnShowToast;
            PictureSyncManager.Instance.Downloader.QueueStatusUpdated += DownloadQueueStatusUpdated;
        }

        private async void FocusFrameAvailablityChanged(bool available)
        {
            DebugUtil.Log("FocusFrame availablity changed: " + available);
            if (available)
            {
                await SetFocusFrameInfo(ApplicationSettings.GetInstance().RequestFocusFrameInfo);
            }
        }

        internal async void FocusFrameSettingChanged(bool setting)
        {
            DebugUtil.Log("FocusFrame setting changed: " + setting);
            if (_cameraStatus.IsAvailable("setLiveviewFrameInfo"))
            {
                await SetFocusFrameInfo(setting);
            }
        }

        internal async Task SetFocusFrameInfo(bool setting)
        {
            try
            {
                if (_cameraStatus.IsSupported("setLiveviewFrameInfo"))
                {
                    DebugUtil.Log("Set liveview frame info true");
                    await CameraApi.SetLiveviewFrameInfo(new FrameInfoSetting() { TransferFrameInfo = setting });
                }
            }
            catch (RemoteApiException e)
            {
                DebugUtil.Log("Failed to call setLiveviewFrameInfo. fallback to previous setting. " + e.StackTrace);
                ApplicationSettings.GetInstance().RequestFocusFrameInfo = !setting;
            }
        }

        private void lvProcessor_FocusFrameRetrieved(object sender, FocusFrameEventArgs e)
        {
            // DebugUtil.Log("[Focus Frame] Retrived " + e.Packet.FocusFrames.Count + " frames.");
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (OnFocusFrameRetrived != null)
                {
                    OnFocusFrameRetrived(e.Packet);
                }
            });
        }

        void cameraStatus_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "FocusStatus":
                case "TouchFocusStatus":
                    if (_cameraStatus.FocusStatus == FocusState.Focused &&
                        _cameraStatus.AfType == CameraStatus.AutoFocusType.Touch &&
                        _cameraStatus.TouchFocusStatus != null &&
                        !_cameraStatus.TouchFocusStatus.Focused)
                    {
                        DebugUtil.Log("Touch AF is cancelled.");
                        _cameraStatus.FocusStatus = FocusState.Released;
                    }
                    if (OnAfStatusChanged != null)
                    {
                        OnAfStatusChanged(_cameraStatus);
                    }
                    break;
                case "ExposureMode":
                    UpdateProgramShiftRange();
                    if (OnExposureModeChanged != null && _cameraStatus != null && _cameraStatus.ExposureMode != null)
                    {
                        OnExposureModeChanged(_cameraStatus.ExposureMode.Current);
                    }
                    break;
                case "PictureUrls":
                    if (_cameraStatus.PictureUrls.Length > 0)
                    {
                        OnResultActTakePicture(_cameraStatus.PictureUrls);
                    }
                    break;
                case "ContShootingResult":
                    if (_cameraStatus.ContShootingResult != null &&
                        _cameraStatus.ContShootingResult.Count > 0)
                    {
                        List<string> urls = new List<string>();
                        foreach (ContShootingResult result in _cameraStatus.ContShootingResult.ToArray())
                        {
                            DebugUtil.Log("Cont shot url: " + result.PostviewUrl);
                            urls.Add(result.PostviewUrl);
                        }
                        OnResultActTakePicture(urls.ToArray());
                    }
                    break;
                case "Storages":
                    if (CurrentDeviceInfo != null && CurrentDeviceInfo.UDN != null)
                    {
                        ThumbnailCacheLoader.INSTANCE.DeleteCache(CurrentDeviceInfo.UDN);
                    }
                    break;
            }
        }

        private async void UpdateProgramShiftRange()
        {
            if (Status == null
                || Status.ProgramShiftRange != null
                || Status.ExposureMode == null
                || Status.ExposureMode.Current != ExposureMode.Program
                || CameraApi == null)
            {
                return;
            }

            if (!Status.IsSupported("setProgramShift"))
            {
                DebugUtil.Log("This device does not support ProgramShift API");
                return;
            }

            DebugUtil.Log("This device supports ProgramShift API");
            try
            {
                var range = await CameraApi.GetSupportedProgramShiftAsync();
                Status.ProgramShiftRange = range;
                DebugUtil.Log("Max: " + range.Max + " Min: " + range.Min);
            }
            catch (RemoteApiException e)
            {
                DebugUtil.Log("Failed to get program shift range: " + e.code);
            }
        }

        void deviceFinder_Finished(object sender, EventArgs e)
        {
            DebugUtil.Log("deviceFinder_Finished");
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (DiscoveryTimeout != null)
                {
                    DebugUtil.Log("Invoke DiscoveryTimeout");
                    DiscoveryTimeout.Invoke();
                }
            });
        }

        void deviceFinder_Discovered(object sender, SonyCameraDeviceEventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                CurrentDeviceInfo = e.SonyCameraDevice;
                DebugUtil.Log("found device: " + CurrentDeviceInfo.ModelName + " - " + CurrentDeviceInfo.UDN);
                if (CurrentDeviceInfo.FriendlyName == "DSC-QX10")
                {
                    Status.DeviceType = DeviceType.DSC_QX10;
                }

                if (CurrentDeviceInfo.Endpoints.ContainsKey("camera"))
                {
                    CameraApi = new CameraApiClient(new Uri(e.SonyCameraDevice.Endpoints["camera"], UriKind.Absolute));
                    DebugUtil.Log(e.SonyCameraDevice.Endpoints["camera"]);

                    GetMethodTypes();
                    Status.isAvailableConnecting = true;

                    observer = new EventObserver(CameraApi);
                }
                if (CurrentDeviceInfo.Endpoints.ContainsKey("system"))
                {
                    SystemApi = new SystemApiClient(new Uri(e.SonyCameraDevice.Endpoints["system"], UriKind.Absolute));
                    DebugUtil.Log(e.SonyCameraDevice.Endpoints["system"]);
                }
                if (CurrentDeviceInfo.Endpoints.ContainsKey("avContent"))
                {
                    AvContentApi = new AvContentApiClient(new Uri(e.SonyCameraDevice.Endpoints["avContent"], UriKind.Absolute));
                    DebugUtil.Log(e.SonyCameraDevice.Endpoints["avContent"]);
                }
                // TODO be careful, device info is updated to the latest found device.

                NotifyWifiInfoUpdated();
            });
        }

        public bool IsClientReady()
        {
            return CameraApi != null && Status.SupportedApis.Count != 0;
        }

        public void Refresh()
        {
            DebugUtil.Log("CameraManager Refresh");
            CloseLiveviewConnection();
            watch = new Stopwatch();
            watch.Start();
            CurrentDeviceInfo = null;

            CameraApi = null;
            SystemApi = null;
            AvContentApi = null;

            Status.Init();
            Status.InitEventParams();
            if (observer != null)
            {
                observer.Stop();
                observer = null;
            }

            if (IntervalManager.ActTakePicture == null)
            {
                IntervalManager.ActTakePicture += this.RequestActTakePicture;
            }

            histogramCreator = null;
            histogramCreator = new HistogramCreator(HistogramCreator.HistogramResolution.Resolution_128);
            histogramCreator.OnHistogramCreated += histogramCreator_OnHistogramCreated;

            if (Downloader.QueueStatusUpdated == null)
            {
                Downloader.QueueStatusUpdated += DownloadQueueStatusUpdated;
            }
        }

        internal void DownloadQueueStatusUpdated(int DownloadItemAmount)
        {
            if (PictureFetchStatusUpdated != null)
            {
                PictureFetchStatusUpdated(DownloadItemAmount);
            }
        }

        void histogramCreator_OnHistogramCreated(int[] arg1, int[] arg2, int[] arg3)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (OnHistogramUpdated != null)
                {
                    OnHistogramUpdated(arg1, arg2, arg3);
                }
            });
        }

        public static CameraManager GetInstance()
        {
            return cameraManager;
        }

        public async Task OperateInitialProcess()
        {
            if (CameraApi == null)
                return;

            ServerAppInfo info = null;
            try
            {
                info = await CameraApi.GetApplicationInfoAsync();
            }
            catch (RemoteApiException e)
            {
                DebugUtil.Log("CameraManager: failed to get application info. - " + e.code);
                OnError(e.code);
                return;
            }

            DebugUtil.Log("Server Info: " + info.Name + " ver " + info.Version);

            try
            {
                Status.Version = new ServerVersion(info.Version);
            }
            catch (ArgumentException e)
            {
                DebugUtil.Log(e.StackTrace);
                DebugUtil.Log("Server version is invalid. Treat this as 2.0.0 device");
                Status.Version = ServerVersion.CreateDefault();
            }

            OnVersionDetected();

            if (AvContentApi != null)
            {
                try
                {
                    if (await CameraApi.GetCameraFunctionAsync() != CameraFunction.RemoteShooting)
                    {
                        if (!await Kazyx.WPPMM.PlaybackMode.PlaybackModeUtility.MoveToShootingModeAsync(CameraApi, Status))
                        {
                            DebugUtil.Log("Failed to move to shooting mode");
                            return;
                        }
                    }
                }
                catch (RemoteApiException e)
                {
                    OnError(e.code);
                }
                catch (TaskCanceledException)
                {
                    DebugUtil.Log("State change await timeout.");
                    OnError(StatusCode.Timeout);
                }
            }
            if (Status.IsSupported("startRecMode"))
            {
                try
                {
                    await CameraApi.StartRecModeAsync();
                    if (Status.IsAvailable("startLiveview"))
                    {
                        OpenLiveviewConnection();
                    }
                }
                catch (RemoteApiException e)
                {
                    OnError(e.code);
                }
            }
            else if (Status.IsAvailable("startLiveview"))
            {
                OpenLiveviewConnection();
            }

            if (SystemApi != null)
            {
                try
                {
                    await SystemApi.SetCurrentTimeAsync(DateTimeOffset.Now); // Should check availability
                }
                catch (RemoteApiException)
                {
                    DebugUtil.Log("Failed to set current time");
                }
            }
        }

        private async void OpenLiveviewConnection(TimeSpan? connectionTimeout = null)
        {
            if (CameraApi == null)
            {
                return;
            }
            if (AppStatus.GetInstance().IsTryingToConnectLiveview)
            {
                DebugUtil.Log("Avoid duplicated liveview opening");
                return;
            }
            AppStatus.GetInstance().IsTryingToConnectLiveview = true;
            try
            {
                var url = await CameraApi.StartLiveviewAsync();

                var uri = new Uri(url);

                if (lvProcessor.ConnectionState == ConnectionState.Closed)
                {
                    var res = await lvProcessor.OpenConnection(uri, connectionTimeout);
                    DebugUtil.Log("Liveview Connection status: " + res);
                    if (!res)
                    {
                        OnError(StatusCode.ServiceUnavailable);
                    }
                }
            }
            catch (RemoteApiException e)
            {
                DebugUtil.Log("Failed to call StartLiveview");
                OnError(e.code);
            }
            catch (UriFormatException e)
            {
                DebugUtil.Log("UriFormatException. Failed to open JPEG stream: " + e.StackTrace);
                OnError(StatusCode.IllegalResponse);
            }
            finally
            {
                AppStatus.GetInstance().IsTryingToConnectLiveview = false;
            }
        }

        private void CloseLiveviewConnection()
        {
            lvProcessor.CloseConnection();
        }

        BitmapImage ImageSource = new BitmapImage()
        {
            CreateOptions = BitmapCreateOptions.None,
        };

        private async void OnLvClosed(object sender, EventArgs e)
        {
            DebugUtil.Log("--- OnLvClosed ---");

            if (AppStatus.GetInstance().IsInShootingDisplay)
            {
                DebugUtil.Log("--- Retry connection for Liveview Stream ---");
                CloseLiveviewConnection();
                await Task.Delay(1000);
                OpenLiveviewConnection();
            }
        }

        private const int FrameSkipRate = 6;
        private int inc = 0;

        private void OnJpegRetrieved(object sender, JpegEventArgs e)
        {
            if (IsRendering)
            {
                return;
            }
            IsRendering = true;
            var size = e.Packet.ImageData.Length;
            Deployment.Current.Dispatcher.BeginInvoke(async () =>
            {
                using (var stream = new MemoryStream(e.Packet.ImageData, 0, size))
                {
                    LiveviewImage.image = null;
                    ImageSource.SetSource(stream);
                    LiveviewImage.image = ImageSource;
                    if (ApplicationSettings.GetInstance().IsHistogramDisplayed)
                    {
                        if (++inc % FrameSkipRate == 0)
                        {
                            inc = 0;
                            await histogramCreator.CreateHistogram(ImageSource);
                        }
                    }
                    IsRendering = false;
                }
            });
        }

        public void RequestCloseLiveView()
        {
            CloseLiveviewConnection();
        }

        private Action DeviceDiscovered;
        private Action DiscoveryTimeout;

        public void RequestSearchDevices(Action Found, Action Timeout)
        {
            DeviceDiscovered = Found;
            DiscoveryTimeout = Timeout;
            deviceFinder.SearchSonyCameraDevices(TimeSpan.FromSeconds(TIMEOUT));
        }

        private async void GetMethodTypes()
        {
            if (CameraApi == null)
            {
                return;
            }

            try
            {
                var methodTypes = await CameraApi.GetMethodTypesAsync(); // Empty string means get all methods in all versions.
                var list = new Dictionary<string, List<string>>();
                foreach (MethodType t in methodTypes)
                {
                    if (list.ContainsKey(t.Name))
                    {
                        list[t.Name].Add(t.Version);
                    }
                    else
                    {
                        var versions = new List<string>();
                        versions.Add(t.Version);
                        list.Add(t.Name, versions);
                    }
                }
                Status.SupportedApis = list;
                if (MethodTypesUpdateNotifer != null)
                {
                    MethodTypesUpdateNotifer.Invoke(); // Notify before call OnFound to update contents of control panel.
                }
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (DeviceDiscovered != null)
                    {
                        DebugUtil.Log("Invoke DeviceDiscovered");
                        DeviceDiscovered.Invoke();
                    }
                });

            }
            catch (RemoteApiException e)
            {
                OnError(e.code);
            }
        }

        public void StartLocalIntervalRec()
        {
            if (IntervalManager != null)
            {
                IntervalManager.Start(ApplicationSettings.GetInstance().IntervalTime);
            }
        }

        public void StopLocalIntervalRec()
        {
            if (IntervalManager != null)
            {
                IntervalManager.Stop();
            }
        }

        public async void RequestActTakePicture()
        {
            if (CameraApi == null)
            {
                return;
            }

            AppStatus.GetInstance().IsTakingPicture = true;
            try
            {
                var urls = await CameraApi.ActTakePictureAsync();
                OnResultActTakePicture(urls.ToArray());
            }
            catch (RemoteApiException e)
            {
                OnActTakePictureError(e.code);
            }
        }

        public Action<Picture> PictureFetched;
        protected void OnPictureFetched(Picture picture)
        {
        }

        private void OnPictureFetched(Picture p, Geoposition pos)
        {
            DebugUtil.Log("download succeed");

            if (PictureFetchSucceed != null)
            {
                PictureFetchSucceed(pos);
            }

            if (PictureFetched != null)
            {
                PictureFetched.Invoke(p);
            }
        }

        private void OnFetchFailed(ImageDLError e)
        {
            if (PictureFetchFailed != null)
            {
                PictureFetchFailed(e);
            }
        }

        public void OnResultActTakePicture(string[] res)
        {
            AppStatus.GetInstance().IsTakingPicture = false;

            if (res == null)
            {
                return;
            }

            if (OnTakePictureSucceed != null)
            {
                OnTakePictureSucceed.Invoke();
            }

            if (!ApplicationSettings.GetInstance().IsPostviewTransferEnabled || IntervalManager.IsRunning)
            {
                return;
            }

            foreach (var s in res)
            {
                try
                {
                    PictureSyncManager.Instance.Enque(new Uri(s));
                }
                catch (UriFormatException)
                {
                    DebugUtil.Log("UriFormatException: " + s);
                }
            }
        }

        public async void OnActTakePictureError(StatusCode err)
        {
            if (err == StatusCode.StillCapturingNotFinished)
            {
                DebugUtil.Log("capturing...");
                try
                {
                    var res = await CameraApi.AwaitTakePictureAsync();
                    OnResultActTakePicture(res.ToArray());
                    return;
                }
                catch (RemoteApiException e)
                {
                    OnActTakePictureError(e.code);
                    return;
                }
            }

            DebugUtil.Log("Error during taking picture: " + err);
            AppStatus.GetInstance().IsTakingPicture = false;
            OnError(err);
        }

        public async void StartMovieRec()
        {
            if (CameraApi == null)
            {
                return;
            }

            try
            {
                await CameraApi.StartMovieRecAsync();
            }
            catch (RemoteApiException e)
            {
                OnError(e.code);
            }
        }

        public async void StopMovieRec()
        {
            if (CameraApi == null)
            {
                return;
            }

            try
            {
                await CameraApi.StopMovieRecAsync();
            }
            catch (RemoteApiException e)
            {
                OnError(e.code);
            }
        }

        public async void StartAudioRec()
        {
            if (CameraApi == null)
            {
                return;
            }

            try
            {
                await CameraApi.StartAudioRecAsync();
            }
            catch (RemoteApiException e)
            {
                OnError(e.code);
            }
        }

        public async void StopAudioRec()
        {
            if (CameraApi == null)
            {
                return;
            }

            try
            {
                await CameraApi.StopAudioRecAsync();
            }
            catch (RemoteApiException e)
            {
                OnError(e.code);
            }
        }

        public async void StartIntervalStillRec()
        {
            if (CameraApi == null)
            {
                return;
            }

            try
            {
                await CameraApi.StartIntervalStillRecAsync();
            }
            catch (RemoteApiException e)
            {
                OnError(e.code);
            }
        }

        public async void StopIntervalStillRec()
        {
            if (CameraApi == null)
            {
                return;
            }

            try
            {
                await CameraApi.StopIntervalStillRecAsync();
            }
            catch (RemoteApiException e)
            {
                OnError(e.code);
            }
        }

        internal async void RequestActZoom(string direction, string movement)
        {
            if (CameraApi == null)
            {
                return;
            }

            try
            {
                await CameraApi.ActZoomAsync(direction, movement);
            }
            catch (RemoteApiException e)
            {
                OnError(e.code);
            }
        }

        public void RunEventObserver()
        {
            if (observer == null)
            {
                return;
            }
            var GetEventVersion = ApiVersion.V1_0;
            if (Status.IsSupported("getEvent", "1.1"))
            {
                GetEventVersion = ApiVersion.V1_1;
            }
            if (Status.IsSupported("getEvent", "1.2"))
            {
                GetEventVersion = ApiVersion.V1_2;
            }
            observer.Start(Status,
                () =>
                {
                    if (this.OnDisconnected != null)
                    {
                        this.OnDisconnected();
                    }
                },
                GetEventVersion);
        }

        public void StopEventObserver()
        {
            if (observer == null)
            {
                return;
            }
            observer.Stop();
        }

        public void RefreshEventObserver()
        {
            if (observer == null)
            {
                return;
            }
            observer.Refresh();
        }

        public void OnError(StatusCode errno)
        {
            DebugUtil.Log("Error: " + errno);

            if (IntervalManager.IsRunning)
            {
                IntervalManager.Stop();
                RefreshEventObserver();
            }

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (OnRemoteClientError != null)
                {
                    OnRemoteClientError(errno);
                }
            });
        }

        internal void NotifyWifiInfoUpdated()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (WifiInfoUpdated != null)
                {
                    WifiInfoUpdated(Status);
                }
            });
        }

        public Action MethodTypesUpdateNotifer;

        public async void RequestTouchAF(double x, double y)
        {
            if (CameraApi == null || x < 0 || x > 100 || y < 0 || y > 100)
            {
                return;
            }

            // set values for SR devices.
            _cameraStatus.AfType = CameraStatus.AutoFocusType.Touch;
            _cameraStatus.FocusStatus = FocusState.InProgress;
            _cameraStatus.TouchFocusStatus.Focused = false;

            try
            {
                var result = await CameraApi.SetAFPositionAsync(x, y);
                if (_cameraStatus != null)
                {
                    if (result.Focused)
                    {
                        // touch AF succeed
                        _cameraStatus.TouchFocusStatus.Focused = true;
                        _cameraStatus.FocusStatus = FocusState.Focused;
                    }
                    else
                    {
                        // failed.
                        _cameraStatus.FocusStatus = FocusState.Failed;
                        Scheduler.Dispatcher.Schedule(() =>
                        {
                            ReleaseFocusStatus();
                        }, TimeSpan.FromSeconds(1));
                    }
                }
            }
            catch (RemoteApiException)
            {
                // in case of AF has failed
                if (_cameraStatus != null)
                {
                    _cameraStatus.FocusStatus = FocusState.Failed;
                    Scheduler.Dispatcher.Schedule(() =>
                    {
                        ReleaseFocusStatus();
                    }, TimeSpan.FromSeconds(1));
                }
            }
        }

        private void ReleaseFocusStatus()
        {
            if (_cameraStatus != null && _cameraStatus.FocusStatus != null && _cameraStatus.FocusStatus == FocusState.Failed)
            {
                _cameraStatus.FocusStatus = FocusState.Released;
            }
        }

        public async void CancelTouchAF()
        {
            if (CameraApi == null)
            {
                return;
            }

            _cameraStatus.AfType = CameraStatus.AutoFocusType.None;

            try
            {
                await CameraApi.CancelTouchAFAsync();
            }
            catch (RemoteApiException) { }
        }

        public bool IsTouchAfAvailable()
        {
            if (_cameraStatus != null)
            {
                return _cameraStatus.IsAvailable("setTouchAFPosition");
            }
            else
            {
                return false;
            }
        }

        public async void RequestHalfPressShutter()
        {
            if (CameraApi != null)
            {
                _cameraStatus.AfType = CameraStatus.AutoFocusType.HalfPress;
                try
                {
                    await CameraApi.ActHalfPressShutterAsync();
                }
                catch (RemoteApiException) { }
            }
        }

        public async void CancelHalfPressShutter()
        {
            if (CameraApi != null)
            {
                _cameraStatus.AfType = CameraStatus.AutoFocusType.None;
                try
                {
                    await CameraApi.CancelHalfPressShutterAsync();
                }
                catch (RemoteApiException) { }
            }
        }

        public async void SetExposureCompensation(int index)
        {
            if (CameraApi == null)
            {
                return;
            }
            try
            {
                await CameraApi.SetEvIndexAsync(index);
            }
            catch (RemoteApiException) { RefreshEventObserver(); }
        }

        public Task SetExposureCompensationAsync(int index)
        {
            if (CameraApi == null)
            {
                throw new InvalidOperationException();
            }
            return CameraApi.SetEvIndexAsync(index);
        }

        public async void SetFNumber(string value)
        {
            DebugUtil.Log("set Fnumber: " + value);
            if (CameraApi == null)
            {
                return;
            }
            try
            {
                await CameraApi.SetFNumberAsync(value);
            }
            catch (RemoteApiException) { RefreshEventObserver(); }
        }

        public async void SetShutterSpeed(string value)
        {
            if (CameraApi == null)
            {
                return;
            }
            try
            {
                await CameraApi.SetShutterSpeedAsync(value);
            }
            catch (RemoteApiException) { RefreshEventObserver(); }
        }

        public async void SetIsoSpeedRate(string value)
        {
            if (CameraApi == null)
            {
                return;
            }
            try
            {
                await CameraApi.SetISOSpeedAsync(value);
            }
            catch (RemoteApiException) { RefreshEventObserver(); }
        }
    }
}
