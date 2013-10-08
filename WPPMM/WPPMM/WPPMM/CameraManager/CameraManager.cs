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


        private static int TIMEOUT = 10;
        // private static String dd_location = null;
        private static DeviceInfo deviceInfo;
        private static DeviceFinder deviceFinder = new DeviceFinder();

        // private static String endpoint = null;
        private static String liveViewUrl = null;
        private static Liveview.LVStreamProcessor lvProcessor = null;

        private static CameraServiceClient10 client;

        private static List<Action<Status>> UpdateListeners;

        private static Action<byte[]> LiveViewUpdateListener;
        private static System.Text.StringBuilder stringBuilder;

        private static byte[] screenData;

        private object lockObject;
        
        private Stopwatch watch;

        private Downloader downloader;

        private Status cameraStatus;


        private CameraManager()
        {
            Debug.WriteLine("Constructor on CameraManager");
            init();
        }

        private void init()
        {
            UpdateListeners = new List<Action<Status>>();
            stringBuilder = new System.Text.StringBuilder();
            liveViewUrl = null;
            lvProcessor = null;
            stringBuilder.Capacity = 64;
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
            if (deviceInfo.FriendlyName == "NEX-5R" || deviceInfo.FriendlyName == "NEX-5T" || deviceInfo.FriendlyName == "NEX-6")
            {
                Debug.WriteLine("it looks E-mount device. calling startRecmode.");
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
            CameraManager.GetInstance().cameraStatus.isAvailableShooting = false;
            NoticeUpdate();
        }


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

        public static void OnResultActTakePicture(String[] res)
        {

            foreach (String s in res)
            {
                CameraManager.GetInstance().downloader.DownloadImageFile(
                    new Uri(s),
                    delegate(Picture p)
                    {
                        Debug.WriteLine("download succeed");
                    },
                    delegate()
                    {
                        Debug.WriteLine("error");
                    }
                );

            }


            CameraManager.GetInstance().cameraStatus.isTakingPicture = false;
            CameraManager.NoticeUpdate();
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



        // getter
        public static String GetModelName()
        {
            return deviceInfo.ModelName;
        }

        public static String GetLiveviewUrl()
        {
            return liveViewUrl;
        }

        public static DeviceDiscovery.DeviceInfo GetDeviceInfo()
        {
            return deviceInfo;
        }


        // register callback for UI
        internal void RegisterUpdateListener(Action<Status> listener)
        {
            if (listener == null)
            {
                Debug.WriteLine("listener is null");
            }
            else if (UpdateListeners == null)
            {
                Debug.WriteLine("updateListener is null");
            }

            UpdateListeners.Add(listener);
        }

        // register EE screen update method
        public void SetLiveViewUpdateListener(Action<byte[]> action)
        {
            LiveViewUpdateListener = action;
        }

        // Notice update to UI classes
        private static void NoticeUpdate()
        {
            foreach (Action<Status> action in UpdateListeners)
            {
                action(CameraManager.GetInstance().cameraStatus);
            }

        }

    }
}
