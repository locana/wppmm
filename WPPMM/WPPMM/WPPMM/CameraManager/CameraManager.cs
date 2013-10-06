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
using WPPMM.Ssdp;
using WPPMM.RemoteApi;

namespace WPPMM.CameraManager
{
    public class CameraManager
    {

        // singleton instance
        private static CameraManager cameraManager = new CameraManager();


        private static int TIMEOUT = 10;
        // private static String dd_location = null;
        private static Ssdp.DeviceInfo deviceInfo;

        // private static String endpoint = null;
        private static String liveViewUrl = null;
        private static Liveview.LVProcessor lvProcessor = null;

        private static CameraServiceClient10 client;

        private static List<Action> UpdateListeners;
        private static Action<byte[]> LiveViewUpdateListener;
        private static System.Text.StringBuilder stringBuilder;

        private static byte[] screenData;

        private object lockObject;
        private bool isRendering;
        private Stopwatch watch;

        public bool isConnected
        {
            get;
            set;
        }

        public bool isAvailableShooting
        {
            get;
            set;
        }

        public bool isTakingPicture
        {
            get;
            set;
        }


        private CameraManager()
        {
            Debug.WriteLine("Constructor on CameraManager");
            init();
        }

        private void init()
        {
            UpdateListeners = new List<Action>();
            stringBuilder = new System.Text.StringBuilder();
            liveViewUrl = null;
            lvProcessor = null;
            stringBuilder.Capacity = 64;
            lockObject = new Object();
            isRendering = false;
            watch = new Stopwatch();
            watch.Start();
            deviceInfo = null;
            isConnected = false;
            isAvailableShooting = false;
            isTakingPicture = false;
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
            /*
            if (!deviceInfo.Endpoints.ContainsKey("camera"))
            {
                Debug.WriteLine("error: endpoint is null");
            }

            String endpoint = deviceInfo.Endpoints["camera"];

            Debug.WriteLine("endpoint: " + endpoint);
            String jsonReq = Json.Request.startRecMode();

            Debug.WriteLine("request json: " + jsonReq);
            
            Json.XhrPost.Post(endpoint, jsonReq, OnStartRecmode, OnError);
             * */

            if (client != null)
            {
                client.StartRecMode(OnError, OnStartRecmodeResult);
            }
        }

        /*
        public void OnStartRecmode(String json)
        {
            Debug.WriteLine("OnStartRecmode: " + json);

            Json.ResultHandler.StartRecMode(json, OnError, OnStartRecmodeResult);
        }
         * */

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
            /*
            String endpoint = deviceInfo.Endpoints["camera"];
            XhrPost.Post(endpoint, Request.startLiveview(),
                (res) => { ResultHandler.StartLiveview(res, error, result); },
                () => { error.Invoke(StatusCode.Any); });
             * */
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
            isConnected = true;
            NoticeUpdate();
        }

        // connect 
        public void ConnectLiveView()
        {
            lvProcessor = new LVProcessor();

            if (lvProcessor == null || liveViewUrl == null)
            {
                Debug.WriteLine("error: liveProcessor or liveViewUrl is null");
            }

            lvProcessor.OpenConnection(liveViewUrl, OnJpegRetrieved, OnLiveViewClosed);
        }

        // callback methods (liveview)
        public static void OnJpegRetrieved(byte[] data)
        {

            if (!CameraManager.GetInstance().isAvailableShooting)
            {
                CameraManager.GetInstance().isAvailableShooting = true;
                NoticeUpdate();
            }

            int size = data.Length;
            Debug.WriteLine("[CameraManager] Jpeg retrived: " + size + "bytes.");

            if (CameraManager.GetInstance().isRendering)
            {
                return;
            }

            screenData = data;

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                lock (CameraManager.GetInstance().lockObject)
                {

                    // Debug.WriteLine("[Start] BeginInvoke!" + watch.ElapsedMilliseconds + "ms");
                    CameraManager.GetInstance().isRendering = true;
                    LiveViewUpdateListener(screenData);
                    // Debug.WriteLine("[End  ] BeginInvoke!" + watch.ElapsedMilliseconds + "ms");
                    CameraManager.GetInstance().isRendering = false;
                }
            });


        }

        public static void OnLiveViewClosed()
        {
            Debug.WriteLine("liveView connection closed.");
            // init();
            CameraManager.GetInstance().isAvailableShooting = false;
            NoticeUpdate();
        }


        private static void requestSearchDevices()
        {
            // WPPMM.Ssdp.DeviceDiscovery.SearchScalarDevices(TIMEOUT, OnDDLocationFound, OnTimeout);
            WPPMM.Ssdp.DeviceDiscovery.SearchDevices(TIMEOUT, OnServerFound, OnTimeout);
        }


        public static void OnServerFound(Ssdp.DeviceInfo di)
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
            actTakePicture(OnActTakePictureError, OnResultActTakePicture);
        }

        public void actTakePicture(Action<int> error, Action<string[]> result)
        {
            /*
            String endpoint = deviceInfo.Endpoints["camera"];
            XhrPost.Post(endpoint, RequestGenerator.actTakePicture(),
                (res) => { ResultHandler.ActTakePicture(res, error, result); },
                () => { error.Invoke(StatusCode.Any); });
            */

            if (client != null)
            {
                client.ActTakePicture(error, result);
            }

            isTakingPicture = true;
            NoticeUpdate();
        }

        public static void OnResultActTakePicture(String[] res)
        {
            CameraManager.GetInstance().isTakingPicture = false;
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
            CameraManager.GetInstance().isTakingPicture = false;
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

        public static Ssdp.DeviceInfo GetDeviceInfo()
        {
            return deviceInfo;
        }


        // register callback for UI
        public void RegisterUpdateListener(Action listener)
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
            foreach (Action action in UpdateListeners)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() => { action(); });
            }
        }

    }
}
