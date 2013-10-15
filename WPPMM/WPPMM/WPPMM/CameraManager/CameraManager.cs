using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WPPMM.Liveview;
using System.IO;
using System.IO.IsolatedStorage;
using Microsoft.Phone;
using Microsoft.Xna.Framework.Media;
using System.Windows.Resources;
using System.Windows.Media.Imaging;
using WPPMM.DeviceDiscovery;
using WPPMM.RemoteApi;

namespace WPPMM.CameraManager
{
    public class CameraManager
    {

        // singleton instance
        private static CameraManager cameraManager = new CameraManager();


        private const int TIMEOUT = 10;
        public const String apiVersion = "1.0";

        private static DeviceInfo deviceInfo;
        private static DeviceFinder deviceFinder = new DeviceFinder();
        private static CameraServiceClient10 client;
        private static Liveview.LVStreamProcessor lvProcessor = null;

        private static String liveViewUrl = null;
        private object lockObject;
        private Downloader downloader;
        private Status cameraStatus;
        private static byte[] screenData;

        private static Action<byte[]> LiveViewUpdateListener;
        internal event Action<WPPMM.CameraManager.Status> UpdateEvent;

        private Stopwatch watch;

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
        public static void OnJpegRetrieved(byte[] data)
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

        public static void OnLiveViewClosed()
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

        private static void requestSearchDevices()
        {
            // WPPMM.DeviceDiscovery.DeviceDiscovery.SearchScalarDevices(TIMEOUT, OnDDLocationFound, OnTimeout);
            deviceFinder.SearchDevices(TIMEOUT, OnServerFound, OnTimeout);
        }


        public static void OnServerFound(DeviceDiscovery.DeviceInfo di)
        {
            deviceInfo = di;
            Debug.WriteLine("found device: " + deviceInfo.ModelName);


            if (deviceInfo.Endpoints.ContainsKey("camera"))
            {
                client = new CameraServiceClient10(di.Endpoints["camera"]);
                client.GetMethodTypes(apiVersion, OnError, new MethodTypesHandler(OnGetMethodTypes));
                GetInstance().cameraStatus.isAvailableConnecting = true;
            }
            // TODO be careful, device info is updated to the latest found device.

            NoticeUpdate();

        }

        internal static void OnGetMethodTypes(MethodType[] methodTypes)
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

        public static void OnResultActTakePicture(String[] res)
        {

            foreach (String s in res)
            {
                CameraManager.GetInstance().downloader.DownloadImageFile(
                    new Uri(s),
                    delegate(Picture p)
                    {
                        Debug.WriteLine("download succeed");
                        MessageBox.Show("Your picture has saved to the album successfully!");
                        CameraManager.GetInstance().cameraStatus.isTakingPicture = false;
                        CameraManager.NoticeUpdate();
                    },
                    delegate()
                    {
                        Debug.WriteLine("error");
                        MessageBox.Show("Error occured during downloading the picture..");
                        CameraManager.GetInstance().cameraStatus.isTakingPicture = false;
                        CameraManager.NoticeUpdate();
                    }
                );
            }
        }

        public static void OnActTakePictureError(int err)
        {
            if (err == RemoteApi.StatusCode.StillCapturingNotFinished)
            {
                Debug.WriteLine("capturing...");
                return;
            }

            Debug.WriteLine("Error during taking picture: " + err);
            CameraManager.GetInstance().cameraStatus.isTakingPicture = false;
            CameraManager.NoticeUpdate();
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

        internal static void OnActZoomResult()
        {
            Debug.WriteLine("Zoom operated.");
        }
        
       
        // -------

        public static void OnTimeout()
        {
            Debug.WriteLine("request timeout.");
            NoticeUpdate();
        }

        public static void OnError()
        {
            Debug.WriteLine("Error, something wrong.");
        }

        public static void OnError(int errno)
        {
            Debug.WriteLine("Error: " + errno.ToString());
        }
 
        public static DeviceDiscovery.DeviceInfo GetDeviceInfo()
        {
            return deviceInfo;
        }

        // register EE screen update method
        public void SetLiveViewUpdateListener(Action<byte[]> action)
        {
            LiveViewUpdateListener = action;
        }

        // Notice update to UI classes
        private static void NoticeUpdate()
        {
            GetInstance().UpdateEvent(GetInstance().cameraStatus);

        }


    }
}
