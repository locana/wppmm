using Microsoft.Phone.Reactive;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
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

        private String liveViewUrl = null;
        private readonly object lockObject = new Object();
        private readonly Downloader downloader = new Downloader();

        private readonly Status _cameraStatus = new Status();
        public Status cameraStatus { get { return _cameraStatus; } }

        private Action<byte[]> LiveViewUpdateListener;
        internal event Action<Status> UpdateEvent;

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

        public Action<String> StartToastAppearance
        {
            get;
            set;
        }

        public Action StartToastDisappearance
        {
            get;
            set;
        }

        private bool IsRendering = false;

        private CameraManager()
        {
            Refresh();
        }

        public bool IsClientReady()
        {
            return client != null && cameraStatus.MethodTypes.Count != 0;
        }

        public void Refresh()
        {
            lvProcessor.CloseConnection();
            liveViewUrl = null;
            watch = new Stopwatch();
            watch.Start();
            deviceInfo = null;
            if (observer != null)
            {
                observer.Stop();
                observer = null;
            }
        }

        public static CameraManager GetInstance()
        {
            return cameraManager;
        }

        public void StartLiveView()
        {
            if (cameraStatus.MethodTypes.Contains("startRecMode"))
            {
                RequestStartRecmode();
            }
            else
            {
                RequestStartLiveView();
            }
        }

        // request and callback
        public void RequestStartRecmode()
        {
            if (client == null)
                return;

            client.StartRecMode(OnError, () => { RequestStartLiveView(); });
        }

        // live view
        public void RequestStartLiveView()
        {
            startLiveview(OnError, (res) =>
            {
                // finally, url for liveView has get
                Debug.WriteLine("OnStartLiveViewResult: " + res);
                liveViewUrl = res;
                cameraStatus.isConnected = true;
                NoticeUpdate();
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

        public void startLiveview(Action<int> error, Action<string> result)
        {
            if (client == null)
                return;

            client.StartLiveview(error, result);
        }

        public bool ConnectLiveView()
        {
            Debug.WriteLine("Connect liveview");
            if (liveViewUrl == null)
            {
                Debug.WriteLine("error: liveViewUrl is null");
                return false;
            }

            if (lvProcessor != null && lvProcessor.IsOpen)
            {
                Debug.WriteLine("Close previous LVProcessor");
                lvProcessor.CloseConnection();
            }

            lvProcessor = new LvStreamProcessor();
            try
            {
                lvProcessor.OpenConnection(liveViewUrl, OnJpegRetrieved, OnLiveViewClosed);
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            return true;
        }

        BitmapImage ImageSource = new BitmapImage()
        {
            CreateOptions = BitmapCreateOptions.None
        };

        // callback methods (liveview)
        public void OnJpegRetrieved(byte[] data)
        {

            if (!cameraStatus.IsAvailableShooting)
            {
                cameraStatus.IsAvailableShooting = true;

                //GetMethodTypes(null); // Why call here?
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    NoticeUpdate();
                });
            }

            //Debug.WriteLine("[CameraManager] Jpeg retrived: " + size + "bytes.");

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

        public void OnLiveViewClosed()
        {
            Debug.WriteLine("liveView connection closed.");
            // init();
            cameraStatus.Init();
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                NoticeUpdate();
            });
        }

        public void RequestCloseLiveView()
        {
            lvProcessor.CloseConnection();
        }

        // --------- prepare

        public void RequestSearchDevices(Action Found, Action Timeout)
        {
            deviceFinder.SearchDevices(TIMEOUT, (info) => { OnServerFound(info, Found); }, () => { OnTimeout(); Timeout.Invoke(); });
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


        public void RequestActTakePicture()
        {
            // lvProcessor.CloseConnection();
            actTakePicture(OnActTakePictureError, OnResultActTakePicture);
        }

        private void actTakePicture(Action<int> error, Action<string[]> result)
        {
            if (client == null)
                return;

            client.ActTakePicture(error, result);

            cameraStatus.IsTakingPicture = true;
            NoticeUpdate();
        }

        public void OnResultActTakePicture(String[] res)
        {
            if (!ApplicationSettings.GetInstance().IsPostviewTransferEnabled)
            {
                StartToastAppearance(AppResources.Message_ImageCapture_Succeed);
                Scheduler.Dispatcher.Schedule(CloseToast, TimeSpan.FromSeconds(3));
                cameraStatus.IsTakingPicture = false;
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
                        if (StartToastAppearance != null)
                        {
                            StartToastAppearance(AppResources.Message_ImageDL_Succeed);
                            Scheduler.Dispatcher.Schedule(CloseToast, TimeSpan.FromSeconds(3));
                        }
                        cameraStatus.IsTakingPicture = false;
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
                        cameraStatus.IsTakingPicture = false;
                        NoticeUpdate();
                    }
                );
            }
        }

        private void CloseToast()
        {
            // _cameraStatus.IsToastVisible = false;
            if (StartToastDisappearance != null)
            {
                StartToastDisappearance();
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
            cameraStatus.IsTakingPicture = false;
            NoticeUpdate();
        }

        public void StartMovieRec()
        {
            if (client == null)
                return;

            client.StartMovieRec(OnError, () => { NoticeUpdate(); });
        }

        public void StopMovieRec()
        {
            if (client == null)
                return;

            client.StopMovieRec(OnError, (urls) => { NoticeUpdate(); });
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
            observer.Start(cameraStatus, OnDetectDifference, OnStop);
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
                    Debug.WriteLine("CameraStatus has changed: " + _cameraStatus.CameraStatus);
                    break;

                default:
                    //Debug.WriteLine("Difference detected: default");
                    break;
            }
        }

        public void OnStop()
        {

        }


        // -------

        public void OnTimeout()
        {
            Debug.WriteLine("request timeout.");
            NoticeUpdate();
        }

        public void OnError()
        {
            Debug.WriteLine("Error, something wrong.");
        }

        public void OnError(int errno)
        {
            Debug.WriteLine("Error: " + errno.ToString());
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
            client.SetShootMode(mode, OnError, () => { NoticeUpdate(); });
        }
    }
}
