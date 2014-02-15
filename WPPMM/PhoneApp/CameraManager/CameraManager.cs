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
        public const String apiVersion = "1.0";

        public DeviceInfo DeviceInfo;

        private readonly DeviceFinder deviceFinder = new DeviceFinder();
        private CameraServiceClient10 client;

        private LvStreamProcessor lvProcessor = new LvStreamProcessor();
        private readonly object lvProcessorLocker = new Object();

        private readonly Downloader downloader = new Downloader();

        private readonly CameraStatus _cameraStatus = new CameraStatus();
        public CameraStatus cameraStatus { get { return _cameraStatus; } }

        private Action<byte[]> LiveViewUpdateListener;
        internal event Action<CameraStatus> UpdateEvent;
        internal event Action OnDisconnected;

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

                    if (lvProcessor.IsOpen && mode == ApiParams.ShootModeAudio)
                    {
                        CloseLiveviewConnection();
                    }
                }
            };

        }


        public bool IsClientReady()
        {
            return client != null && cameraStatus.MethodTypes.Count != 0;
        }

        public void Refresh()
        {
            CloseLiveviewConnection();
            watch = new Stopwatch();
            watch.Start();
            DeviceInfo = null;
            client = null;
            cameraStatus.Init();
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

        public void OperateInitialProcess()
        {
            if (client == null)
                return;

            if (cameraStatus.MethodTypes.Contains("startRecMode"))
            {
                client.StartRecMode(OnError, () =>
                {
                    if (cameraStatus.IsAvailable("startLiveview"))
                    {
                        OpenLiveviewConnection();
                    }
                });
            }
            else if (cameraStatus.IsAvailable("startLiveview"))
            {
                OpenLiveviewConnection();
            }
        }

        private void OpenLiveviewConnection()
        {
            AppStatus.GetInstance().IsTryingToConnectLiveview = true;
            client.StartLiveview((code) =>
            {
                AppStatus.GetInstance().IsTryingToConnectLiveview = false;
            }, (url) =>
            {
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
            });
        }

        private void CloseLiveviewConnection()
        {
            lock (lvProcessorLocker)
            {
                lvProcessor.CloseConnection();
            }
        }

        public Task<int> SetPostViewImageSizeAsync(string size)
        {
            var taskCS = new TaskCompletionSource<int>();
            if (client == null)
            {
                throw new InvalidOperationException();
            }
            else
            {
                client.SetPostviewImageSize(
                    size,
                    (code) => { taskCS.SetResult(code); },
                    () =>
                    {
                        Debug.WriteLine("SetPostViewImageSizeAsync success");
                        taskCS.SetResult(0);
                    }
                );
            }
            return taskCS.Task;
        }

        public Task<int> SetSelfTimerAsync(int timer)
        {
            var taskCS = new TaskCompletionSource<int>();
            if (client == null)
            {
                throw new InvalidOperationException();
            }
            else
            {
                client.SetSelfTimer(
                    timer,
                    (code) => { taskCS.SetResult(code); },
                    () =>
                    {
                        Debug.WriteLine("SetSelfTimerAsync success");
                        taskCS.SetResult(0);
                    }
                );
            }
            return taskCS.Task;
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
            deviceFinder.SearchDevices(TIMEOUT, (info) => { OnServerFound(info, Found); }, () => { Timeout.Invoke(); });
        }

        private void OnServerFound(DeviceInfo di, Action Found)
        {
            DeviceInfo = di;
            Debug.WriteLine("found device: " + DeviceInfo.ModelName);

            if (DeviceInfo.Endpoints.ContainsKey("camera"))
            {
                client = new CameraServiceClient10(di.Endpoints["camera"]);
                Debug.WriteLine(di.Endpoints["camera"]);
                GetMethodTypes(Found);
                cameraStatus.isAvailableConnecting = true;

                observer = new EventObserver(client);
            }
            // TODO be careful, device info is updated to the latest found device.

            NoticeUpdate();
        }

        private void GetMethodTypes(Action found)
        {
            if (client == null)
                return;

            client.GetMethodTypes(apiVersion, OnError, (methodTypes) =>
            {
                List<String> list = new List<string>();
                foreach (MethodType t in methodTypes)
                {
                    list.Add(t.name);
                }
                cameraStatus.MethodTypes = list;
                if (MethodTypesUpdateNotifer != null)
                {
                    MethodTypesUpdateNotifer.Invoke(); // Notify before call OnFound to update contents of control panel.
                }
                if (found != null)
                {
                    found.Invoke();
                }
                NoticeUpdate();
            });
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

        public void RequestActTakePicture()
        {
            if (client == null)
                return;

            client.ActTakePicture(OnActTakePictureError, OnResultActTakePicture);
            AppStatus.GetInstance().IsTakingPicture = true;
        }

        public void OnResultActTakePicture(String[] res)
        {
            if (!ApplicationSettings.GetInstance().IsPostviewTransferEnabled)
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
                    delegate(Picture p)
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
                    delegate(ImageDLError e)
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
                        MessageBox.Show(error);
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

        public void StartMovieRec()
        {
            if (client == null)
                return;

            client.StartMovieRec(OnError, () => { });
        }

        public void StopMovieRec()
        {
            if (client == null)
                return;

            client.StopMovieRec(OnError, (url) => { });
        }

        public void StartAudioRec()
        {
            if (client == null)
                return;

            client.StartAudioRec(OnError, () => { });
        }

        public void StopAudioRec()
        {
            if (client == null)
                return;

            client.StopAudioRec(OnError, () => { });
        }

        // ------- zoom

        internal void RequestActZoom(String direction, String movement)
        {
            if (client == null)
                return;

            client.ActZoom(direction, movement, OnError, () => { });
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
                });
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

                default:
                    //Debug.WriteLine("Difference detected: default");
                    break;
            }
        }

        public void OnError(int errno)
        {
            Debug.WriteLine("Error: " + errno.ToString());

            if (IntervalManager.IsRunning)
            {
                IntervalManager.Stop();
                MessageBox.Show(AppResources.ErrorMessage_Interval);
            }

            String err = "error";

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

            MessageBox.Show(err);
        }

        // register EE screen update method
        public void SetLiveViewUpdateListener(Action<byte[]> action)
        {
            LiveViewUpdateListener = action;
        }

        // Notice update to UI classes
        internal void NoticeUpdate()
        {
            UpdateEvent(cameraStatus);
        }

        public Action<Picture> PictureNotifier;

        public Action MethodTypesUpdateNotifer;

        public Task<int> SetShootModeAsync(string mode)
        {
            var taskCS = new TaskCompletionSource<int>();
            if (client == null)
            {
                throw new InvalidOperationException();
            }
            else
            {
                client.SetShootMode(
                    mode,
                    (code) => { taskCS.SetResult(code); },
                    () =>
                    {
                        Debug.WriteLine("SetShootModeAsync success");
                        taskCS.SetResult(0);
                    }
                );
            }
            return taskCS.Task;
        }
    }
}
