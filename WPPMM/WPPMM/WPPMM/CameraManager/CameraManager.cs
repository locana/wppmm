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
        private Liveview.LVProcessor lvProcessor = null;

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
            if (!deviceInfo.Endpoints.ContainsKey("camera"))
            {
                Debug.WriteLine("error: endpoint is null");
            }

            String endpoint = deviceInfo.Endpoints["camera"];

            Debug.WriteLine("endpoint: " + endpoint);
            String jsonReq = Json.Request.startRecMode();

            Debug.WriteLine("request json: " + jsonReq);
            
            Json.XhrPost.Post(endpoint, jsonReq, OnStartRecmode, OnError);
    
        }

        public void OnStartRecmode(String json)
        {
            Debug.WriteLine("OnStartRecmode: " + json);

            Json.ResultHandler.StartRecMode(json, OnError, OnStartRecmodeResult);
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
            if (!deviceInfo.Endpoints.ContainsKey("camera"))
            {
                Debug.WriteLine("error: endpoint is null");
            }

            String endpoint = deviceInfo.Endpoints["camera"];

            String requestJson = Json.Request.startLiveview();
            Debug.WriteLine("requestJson: " + requestJson);

            Json.XhrPost.Post(endpoint, requestJson, OnStartLiveViewRetrieved, OnError);
        }


        public void OnStartLiveViewRetrieved(String json)
        {
            Debug.WriteLine("StartLiveView retrieved: " + json);
            Json.ResultHandler.StartLiveview(json, OnError, OnStartLiveViewResult);

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
        public void OnJpegRetrieved(byte[] data)
        {

            if (!isAvailableShooting)
            {
                isAvailableShooting = true;
                NoticeUpdate();
            }

            /*
            int size = data.Length;
            Debug.WriteLine("Jpeg retrived. " + size + "bytes.");
            MemoryStream ms = new MemoryStream(data, 0, data.Length);
             */

            int size = data.Length;
            Debug.WriteLine("[CameraManager] Jpeg retrived: " + size + "bytes.");
            
            if (isRendering)
            {
                return;
            }
            
            screenData = data;

            Deployment.Current.Dispatcher.BeginInvoke(() => {
                lock (this.lockObject)
                {

                    // Debug.WriteLine("[Start] BeginInvoke!" + watch.ElapsedMilliseconds + "ms");
                    isRendering = true;
                    LiveViewUpdateListener(screenData);
                    // Debug.WriteLine("[End  ] BeginInvoke!" + watch.ElapsedMilliseconds + "ms");
                    isRendering = false;
                }
            });
            

        }

        public void OnLiveViewClosed()
        {
            Debug.WriteLine("liveView connection closed.");
            // init();
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
            NoticeUpdate();
        }


        // -------- take picture


        public void RequestActTakePicture()
        {
            if (!deviceInfo.Endpoints.ContainsKey("camera"))
            {
                Debug.WriteLine("error: endpoint is null");
            }
            String json = Json.Request.actTakePicture();
            String endpoint = deviceInfo.Endpoints["camera"];
            Json.XhrPost.Post(endpoint, json, OnActTakePictureRespond, OnError);
            isTakingPicture = true;
            NoticeUpdate();
        }

        public static void OnActTakePictureRespond(String json)
        {
            Debug.WriteLine("onrequestActtakePicture: " + json);
            Json.ResultHandler.ActTakePicture(json, OnActTakePictureError, OnResultActTakePicture);
        }

        public static void OnResultActTakePicture(String[] res)
        {
            CameraManager.GetInstance().isTakingPicture = false;
            CameraManager.NoticeUpdate();
        }

        public static void OnActTakePictureError(int err)
        {
            if (err == Json.StatusCode.StillCapturingNotFinished)
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
