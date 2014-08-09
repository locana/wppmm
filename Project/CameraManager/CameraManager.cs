using Kazyx.DeviceDiscovery;
using Kazyx.Liveview;
using Kazyx.RemoteApi;
using Kazyx.WPMMM.CameraManager;
using Kazyx.WPMMM.Resources;
using Kazyx.WPPMM.DataModel;
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

        public ScalarDeviceInfo DeviceInfo;

        private readonly SoDiscovery deviceFinder = new SoDiscovery();

        private CameraApiClient _CameraApi;
        public CameraApiClient CameraApi
        {
            get { return _CameraApi; }
        }

        private SystemApiClient _SystemApi;

        private readonly LvStreamProcessor lvProcessor = new LvStreamProcessor();

        public readonly Downloader Downloader = new Downloader();

        private readonly CameraStatus _cameraStatus = new CameraStatus();
        public CameraStatus cameraStatus { get { return _cameraStatus; } }

        internal event Action<CameraStatus> UpdateEvent;
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
                    VersionDetected.Invoke(cameraStatus.Version);
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

            cameraStatus.LiveviewAvailabilityNotifier += (available) =>
            {
                Debug.WriteLine("Liveview Availability changed:" + available);

                if (!available)
                {
                    CloseLiveviewConnection();
                }
                else if (!lvProcessor.IsProcessing && AppStatus.GetInstance().IsInShootingDisplay)
                {
                    OpenLiveviewConnection();
                }
            };
            cameraStatus.CurrentShootModeNotifier += (mode) =>
            {
                Debug.WriteLine("Current shoot mode updated: " + mode);

                if (!lvProcessor.IsProcessing && cameraStatus.IsAvailable("startLiveview") && AppStatus.GetInstance().IsInShootingDisplay)
                {
                    OpenLiveviewConnection();
                }

                if (lvProcessor.IsProcessing && mode == ShootModeParam.Audio)
                {
                    CloseLiveviewConnection();
                }
            };
            cameraStatus.PropertyChanged += cameraStatus_PropertyChanged;
            lvProcessor.JpegRetrieved += OnJpegRetrieved;
            lvProcessor.Closed += OnLvClosed;
            deviceFinder.ScalarDeviceDiscovered += deviceFinder_Discovered;
            deviceFinder.Finished += deviceFinder_Finished;
            PictureSyncManager.Instance.Fetched += OnPictureFetched;
            PictureSyncManager.Instance.Failed += OnFetchFailed;
            PictureSyncManager.Instance.Message += OnShowToast;
            PictureSyncManager.Instance.Downloader.QueueStatusUpdated += DownloadQueueStatusUpdated;
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
                        Debug.WriteLine("Touch AF is cancelled.");
                        _cameraStatus.FocusStatus = FocusState.Released;
                    }
                    if (OnAfStatusChanged != null)
                    {
                        OnAfStatusChanged(_cameraStatus);
                    }
                    break;
                case "ZoomInfo":
                    NoticeUpdate();
                    break;
                case "ExposureMode":
                    UpdateProgramShiftRange();
                    break;
                case "PictureUrls":
                    if (_cameraStatus.PictureUrls.Length > 0)
                    {
                        OnResultActTakePicture(_cameraStatus.PictureUrls);
                    }
                    break;
            }
        }

        private async void UpdateProgramShiftRange()
        {
            if (cameraStatus == null
                || cameraStatus.ProgramShiftRange != null
                || cameraStatus.ExposureMode == null
                || cameraStatus.ExposureMode.Current != ExposureMode.Program
                || _CameraApi == null)
            {
                return;
            }

            if (!cameraStatus.IsSupported("setProgramShift"))
            {
                Debug.WriteLine("This device does not support ProgramShift API");
                return;
            }

            Debug.WriteLine("This device supports ProgramShift API");
            try
            {
                var range = await _CameraApi.GetSupportedProgramShift();
                cameraStatus.ProgramShiftRange = range;
                Debug.WriteLine("Max: " + range.Max + " Min: " + range.Min);
            }
            catch (RemoteApiException e)
            {
                Debug.WriteLine("Failed to get program shift range: " + e.code);
            }
        }

        void deviceFinder_Finished(object sender, EventArgs e)
        {
            Debug.WriteLine("deviceFinder_Finished");
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (DiscoveryTimeout != null)
                {
                    Debug.WriteLine("Invoke DiscoveryTimeout");
                    DiscoveryTimeout.Invoke();
                }
            });
        }

        void deviceFinder_Discovered(object sender, ScalarDeviceEventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                DeviceInfo = e.ScalarDevice;
                Debug.WriteLine("found device: " + DeviceInfo.ModelName + " - " + DeviceInfo.UDN);
                if (DeviceInfo.FriendlyName == "DSC-QX10")
                {
                    cameraStatus.DeviceType = DeviceType.DSC_QX10;
                }

                if (DeviceInfo.Endpoints.ContainsKey("camera"))
                {
                    _CameraApi = new CameraApiClient(e.ScalarDevice.Endpoints["camera"]);
                    Debug.WriteLine(e.ScalarDevice.Endpoints["camera"]);
                    GetMethodTypes();
                    cameraStatus.isAvailableConnecting = true;

                    observer = new EventObserver(_CameraApi);
                }
                if (DeviceInfo.Endpoints.ContainsKey("system"))
                {
                    _SystemApi = new SystemApiClient(e.ScalarDevice.Endpoints["system"]);
                    Debug.WriteLine(e.ScalarDevice.Endpoints["system"]);
                }
                // TODO be careful, device info is updated to the latest found device.

                NoticeUpdate();
            });
        }

        public bool IsClientReady()
        {
            return _CameraApi != null && cameraStatus.SupportedApis.Count != 0;
        }

        public void Refresh()
        {
            CloseLiveviewConnection();
            watch = new Stopwatch();
            watch.Start();
            DeviceInfo = null;
            _CameraApi = null;
            cameraStatus.Init();
            cameraStatus.InitEventParams();
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
            Debug.WriteLine("Download queue updated: " + DownloadItemAmount);

            if (DownloadItemAmount == 0)
            {
                // ShowToast("Download finished.");
                AppStatus.GetInstance().IsDownloadingImages = false;
            }
            else
            {
                // ShowToast("Downloading images. (" + DownloadItemAmount + " images are scheduled.)");
                AppStatus.GetInstance().IsDownloadingImages = true;
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
            if (_CameraApi == null)
                return;

            ServerAppInfo info = null;
            try
            {
                info = await _CameraApi.GetApplicationInfoAsync();
            }
            catch (RemoteApiException e)
            {
                Debug.WriteLine("CameraManager: failed to get application info. - " + e.code);
                OnError(e.code);
                return;
            }
            Debug.WriteLine("Server Info: " + info.Name + " ver " + info.Version);
            try
            {
                cameraStatus.Version = new ServerVersion(info.Version);
            }
            catch (ArgumentException e)
            {
                Debug.WriteLine(e.StackTrace);
                Debug.WriteLine("Server version is invalid. Treat this as 2.0.0 device");
                cameraStatus.Version = ServerVersion.CreateDefault();
            }

            OnVersionDetected();

            if (cameraStatus.IsSupported("startRecMode"))
            {
                try
                {
                    await _CameraApi.StartRecModeAsync();
                    if (cameraStatus.IsAvailable("startLiveview"))
                    {
                        OpenLiveviewConnection();
                    }
                }
                catch (RemoteApiException e)
                {
                    OnError(e.code);
                }
            }
            else if (cameraStatus.IsAvailable("startLiveview"))
            {
                OpenLiveviewConnection();
            }

            if (_SystemApi != null)
            {
                try
                {
                    await _SystemApi.SetCurrentTimeAsync(DateTimeOffset.Now); // Should check availability
                }
                catch (RemoteApiException)
                {
                    Debug.WriteLine("Failed to set current time");
                }
            }

        }

        private async void OpenLiveviewConnection(TimeSpan? connectionTimeout = null)
        {
            if (_CameraApi == null)
            {
                return;
            }
            if (AppStatus.GetInstance().IsTryingToConnectLiveview)
            {
                Debug.WriteLine("Avoid duplicated liveview opening");
                return;
            }
            AppStatus.GetInstance().IsTryingToConnectLiveview = true;
            try
            {
                var url = await _CameraApi.StartLiveviewAsync();

                var uri = new Uri(url);

                if (!lvProcessor.IsProcessing)
                {
                    var res = await lvProcessor.OpenConnection(uri, connectionTimeout);
                    Debug.WriteLine("Liveview Connection status: " + res);
                    if (!res)
                    {
                        OnError(StatusCode.ServiceUnavailable);
                    }
                }
            }
            catch (RemoteApiException e)
            {
                Debug.WriteLine("Failed to call StartLiveview");
                OnError(e.code);
            }
            catch (UriFormatException e)
            {
                Debug.WriteLine("UriFormatException. Failed to open JPEG stream: " + e.StackTrace);
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
            Debug.WriteLine("--- OnLvClosed ---");

            if (AppStatus.GetInstance().IsInShootingDisplay)
            {
                Debug.WriteLine("--- Retry connection for Liveview Stream ---");
                CloseLiveviewConnection();
                await Task.Delay(1000);
                OpenLiveviewConnection();
            }
        }

        private const int FrameSkipRate = 6;
        private int inc = 0;

        // callback methods (liveview)
        //public void OnJpegRetrieved(byte[] data)
        private void OnJpegRetrieved(object sender, JpegEventArgs e)
        {
            if (IsRendering)
            {
                return;
            }
            IsRendering = true;
            var size = e.JpegData.Length;
            Deployment.Current.Dispatcher.BeginInvoke(async () =>
            {
                using (var stream = new MemoryStream(e.JpegData, 0, size))
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
            deviceFinder.SearchScalarDevices(TimeSpan.FromSeconds(TIMEOUT));
        }

        private async void GetMethodTypes()
        {
            if (_CameraApi == null)
            {
                return;
            }

            try
            {
                var methodTypes = await _CameraApi.GetMethodTypesAsync(); // Empty string means get all methods in all versions.
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
                cameraStatus.SupportedApis = list;
                if (MethodTypesUpdateNotifer != null)
                {
                    MethodTypesUpdateNotifer.Invoke(); // Notify before call OnFound to update contents of control panel.
                }
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (DeviceDiscovered != null)
                    {
                        Debug.WriteLine("Invoke DeviceDiscovered");
                        DeviceDiscovered.Invoke();
                    }
                });
                NoticeUpdate();
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
            if (_CameraApi == null)
            {
                return;
            }

            AppStatus.GetInstance().IsTakingPicture = true;
            try
            {
                var urls = await _CameraApi.ActTakePictureAsync();
                OnResultActTakePicture(urls);
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
            Debug.WriteLine("download succeed");

            if (ApplicationSettings.GetInstance().GeotagEnabled && pos != null)
            {
                OnShowToast(AppResources.Message_ImageDL_Succeed_withGeotag);
            }
            else if (ApplicationSettings.GetInstance().GeotagEnabled)
            {
                MessageBox.Show(AppResources.ErrorMessage_FailedToGetGeoposition);
            }
            else
            {
                OnShowToast(AppResources.Message_ImageDL_Succeed);
            }
            // AppStatus.GetInstance().IsTakingPicture = false;
            NoticeUpdate();

            if (PictureFetched != null)
            {
                PictureFetched.Invoke(p);
            }
        }

        private void OnFetchFailed(ImageDLError e)
        {
            String error = "";
            bool isOriginal = false;
            if (cameraStatus.PostviewSizeInfo != null
                && cameraStatus.PostviewSizeInfo.Current == "Original")
            {
                isOriginal = true;
            }

            switch (e)
            {
                case ImageDLError.Gone:
                    error = AppResources.ErrorMessage_ImageDL_Gone;
                    break;
                case ImageDLError.Network:
                    error = AppResources.ErrorMessage_ImageDL_Network;
                    break;
                case ImageDLError.Saving:
                case ImageDLError.DeviceInternal:
                    if (isOriginal)
                    {
                        error = AppResources.ErrorMessage_ImageDL_SavingOriginal;
                    }
                    else
                    {
                        error = AppResources.ErrorMessage_ImageDL_Saving;
                    }
                    break;
                case ImageDLError.GeotagAlreadyExists:
                    error = AppResources.ErrorMessage_ImageDL_DuplicatedGeotag;
                    break;
                case ImageDLError.GeotagAddition:
                    error = AppResources.ErrorMessage_ImageDL_Geotagging;
                    break;
                case ImageDLError.Unknown:
                case ImageDLError.Argument:
                default:
                    if (isOriginal)
                    {
                        error = AppResources.ErrorMessage_ImageDL_OtherOriginal;
                    }
                    else
                    {
                        error = AppResources.ErrorMessage_ImageDL_Other;
                    }
                    break;
            }
            MessageBox.Show(error, AppResources.MessageCaption_error, MessageBoxButton.OK);
            Debug.WriteLine(error);
            // AppStatus.GetInstance().IsTakingPicture = false;
            NoticeUpdate();
        }

        public void OnResultActTakePicture(String[] res)
        {
            AppStatus.GetInstance().IsTakingPicture = false;
            if (!ApplicationSettings.GetInstance().IsPostviewTransferEnabled || IntervalManager.IsRunning)
            {
                OnShowToast(AppResources.Message_ImageCapture_Succeed);
                NoticeUpdate();
                return;
            }

            foreach (var s in res)
            {
                PictureSyncManager.Instance.Enque(new Uri(s));
            }
        }

        public async void OnActTakePictureError(StatusCode err)
        {
            if (err == StatusCode.StillCapturingNotFinished)
            {
                Debug.WriteLine("capturing...");
                try
                {
                    var res = await _CameraApi.AwaitTakePictureAsync();
                    OnResultActTakePicture(res);
                    return;
                }
                catch (RemoteApiException e)
                {
                    OnActTakePictureError(e.code);
                    return;
                }
            }

            Debug.WriteLine("Error during taking picture: " + err);
            AppStatus.GetInstance().IsTakingPicture = false;
            NoticeUpdate();

            OnError(err);
        }

        public async void StartMovieRec()
        {
            if (_CameraApi == null)
            {
                return;
            }

            try
            {
                await _CameraApi.StartMovieRecAsync();
            }
            catch (RemoteApiException e)
            {
                OnError(e.code);
            }
        }

        public async void StopMovieRec()
        {
            if (_CameraApi == null)
            {
                return;
            }

            try
            {
                await _CameraApi.StopMovieRecAsync();
            }
            catch (RemoteApiException e)
            {
                OnError(e.code);
            }
        }

        public async void StartAudioRec()
        {
            if (_CameraApi == null)
            {
                return;
            }

            try
            {
                await _CameraApi.StartAudioRecAsync();
            }
            catch (RemoteApiException e)
            {
                OnError(e.code);
            }
        }

        public async void StopAudioRec()
        {
            if (_CameraApi == null)
            {
                return;
            }

            try
            {
                await _CameraApi.StopAudioRecAsync();
            }
            catch (RemoteApiException e)
            {
                OnError(e.code);
            }
        }

        public async void StartIntervalStillRec()
        {
            if (_CameraApi == null)
            {
                return;
            }

            try
            {
                await _CameraApi.StartIntervalStillRecAsync();
            }
            catch (RemoteApiException e)
            {
                OnError(e.code);
            }
        }

        public async void StopIntervalStillRec()
        {
            if (_CameraApi == null)
            {
                return;
            }

            try
            {
                await _CameraApi.StopIntervalStillRecAsync();
            }
            catch (RemoteApiException e)
            {
                OnError(e.code);
            }
        }

        // ------- zoom

        internal async void RequestActZoom(String direction, String movement)
        {
            if (_CameraApi == null)
            {
                return;
            }

            try
            {
                await _CameraApi.ActZoomAsync(direction, movement);
            }
            catch (RemoteApiException e)
            {
                OnError(e.code);
            }
        }

        // ------- Event Observer

        public void RunEventObserver()
        {
            if (observer == null)
            {
                return;
            }
            observer.Start(cameraStatus,
                () =>
                {
                    if (this.OnDisconnected != null)
                    {
                        this.OnDisconnected();
                    }
                },
                cameraStatus.IsSupported("getEvent", "1.1") ? ApiVersion.V1_1 : ApiVersion.V1_0); // Use higher version
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
            Debug.WriteLine("Error: " + errno);

            if (IntervalManager.IsRunning)
            {
                IntervalManager.Stop();
                RefreshEventObserver();
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show(AppResources.ErrorMessage_Interval + Environment.NewLine + Environment.NewLine + "Error code: " + errno,
                        AppResources.MessageCaption_error, MessageBoxButton.OK);
                });
                return;
            }

            String err = null;

            switch (errno)
            {
                case StatusCode.Any:
                    err = AppResources.ErrorMessage_fatal;
                    break;
                case StatusCode.Timeout:
                    err = AppResources.ErrorMessage_timeout;
                    break;
                case StatusCode.ShootingFailure:
                    err = AppResources.ErrorMessage_shootingFailure;
                    break;
                case StatusCode.CameraNotReady:
                    err = AppResources.ErrorMessage_cameraNotReady;
                    break;
                case StatusCode.Forbidden:
                    err = AppResources.BuiltInSRNotSupported;
                    break;
                default:
                    err = AppResources.ErrorMessage_fatal;
                    break;
            }

            err = err + Environment.NewLine + Environment.NewLine + "Error code: " + errno;

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                MessageBox.Show(err, AppResources.MessageCaption_error, MessageBoxButton.OK);
            });
        }

        // Notice update to UI classes
        internal void NoticeUpdate()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (UpdateEvent != null)
                {
                    UpdateEvent(cameraStatus);
                }
            });
        }

        public Action MethodTypesUpdateNotifer;

        public async void RequestTouchAF(double x, double y)
        {
            if (_CameraApi == null || x < 0 || x > 100 || y < 0 || y > 100)
            {
                return;
            }

            // set values for SR devices.
            _cameraStatus.AfType = CameraStatus.AutoFocusType.Touch;
            _cameraStatus.FocusStatus = FocusState.InProgress;
            _cameraStatus.TouchFocusStatus.Focused = false;

            try
            {
                var result = await _CameraApi.SetAFPositionAsync(x, y);
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
            if (_CameraApi == null)
            {
                return;
            }

            _cameraStatus.AfType = CameraStatus.AutoFocusType.None;

            try
            {
                await _CameraApi.CancelTouchAFAsync();
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
            if (_CameraApi != null)
            {
                _cameraStatus.AfType = CameraStatus.AutoFocusType.HalfPress;
                try
                {
                    await _CameraApi.ActHalfPressShutterAsync();
                }
                catch (RemoteApiException) { }
            }
        }

        public async void CancelHalfPressShutter()
        {
            if (_CameraApi != null)
            {
                _cameraStatus.AfType = CameraStatus.AutoFocusType.None;
                try
                {
                    await _CameraApi.CancelHalfPressShutterAsync();
                }
                catch (RemoteApiException) { }
            }
        }

        public async void SetExposureCompensation(int index)
        {
            if (_CameraApi == null)
            {
                return;
            }
            try
            {
                await _CameraApi.SetEvIndexAsync(index);
            }
            catch (RemoteApiException) { RefreshEventObserver(); }
        }

        public Task SetExposureCompensationAsync(int index)
        {
            if (_CameraApi == null)
            {
                throw new InvalidOperationException();
            }
            return _CameraApi.SetEvIndexAsync(index);
        }

        public async void SetFNumber(string value)
        {
            Debug.WriteLine("set Fnumber: " + value);
            if (_CameraApi == null)
            {
                return;
            }
            try
            {
                await _CameraApi.SetFNumberAsync(value);
            }
            catch (RemoteApiException) { RefreshEventObserver(); }
        }

        public async void SetShutterSpeed(string value)
        {
            if (_CameraApi == null)
            {
                return;
            }
            try
            {
                await _CameraApi.SetShutterSpeedAsync(value);
            }
            catch (RemoteApiException) { RefreshEventObserver(); }
        }

        public async void SetIsoSpeedRate(string value)
        {
            if (_CameraApi == null)
            {
                return;
            }
            try
            {
                await _CameraApi.SetISOSpeedAsync(value);
            }
            catch (RemoteApiException) { RefreshEventObserver(); }
        }
    }
}
