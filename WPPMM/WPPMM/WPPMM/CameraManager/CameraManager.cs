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
        private DeviceFinder deviceFinder = new DeviceFinder();
        private CameraServiceClient10 client;
        private LVStreamProcessor lvProcessor = null;

        private String liveViewUrl = null;
        private object lockObject;
        private Downloader downloader;
        private Status cameraStatus;
        private byte[] screenData;

        private Action<byte[]> LiveViewUpdateListener;
        internal event Action<Status> UpdateEvent;

        private Stopwatch watch;

        private EventObserver observer;

        private CameraManager()
        {
            init();
        }

        private void init()
        {
            liveViewUrl = null;
            lvProcessor = null;
            lockObject = new Object();
            watch = new Stopwatch();
            watch.Start();
            deviceInfo = null;
            downloader = new Downloader();

            cameraStatus = new Status();
        }

        public static CameraManager GetInstance()
        {
            return cameraManager;
        }

        public void InitializeConnection()
        {
            requestSearchDevices();
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
            lvProcessor = new LVStreamProcessor();

            if (lvProcessor == null || liveViewUrl == null)
            {
                Debug.WriteLine("error: liveProcessor or liveViewUrl is null");
            }

            lvProcessor.OpenConnection(liveViewUrl, OnJpegRetrieved, OnLiveViewClosed);
        }

        // callback methods (liveview)
        public void OnJpegRetrieved(byte[] data)
        {

            if (!CameraManager.GetInstance().cameraStatus.isAvailableShooting)
            {
                CameraManager.GetInstance().cameraStatus.isAvailableShooting = true;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    NoticeUpdate();
                });
            }

            int size = data.Length;
            Debug.WriteLine("[CameraManager] Jpeg retrived: " + size + "bytes.");

            if (CameraManager.GetInstance().cameraStatus.isRendering)
            {
                return;
            }

            screenData = data;

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                lock (CameraManager.GetInstance().lockObject)
                {

                    // Debug.WriteLine("[Start] BeginInvoke!" + watch.ElapsedMilliseconds + "ms");
                    CameraManager.GetInstance().cameraStatus.isRendering = true;
                    LiveViewUpdateListener(screenData);
                    // Debug.WriteLine("[End  ] BeginInvoke!" + watch.ElapsedMilliseconds + "ms");
                    CameraManager.GetInstance().cameraStatus.isRendering = false;
                }
            });


        }

        public void OnLiveViewClosed()
        {
            Debug.WriteLine("liveView connection closed.");
            // init();
            CameraManager.GetInstance().cameraStatus.Init();
            NoticeUpdate();
        }

        public void RequestCloseLiveView()
        {
            lvProcessor.CloseConnection();
        }

        // --------- prepare

        private void requestSearchDevices()
        {
            // WPPMM.DeviceDiscovery.DeviceDiscovery.SearchScalarDevices(TIMEOUT, OnDDLocationFound, OnTimeout);
            deviceFinder.SearchDevices(TIMEOUT, OnServerFound, OnTimeout);
        }


        public void OnServerFound(DeviceInfo di)
        {
            deviceInfo = di;
            Debug.WriteLine("found device: " + deviceInfo.ModelName);


            if (deviceInfo.Endpoints.ContainsKey("camera"))
            {
                client = new CameraServiceClient10(di.Endpoints["camera"]);

                client.GetMethodTypes(apiVersion, OnError, new MethodTypesHandler(OnGetMethodTypes));
                GetInstance().cameraStatus.isAvailableConnecting = true;

                GetInstance().InitEventObserver();
            }
            // TODO be careful, device info is updated to the latest found device.

            NoticeUpdate();

        }

        internal void OnGetMethodTypes(MethodType[] methodTypes)
        {
            List<String> list = new List<string>();
            foreach (MethodType t in methodTypes)
            {
                Debug.WriteLine("method: " + t.name);
                list.Add(t.name);
            }
            CameraManager.GetInstance().cameraStatus.MethodTypes = list;
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
                CameraManager.GetInstance().downloader.DownloadImageFile(
                    new Uri(s),
                    delegate(Picture p)
                    {
                        Debug.WriteLine("download succeed");
                        MessageBox.Show("Your picture has been saved to the album successfully!");
                        CameraManager.GetInstance().cameraStatus.isTakingPicture = false;
                        CameraManager.GetInstance().NoticeUpdate();
                    },
                    delegate()
                    {
                        Debug.WriteLine("error");
                        MessageBox.Show("Error occured during downloading the picture..");
                        CameraManager.GetInstance().cameraStatus.isTakingPicture = false;
                        CameraManager.GetInstance().NoticeUpdate();
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
            CameraManager.GetInstance().cameraStatus.isTakingPicture = false;
            CameraManager.GetInstance().NoticeUpdate();
        }


        // ------- zoom

        internal void RequestActZoom(String direction, String movement)
        {

            if (!cameraManager.cameraStatus.MethodTypes.Contains("actZoom"))
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

        public void OnStop(){
        
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
            UpdateEvent(GetInstance().cameraStatus);
        }


    }
}
