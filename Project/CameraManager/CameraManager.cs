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

        private readonly Downloader downloader = new Downloader();

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
                else if (!lvProcessor.IsProcessing)
                {
                    OpenLiveviewConnection();
                }
            };
            cameraStatus.CurrentShootModeNotifier += (mode) =>
            {
                Debug.WriteLine("Current shoot mode updated: " + mode);

                if (!lvProcessor.IsProcessing && cameraStatus.IsAvailable("startLiveview"))
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
        }

        void cameraStatus_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "FocusStatus":
                case "TouchFocusStatus":
                    if (OnAfStatusChanged != null)
                    {
                        OnAfStatusChanged(_cameraStatus);
                    }
                    break;
                case "ZoomInfo":
                    Debug.WriteLine("Difference detected: zoom");
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

            if (downloader.QueueStatusUpdated == null)
            {
                downloader.QueueStatusUpdated += DownloadQueueStatusUpdated;
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

        public async void OperateInitialProcess()
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

                if (!lvProcessor.IsProcessing)
                {
                    var res = await lvProcessor.OpenConnection(url, connectionTimeout);
                    Debug.WriteLine("Liveview Connection status: " + res);
                }
            }
            catch (RemoteApiException e)
            {
                Debug.WriteLine("Failed to call StartLiveview");
                OnError(e.code);
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

        // --------- prepare

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

        // -------- take picture

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

        public async void OnResultActTakePicture(String[] res)
        {
            AppStatus.GetInstance().IsTakingPicture = false;
            if (!ApplicationSettings.GetInstance().IsPostviewTransferEnabled || IntervalManager.IsRunning)
            {
                if (ShowToast != null)
                {
                    ShowToast(AppResources.Message_ImageCapture_Succeed);
                }                
                NoticeUpdate();
                return;
            }

            Deployment.Current.Dispatcher.BeginInvoke(async () =>
            {
                Geoposition pos = null;
                if (ApplicationSettings.GetInstance().GeotagEnabled)
                {
                    if (GeopositionManager.GetInstance().LatestPosition == null)
                    {
                        // takes some more time
                        ShowToast(AppResources.WaitingGeoposition);
                    }
                    pos = await GeopositionManager.GetInstance().AcquireGeoPosition();
                }

                foreach (String s in res)
                {
                    downloader.AddDownloadQueue(
                        new Uri(s),
                        pos,
                        (p) =>
                        {
                            Debug.WriteLine("download succeed");
                            if (ShowToast != null)
                            {
                                if (ApplicationSettings.GetInstance().GeotagEnabled && pos != null)
                                {
                                    ShowToast(AppResources.Message_ImageDL_Succeed_withGeotag);
                                }
                                else if (ApplicationSettings.GetInstance().GeotagEnabled)
                                {
                                    MessageBox.Show(AppResources.ErrorMessage_FailedToGetGeoposition);
                                }
                                else
                                {
                                    ShowToast(AppResources.Message_ImageDL_Succeed);
                                }
                            }
                            // AppStatus.GetInstance().IsTakingPicture = false;
                            NoticeUpdate();
                            if (PictureNotifier != null)
                            {
                                PictureNotifier.Invoke(p);
                            }
                        },
                        (e) =>
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
                    );
                }
            });
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

        public Action<Picture> PictureNotifier;

        public Action MethodTypesUpdateNotifer;

        public async void RequestTouchAF(double x, double y)
        {
            if (_CameraApi == null || x < 0 || x > 100 || y < 0 || y > 100)
            {
                return;
            }

            _cameraStatus.AfType = CameraStatus.AutoFocusType.Touch;
            _cameraStatus.FocusStatus = FocusState.InProgress;
            _cameraStatus.TouchFocusStatus.Focused = false;

            try
            {
                var result = await _CameraApi.SetAFPositionAsync(x, y);
                if (!result.Focused && _cameraStatus != null)
                {
                    _cameraStatus.FocusStatus = FocusState.Failed;
                    Scheduler.Dispatcher.Schedule(() =>
                    {
                        ReleaseFocusStatus();
                    }, TimeSpan.FromSeconds(1));
                }
            }
            catch (RemoteApiException e)
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
            catch (RemoteApiException e) { }
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
                catch (RemoteApiException e) { }
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
                catch (RemoteApiException e) { }
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
            catch (RemoteApiException e) { RefreshEventObserver(); }
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
            catch (RemoteApiException e) { RefreshEventObserver(); }
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
            catch (RemoteApiException e) { RefreshEventObserver(); }
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
            catch (RemoteApiException e) { RefreshEventObserver(); }
        }

        public void ShiftEv(int relativeIndex)
        {
            var target = cameraStatus.EvInfo.CurrentIndex + relativeIndex;
            if (target < cameraStatus.EvInfo.Candidate.MinIndex)
            {
                target = cameraStatus.EvInfo.Candidate.MinIndex;
            }
            else if (target > cameraStatus.EvInfo.Candidate.MaxIndex)
            {
                target = cameraStatus.EvInfo.Candidate.MaxIndex;
            }

            this.SetExposureCompensation(target);
        }

        public void ShiftFNumber(int relativeIndex)
        {
            if (cameraStatus.FNumber.Candidates.Length == 0)
            {
                return;
            }

            int current = 0;
            for (int i = 0; i < cameraStatus.FNumber.Candidates.Length; i++)
            {
                if (cameraStatus.FNumber.Current == cameraStatus.FNumber.Candidates[i])
                {
                    current = i;
                }
            }

            var targetIndex = current + relativeIndex;
            var target = "";

            if (targetIndex < 0)
            {
                target = cameraStatus.FNumber.Candidates[0];
            }
            else if (targetIndex >= cameraStatus.FNumber.Candidates.Length)
            {
                target = cameraStatus.FNumber.Candidates[cameraStatus.FNumber.Candidates.Length - 1];
            }
            else
            {
                target = cameraStatus.FNumber.Candidates[targetIndex];
            }

            this.SetFNumber(target);
        }

        public void ShiftShutterSpeed(int relativeIndex)
        {
            if (cameraStatus.ShutterSpeed.Candidates.Length == 0)
            {
                return;
            }
            int current = 0;
            for (int i = 0; i < cameraStatus.ShutterSpeed.Candidates.Length; i++)
            {
                if (cameraStatus.ShutterSpeed.Current == cameraStatus.ShutterSpeed.Candidates[i])
                {
                    current = i;
                }
            }

            var targetIndex = current + relativeIndex;
            var target = "";

            if (targetIndex < 0)
            {
                target = cameraStatus.ShutterSpeed.Candidates[0];
            }
            else if (targetIndex >= cameraStatus.ShutterSpeed.Candidates.Length)
            {
                target = cameraStatus.ShutterSpeed.Candidates[cameraStatus.ShutterSpeed.Candidates.Length - 1];
            }
            else
            {
                target = cameraStatus.ShutterSpeed.Candidates[targetIndex];
            }

            this.SetShutterSpeed(target);
        }
    }
}
