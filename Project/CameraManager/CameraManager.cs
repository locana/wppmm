using Kazyx.DeviceDiscovery;
using Kazyx.Liveview;
using Kazyx.RemoteApi;
using Kazyx.WPMMM.Resources;
using Kazyx.WPPMM.DataModel;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using NtImageProcessor;

namespace Kazyx.WPPMM.CameraManager
{
    public class CameraManager
    {

        // singleton instance
        private static CameraManager cameraManager = new CameraManager();

        private const int TIMEOUT = 10;

        public ScalarDeviceInfo DeviceInfo;

        private readonly SoDiscovery deviceFinder = new SoDiscovery();
        private CameraApiClient apiClient;
        private SystemApiClient sysClient;

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

        internal LocalIntervalShootingManager IntervalManager;

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
                    if (OnAfStatusChanged != null)
                    {
                        OnAfStatusChanged(_cameraStatus);
                    }
                    break;
                case "ZoomInfo":
                    Debug.WriteLine("Difference detected: zoom");
                    NoticeUpdate();
                    break;
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

                if (DeviceInfo.Endpoints.ContainsKey("camera"))
                {
                    apiClient = new CameraApiClient(e.ScalarDevice.Endpoints["camera"]);
                    Debug.WriteLine(e.ScalarDevice.Endpoints["camera"]);
                    GetMethodTypes();
                    cameraStatus.isAvailableConnecting = true;

                    observer = new EventObserver(apiClient);
                }
                if (DeviceInfo.Endpoints.ContainsKey("system"))
                {
                    sysClient = new SystemApiClient(e.ScalarDevice.Endpoints["system"]);
                    Debug.WriteLine(e.ScalarDevice.Endpoints["system"]);
                }
                // TODO be careful, device info is updated to the latest found device.

                NoticeUpdate();
            });
        }


        public bool IsClientReady()
        {
            return apiClient != null && cameraStatus.SupportedApis.Count != 0;
        }

        public void Refresh()
        {
            CloseLiveviewConnection();
            watch = new Stopwatch();
            watch.Start();
            DeviceInfo = null;
            apiClient = null;
            cameraStatus.Init();
            cameraStatus.InitEventParams();
            if (observer != null)
            {
                observer.Stop();
                observer = null;
            }

            IntervalManager = new LocalIntervalShootingManager(AppStatus.GetInstance());

            if (IntervalManager.ActTakePicture == null)
            {
                IntervalManager.ActTakePicture += this.RequestActTakePicture;
            }

            histogramCreator = null;
            histogramCreator = new HistogramCreator(HistogramCreator.HistogramResolution.Resolution_64, 5000);
            histogramCreator.OnHistogramCreated += histogramCreator_OnHistogramCreated;
        }

        void histogramCreator_OnHistogramCreated(int[] arg1, int[] arg2, int[] arg3)
        {
            if (OnHistogramUpdated != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {  
                    OnHistogramUpdated(arg1, arg2, arg3);
                });
            }
        }

        public static CameraManager GetInstance()
        {
            return cameraManager;
        }

        public async void OperateInitialProcess()
        {
            if (apiClient == null)
                return;

            ServerAppInfo info = null;
            try
            {
                info = await apiClient.GetApplicationInfoAsync();
            }
            catch (RemoteApiException e)
            {
                Debug.WriteLine("CameraManager: failed to get application info. - " + e.code);
                return;
            }
            Debug.WriteLine("Server Info: " + info.name + " ver " + info.version);
            try
            {
                cameraStatus.Version = new ServerVersion(info.version);
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
                    await apiClient.StartRecModeAsync();
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

            if (sysClient != null)
            {
                try
                {
                    await sysClient.SetCurrentTimeAsync(DateTimeOffset.Now); // Should check availability
                }
                catch (RemoteApiException)
                {
                    Debug.WriteLine("Failed to set current time");
                }
            }
        }

        private async void OpenLiveviewConnection(TimeSpan? connectionTimeout = null)
        {
            if (apiClient == null)
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
                var url = await apiClient.StartLiveviewAsync();

                if (!lvProcessor.IsProcessing)
                {
                    var res = await lvProcessor.OpenConnection(url, connectionTimeout);
                    Debug.WriteLine("Liveview Connection status: " + res);
                }
            }
            catch (RemoteApiException)
            {
                Debug.WriteLine("Failed to call StartLiveview");
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

        public Task SetPostViewImageSizeAsync(string size)
        {
            if (apiClient == null)
            {
                throw new InvalidOperationException();
            }
            return apiClient.SetPostviewImageSizeAsync(size);
        }

        public Task SetSelfTimerAsync(int timer)
        {
            if (apiClient == null)
            {
                throw new InvalidOperationException();
            }
            return apiClient.SetSelfTimerAsync(timer);
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
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                using (var stream = new MemoryStream(e.JpegData, 0, size))
                {
                    LiveviewImage.image = null;
                    ImageSource.SetSource(stream);
                    LiveviewImage.image = ImageSource;
                    histogramCreator.CreateHistogram(ImageSource);
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
            if (apiClient == null)
            {
                return;
            }

            try
            {
                var methodTypes = await apiClient.GetMethodTypesAsync(); // Empty string means get all methods in all versions.
                var list = new Dictionary<string, List<string>>();
                foreach (MethodType t in methodTypes)
                {
                    if (list.ContainsKey(t.name))
                    {
                        list[t.name].Add(t.version);
                    }
                    else
                    {
                        var versions = new List<string>();
                        versions.Add(t.version);
                        list.Add(t.name, versions);
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
            if (apiClient == null)
            {
                return;
            }

            AppStatus.GetInstance().IsTakingPicture = true;
            try
            {
                var urls = await apiClient.ActTakePictureAsync();
                OnResultActTakePicture(urls);
            }
            catch (RemoteApiException e)
            {
                OnActTakePictureError(e.code);
            }
        }

        public void OnResultActTakePicture(String[] res)
        {
            if (!ApplicationSettings.GetInstance().IsPostviewTransferEnabled || IntervalManager.IsRunning)
            {
                if (ShowToast != null)
                {
                    ShowToast(AppResources.Message_ImageCapture_Succeed);
                }

                AppStatus.GetInstance().IsTakingPicture = false;
                NoticeUpdate();
                return;
            }

            foreach (String s in res)
            {
                downloader.DownloadImageFile(
                    new Uri(s),
                    (p) =>
                    {
                        Debug.WriteLine("download succeed");
                        if (ShowToast != null)
                        {
                            ShowToast(AppResources.Message_ImageDL_Succeed);
                        }
                        AppStatus.GetInstance().IsTakingPicture = false;
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
                            && cameraStatus.PostviewSizeInfo.current == "Original")
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
                        AppStatus.GetInstance().IsTakingPicture = false;
                        NoticeUpdate();
                    }
                );
            }
        }

        public void OnActTakePictureError(StatusCode err)
        {
            if (err == StatusCode.StillCapturingNotFinished)
            {
                Debug.WriteLine("capturing...");
                return;
            }

            Debug.WriteLine("Error during taking picture: " + err);
            AppStatus.GetInstance().IsTakingPicture = false;
            NoticeUpdate();

            OnError(err);
        }

        public async void StartMovieRec()
        {
            if (apiClient == null)
            {
                return;
            }

            try
            {
                await apiClient.StartMovieRecAsync();
            }
            catch (RemoteApiException e)
            {
                OnError(e.code);
            }
        }

        public async void StopMovieRec()
        {
            if (apiClient == null)
            {
                return;
            }

            try
            {
                await apiClient.StopMovieRecAsync();
            }
            catch (RemoteApiException e)
            {
                OnError(e.code);
            }
        }

        public async void StartAudioRec()
        {
            if (apiClient == null)
            {
                return;
            }

            try
            {
                await apiClient.StartAudioRecAsync();
            }
            catch (RemoteApiException e)
            {
                OnError(e.code);
            }
        }

        public async void StopAudioRec()
        {
            if (apiClient == null)
            {
                return;
            }

            try
            {
                await apiClient.StopAudioRecAsync();
            }
            catch (RemoteApiException e)
            {
                OnError(e.code);
            }
        }

        public async void StartIntervalStillRec()
        {
            if (apiClient == null)
            {
                return;
            }

            try
            {
                await apiClient.StartIntervalStillRecAsync();
            }
            catch (RemoteApiException e)
            {
                OnError(e.code);
            }
        }

        public async void StopIntervalStillRec()
        {
            if (apiClient == null)
            {
                return;
            }

            try
            {
                await apiClient.StopIntervalStillRecAsync();
            }
            catch (RemoteApiException e)
            {
                OnError(e.code);
            }
        }

        // ------- zoom

        internal async void RequestActZoom(String direction, String movement)
        {
            if (apiClient == null)
            {
                return;
            }

            try
            {
                await apiClient.ActZoomAsync(direction, movement);
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
                    MessageBox.Show(AppResources.ErrorMessage_Interval, AppResources.MessageCaption_error, MessageBoxButton.OK);
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

                default:
                    err = AppResources.ErrorMessage_fatal;
                    break;
            }

            err = err + Environment.NewLine + "Error code: " + errno;

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

        public Task SetShootModeAsync(string mode)
        {
            if (apiClient == null)
            {
                throw new InvalidOperationException();
            }

            return apiClient.SetShootModeAsync(mode);
        }

        public async void RequestTouchAF(double x, double y)
        {
            if (apiClient == null || x < 0 || x > 100 || y < 0 || y > 100)
            {
                return;
            }

            _cameraStatus.AfType = CameraStatus.AutoFocusType.Touch;
            _cameraStatus.FocusStatus = FocusState.InProgress;

            try
            {
                await apiClient.SetAFPositionAsync(x, y);
            }
            catch (RemoteApiException e)
            {
                // in case of AF has failed
                if (_cameraStatus != null)
                {
                    _cameraStatus.FocusStatus = FocusState.Failed;
                }
            }
        }

        public async void CancelTouchAF()
        {
            if (apiClient == null)
            {
                return;
            }

            _cameraStatus.AfType = CameraStatus.AutoFocusType.None;

            try
            {
                await apiClient.CancelTouchAFAsync();
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
            if (apiClient != null)
            {
                _cameraStatus.AfType = CameraStatus.AutoFocusType.HalfPress;
                try
                {
                    await apiClient.ActHalfPressShutterAsync();
                }
                catch (RemoteApiException e) { }
            }
        }

        public async void CancelHalfPressShutter()
        {
            if (apiClient != null)
            {
                _cameraStatus.AfType = CameraStatus.AutoFocusType.None;
                try
                {
                    await apiClient.CancelHalfPressShutterAsync();
                }
                catch (RemoteApiException e) { }
            }
        }

        public async void SetExporeMode(String mode)
        {
            if (apiClient != null)
            {
                try
                {
                    await apiClient.SetExposureModeAsync(mode);
                }
                catch (RemoteApiException e)
                {
                    OnError(e.code);
                }
            }
        }


        public Task SetExporeModeAsync(string mode)
        {
            if (apiClient == null)
            {
                throw new InvalidOperationException();
            }

            return apiClient.SetExposureModeAsync(mode);
        }

        public async void SetExposureCompensation(int index)
        {
            if (apiClient == null)
            {
                return;
            }
            try
            {
                await apiClient.SetEvIndexAsync(index);
            }
            catch (RemoteApiException e) { RefreshEventObserver(); }
        }

        public Task SetExposureCompensationAsync(int index)
        {
            if (apiClient == null)
            {
                throw new InvalidOperationException();
            }
            return apiClient.SetEvIndexAsync(index);
        }

        public async void SetFNumber(string value)
        {
            Debug.WriteLine("set Fnumber: " + value);
            if (apiClient == null)
            {
                return;
            }
            try
            {
                await apiClient.SetFNumberAsync(value);
            }
            catch (RemoteApiException e) { RefreshEventObserver(); }
        }

        public async void SetShutterSpeed(string value)
        {
            if (apiClient == null)
            {
                return;
            }
            try
            {
                await apiClient.SetShutterSpeedAsync(value);
            }
            catch (RemoteApiException e) { RefreshEventObserver(); }
        }

        public async void SetIsoSpeedRate(string value)
        {
            if (apiClient == null)
            {
                return;
            }
            try
            {
                await apiClient.SetISOSpeedAsync(value);
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
            if (cameraStatus.FNumber.candidates.Length == 0)
            {
                return;
            }

            int current = 0;
            for (int i = 0; i < cameraStatus.FNumber.candidates.Length; i++)
            {
                if (cameraStatus.FNumber.current == cameraStatus.FNumber.candidates[i])
                {
                    current = i;
                }
            }

            var targetIndex = current + relativeIndex;
            var target = "";

            if (targetIndex < 0)
            {
                target = cameraStatus.FNumber.candidates[0];
            }
            else if (targetIndex >= cameraStatus.FNumber.candidates.Length)
            {
                target = cameraStatus.FNumber.candidates[cameraStatus.FNumber.candidates.Length - 1];
            }
            else
            {
                target = cameraStatus.FNumber.candidates[targetIndex];
            }

            this.SetFNumber(target);
        }

        public void ShiftShutterSpeed(int relativeIndex)
        {
            if (cameraStatus.ShutterSpeed.candidates.Length == 0)
            {
                return;
            }
            int current = 0;
            for (int i = 0; i < cameraStatus.ShutterSpeed.candidates.Length; i++)
            {
                if (cameraStatus.ShutterSpeed.current == cameraStatus.ShutterSpeed.candidates[i])
                {
                    current = i;
                }
            }

            var targetIndex = current + relativeIndex;
            var target = "";

            if (targetIndex < 0)
            {
                target = cameraStatus.ShutterSpeed.candidates[0];
            }
            else if (targetIndex >= cameraStatus.ShutterSpeed.candidates.Length)
            {
                target = cameraStatus.ShutterSpeed.candidates[cameraStatus.ShutterSpeed.candidates.Length - 1];
            }
            else
            {
                target = cameraStatus.ShutterSpeed.candidates[targetIndex];
            }

            this.SetShutterSpeed(target);
        }

        public Task SetBeepModeAsync(string mode)
        {
            if (apiClient == null)
            {
                throw new InvalidOperationException();
            }

            return apiClient.SetBeepModeAsync(mode);
        }

        public Task SetSteadyModeAsync(string mode)
        {
            if (apiClient == null)
            {
                throw new InvalidOperationException();
            }

            return apiClient.SetSteadyModeAsync(mode);
        }

        public Task SetViewAngleAsync(int value)
        {
            if (apiClient == null)
            {
                throw new InvalidOperationException();
            }

            return apiClient.SetViewAngleAsync(value);
        }

        public Task SetMovieQualityAsync(string value)
        {
            if (apiClient == null)
            {
                throw new InvalidOperationException();
            }
            return apiClient.SetMovieQualityAsync(value);
        }

        public async void SetMovieQuality(string value)
        {
            if (apiClient == null)
            {
                return;
            }

            try
            {
                await apiClient.SetMovieQualityAsync(value);
            }
            catch (RemoteApiException e)
            {
                OnError(e.code);
            }
        }
    }
}
