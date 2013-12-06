using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        private DeviceInfo deviceInfo;
        private readonly DeviceFinder deviceFinder = new DeviceFinder();
        private CameraServiceClient10 client;
        private LvStreamProcessor lvProcessor = new LvStreamProcessor();

        private readonly object lockObject = new Object();
        private readonly Downloader downloader = new Downloader();

        private readonly CameraStatus _cameraStatus = new CameraStatus();
        public CameraStatus cameraStatus { get { return _cameraStatus; } }

        private Action<byte[]> LiveViewUpdateListener;
        internal event Action<CameraStatus> UpdateEvent;

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
                if (!available)
                {
                    lvProcessor.CloseConnection();
                }
                else if (!lvProcessor.IsOpen)
                {
                    OpenLiveviewConnection();
                }
            };
            cameraStatus.CurrentShootModeNotifier += (mode) =>
            {
                Debug.WriteLine("Current shoot mode updated: " + mode);
                if (!lvProcessor.IsOpen && cameraStatus.IsAvailable("startLiveview"))
                {
                    OpenLiveviewConnection();
                }
            };

        }

        public bool IsClientReady()
        {
            return client != null && cameraStatus.MethodTypes.Count != 0;
        }

        public void Refresh()
        {
            lvProcessor.CloseConnection();
            watch = new Stopwatch();
            watch.Start();
            deviceInfo = null;
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
                if (lvProcessor != null && lvProcessor.IsOpen)
                {
                    Debug.WriteLine("Close previous LVProcessor");
                    lvProcessor.CloseConnection();
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
            });
        }

        public void SetPostViewImageSize(String size)
        {
            if (client == null)
                return;

            client.SetPostviewImageSize(size, OnError, () => { });
        }

        public void SetSelfTimer(int timer)
        {
            if (client == null)
                return;

            client.SetSelfTimer(timer, OnError, () => { });
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
            lvProcessor.CloseConnection();
        }

        // --------- prepare

        public void RequestSearchDevices(Action Found, Action Timeout)
        {
            deviceFinder.SearchDevices(TIMEOUT, (info) => { OnServerFound(info, Found); }, () => { Timeout.Invoke(); });
        }

        private void OnServerFound(DeviceInfo di, Action Found)
        {
            deviceInfo = di;
            Debug.WriteLine("found device: " + deviceInfo.ModelName);

            if (deviceInfo.Endpoints.ContainsKey("camera"))
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
            observer.Start(cameraStatus, OnDetectDifference, () => { });
        }

        public void StopEventObserver()
        {
            if (observer == null)
            {
                return;
            }
            observer.Stop();
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
        }

        public DeviceInfo GetDeviceInfo()
        {
            return deviceInfo;
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

        public void SetShootMode(string mode)
        {
            if (client == null)
                return;
            client.SetShootMode(mode, OnError, () => { });
        }
    }
}
