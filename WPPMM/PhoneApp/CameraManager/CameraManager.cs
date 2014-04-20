using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using WPPMM.DataModel;
using WPPMM.DeviceDiscovery;
using WPPMM.Liveview;
using WPPMM.RemoteApi;
using WPPMM.Resources;

namespace WPPMM.CameraManager
{
    public class CameraManager
    {

        // singleton instance
        private static CameraManager cameraManager = new CameraManager();

        private const int TIMEOUT = 10;

        public DeviceInfo DeviceInfo;

        private readonly DeviceFinder deviceFinder = new DeviceFinder();
        private CameraApiClient apiClient;
        private SystemApiClient sysClient;

        private LvStreamProcessor lvProcessor = new LvStreamProcessor();
        private readonly object lvProcessorLocker = new Object();

        private readonly Downloader downloader = new Downloader();

        private readonly CameraStatus _cameraStatus = new CameraStatus();
        public CameraStatus cameraStatus { get { return _cameraStatus; } }

        internal event Action<CameraStatus> UpdateEvent;
        internal event Action OnDisconnected;
        internal event Action<CameraStatus> OnAfStatusChanged;

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

        internal IntervalShootingManager IntervalManager;

        private CameraManager()
        {
            Refresh();

            cameraStatus.LiveviewAvailabilityNotifier += (available) =>
            {
                Debug.WriteLine("Liveview Availability changed:" + available);

                lock (lvProcessorLocker)
                {
                    if (!available)
                    {
                        CloseLiveviewConnection();
                    }
                    else if (!lvProcessor.IsOpen)
                    {
                        OpenLiveviewConnection();
                    }
                }
            };
            cameraStatus.CurrentShootModeNotifier += (mode) =>
            {
                Debug.WriteLine("Current shoot mode updated: " + mode);

                lock (lvProcessorLocker)
                {
                    if (!lvProcessor.IsOpen && cameraStatus.IsAvailable("startLiveview"))
                    {
                        OpenLiveviewConnection();
                    }

                    if (lvProcessor.IsOpen && mode == ShootModeParam.Audio)
                    {
                        CloseLiveviewConnection();
                    }
                }
            };

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

            IntervalManager = new IntervalShootingManager(AppStatus.GetInstance());

            if (IntervalManager.ActTakePicture == null)
            {
                IntervalManager.ActTakePicture += this.RequestActTakePicture;
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

            var info = await apiClient.GetApplicationInfoAsync();
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
                    await sysClient.SetCurrentTime(DateTimeOffset.Now); // Should check availability
                }
                catch (RemoteApiException)
                {
                    Debug.WriteLine("Failed to set current time");
                }
            }
        }

        private async void OpenLiveviewConnection()
        {
            AppStatus.GetInstance().IsTryingToConnectLiveview = true;
            try
            {
                var url = await apiClient.StartLiveviewAsync();
                lock (lvProcessorLocker)
                {
                    if (lvProcessor.IsOpen)
                    {
                        Debug.WriteLine("Close previous LVProcessor");
                        CloseLiveviewConnection();
                    }
                    lvProcessor = new LvStreamProcessor();
                    try
                    {
                        lvProcessor.OpenConnection(url, OnJpegRetrieved, () =>
                        {
                            AppStatus.GetInstance().IsTryingToConnectLiveview = false;
                        });
                    }
                    catch (InvalidOperationException)
                    {
                        return;
                    }
                }
            }
            catch (RemoteApiException)
            {
                AppStatus.GetInstance().IsTryingToConnectLiveview = false;
            }
        }

        private void CloseLiveviewConnection()
        {
            lock (lvProcessorLocker)
            {
                lvProcessor.CloseConnection();
            }
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
            CreateOptions = BitmapCreateOptions.None
        };

        // callback methods (liveview)
        public void OnJpegRetrieved(byte[] data)
        {
            AppStatus.GetInstance().IsTryingToConnectLiveview = false;
            if (IsRendering)
            {
                return;
            }
            IsRendering = true;
            var size = data.Length;
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                using (var stream = new MemoryStream(data, 0, size))
                {
                    LiveviewImage.image = null;
                    ImageSource.SetSource(stream);
                    LiveviewImage.image = ImageSource;
                    IsRendering = false;
                }
            });
        }

        public void RequestCloseLiveView()
        {
            CloseLiveviewConnection();
        }

        // --------- prepare

        public void RequestSearchDevices(Action Found, Action Timeout)
        {
            deviceFinder.SearchDevices(TIMEOUT,
                (info) => { Deployment.Current.Dispatcher.BeginInvoke(() => OnServerFound(info, Found)); },
                () => { Deployment.Current.Dispatcher.BeginInvoke(() => Timeout.Invoke()); });
        }

        private void OnServerFound(DeviceInfo di, Action Found)
        {
            DeviceInfo = di;
            Debug.WriteLine("found device: " + DeviceInfo.ModelName);

            if (DeviceInfo.Endpoints.ContainsKey("camera"))
            {
                apiClient = new CameraApiClient(di.Endpoints["camera"]);
                Debug.WriteLine(di.Endpoints["camera"]);
                GetMethodTypes(Found);
                cameraStatus.isAvailableConnecting = true;

                observer = new EventObserver(apiClient);
            }
            if (DeviceInfo.Endpoints.ContainsKey("system"))
            {
                sysClient = new SystemApiClient(di.Endpoints["system"]);
                Debug.WriteLine(di.Endpoints["system"]);
            }
            // TODO be careful, device info is updated to the latest found device.

            NoticeUpdate();
        }

        private async void GetMethodTypes(Action found)
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
                if (found != null)
                {
                    found.Invoke();
                }
                NoticeUpdate();
            }
            catch (RemoteApiException e)
            {
                OnError(e.code);
            }
        }

        // -------- take picture

        public void StartIntervalRec()
        {
            if (IntervalManager != null)
            {
                IntervalManager.Start(ApplicationSettings.GetInstance().IntervalTime);

            }
        }

        public void StopIntervalRec()
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

        public void OnActTakePictureError(int err)
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
            observer.Start(cameraStatus, OnDetectDifference,
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

        public void OnDetectDifference(EventMember member)
        {
            switch (member)
            {
                case EventMember.ZoomInfo:
                    Debug.WriteLine("Difference detected: zoom");
                    NoticeUpdate();
                    break;
                case EventMember.CameraStatus:
                    Debug.WriteLine("CameraStatus has changed: " + _cameraStatus.Status);
                    break;
                case EventMember.FocusStatus:
                    if (OnAfStatusChanged != null)
                    {
                        OnAfStatusChanged(_cameraStatus);
                    }
                    break;
                    
                default:
                    //Debug.WriteLine("Difference detected: default");
                    break;
            }
        }

        public void OnError(int errno)
        {
            Debug.WriteLine("Error: " + errno);

            if (IntervalManager.IsRunning)
            {
                IntervalManager.Stop();
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
                UpdateEvent(cameraStatus);
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
            _cameraStatus.FocusStatus = RemoteApi.FocusState.InProgress;

            try
            {
                await apiClient.SetAFPositionAsync(x, y);
            }
            catch (RemoteApiException e)
            {
                // in case of AF has failed
                if (_cameraStatus != null)
                {
                    _cameraStatus.FocusStatus = RemoteApi.FocusState.Failed;
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
    }
}
