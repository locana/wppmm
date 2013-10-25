using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using WPPMM.DeviceDiscovery;
using WPPMM.Liveview;
using WPPMM.RemoteApi;

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
        private readonly LVStreamProcessor lvProcessor = new LVStreamProcessor();

        private String liveViewUrl = null;
        private readonly object lockObject = new Object();
        private readonly Downloader downloader = new Downloader();
        private Status cameraStatus;
        private byte[] screenData;

        private Action<byte[]> LiveViewUpdateListener;
        internal event Action<Status> UpdateEvent;

        private Stopwatch watch;

        private EventObserver observer;

        private CameraManager()
        {
            Refresh();
        }

        public void Refresh()
        {
            lvProcessor.CloseConnection();
            liveViewUrl = null;
            watch = new Stopwatch();
            watch.Start();
            deviceInfo = null;
            cameraStatus = new Status();
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
            if (client != null)
            {
                client.StartRecMode(OnError, OnStartRecmodeResult);
            }
        }

        public void OnStartRecmodeResult()
        {
            // finally, startrecMode is done.
            // for NEX-5R, starting to request liveview

            RequestStartLiveView();
        }

        // live view
        public void RequestStartLiveView()
        {
            startLiveview(OnError, OnStartLiveViewResult);

            // get image size
            client.GetPostviewImageSize(OnError, OnGetPostviewImageSize);
        }

        public void OnGetPostviewImageSize(String size)
        {
            Debug.WriteLine("Postview Image size: " + size);
        }

        public void startLiveview(Action<int> error, Action<string> result)
        {
            if (client != null)
            {
                client.StartLiveview(error, result);
            }
        }

        public void OnStartLiveViewResult(String result)
        {
            // finally, url for liveView has get
            Debug.WriteLine("OnStartLiveViewResult: " + result);
            liveViewUrl = result;
            cameraStatus.isConnected = true;
            NoticeUpdate();

        }

        // connect 
        public void ConnectLiveView()
        {
            if (liveViewUrl == null)
            {
                Debug.WriteLine("error: liveProcessor or liveViewUrl is null");
            }

            lvProcessor.OpenConnection(liveViewUrl, OnJpegRetrieved, OnLiveViewClosed);
        }

        // callback methods (liveview)
        public void OnJpegRetrieved(byte[] data)
        {

            if (!cameraStatus.isAvailableShooting)
            {
                cameraStatus.isAvailableShooting = true;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    NoticeUpdate();
                });
            }

            int size = data.Length;
            Debug.WriteLine("[CameraManager] Jpeg retrived: " + size + "bytes.");

            if (cameraStatus.isRendering)
            {
                return;
            }

            screenData = data;

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                lock (lockObject)
                {

                    // Debug.WriteLine("[Start] BeginInvoke!" + watch.ElapsedMilliseconds + "ms");
                    cameraStatus.isRendering = true;
                    LiveViewUpdateListener(screenData);
                    // Debug.WriteLine("[End  ] BeginInvoke!" + watch.ElapsedMilliseconds + "ms");
                    cameraStatus.isRendering = false;
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

                client.GetMethodTypes(apiVersion, OnError, (methodTypes) =>
                {
                    List<String> list = new List<string>();
                    foreach (MethodType t in methodTypes)
                    {
                        Debug.WriteLine("method: " + t.name);
                        list.Add(t.name);
                    }
                    cameraStatus.MethodTypes = list;
                    NoticeUpdate();
                    Found.Invoke();
                });
                cameraStatus.isAvailableConnecting = true;

                InitEventObserver();
            }
            // TODO be careful, device info is updated to the latest found device.

            NoticeUpdate();

        }

        // -------- take picture


        public void RequestActTakePicture()
        {
            // lvProcessor.CloseConnection();
            actTakePicture(OnActTakePictureError, OnResultActTakePicture);

        }

        private void actTakePicture(Action<int> error, Action<string[]> result)
        {

            if (client != null)
            {
                client.ActTakePicture(error, result);
            }

            cameraStatus.isTakingPicture = true;
            NoticeUpdate();
        }

        public void OnResultActTakePicture(String[] res)
        {

            foreach (String s in res)
            {
                downloader.DownloadImageFile(
                    new Uri(s),
                    delegate(Picture p)
                    {
                        Debug.WriteLine("download succeed");
                        MessageBox.Show("Your picture has been saved to the album successfully!");
                        cameraStatus.isTakingPicture = false;
                        NoticeUpdate();
                    },
                    delegate()
                    {
                        Debug.WriteLine("error");
                        MessageBox.Show("Error occured during downloading the picture..");
                        cameraStatus.isTakingPicture = false;
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
            cameraStatus.isTakingPicture = false;
            NoticeUpdate();
        }


        // ------- zoom

        internal void RequestActZoom(String direction, String movement)
        {

            if (!cameraStatus.MethodTypes.Contains("actZoom"))
            {
                // if zoom is not supported, display warning.　Yes, just warning.
                Debug.WriteLine("It seems this device does not support zoom");
            }

            if (client != null)
            {
                client.ActZoom(direction, movement, OnError, OnActZoomResult);
            }
        }

        internal void OnActZoomResult()
        {
            Debug.WriteLine("Zoom operated.");
        }

        // ------- Event Observer

        private void InitEventObserver()
        {
            observer = new EventObserver(client);
            observer.Start(cameraStatus, OnDetectDifference, OnStop);
        }

        public void OnDetectDifference(EventMember member)
        {
            switch (member)
            {
                case EventMember.ZoomInfo:
                    Debug.WriteLine("Difference detected: zoom");
                    break;
                default:
                    Debug.WriteLine("Difference detected: default");
                    break;
            }
            NoticeUpdate();
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
    }
}
