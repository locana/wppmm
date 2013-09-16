using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WPPMM.Liveview;

namespace WPPMM.CameraManager
{
    class CameraManager
    {

        // singleton instance
        private static CameraManager cameraManager = new CameraManager();


        private static int TIMEOUT = 10;
        private static String dd_location = null;

        private static String endpoint = null;
        private static String liveViewUrl = null;
        private Liveview.LVProcessor lvProcessor = null;

        private static String friendlyName = "NEX-5R";

        private static List<Action> UpdateListeners;

        private CameraManager()
        {
            UpdateListeners = new List<Action>();
           
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
            if (friendlyName == "NEX-5R")
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
            if (endpoint == null)
            {
                Debug.WriteLine("error: endpoint is null");
            }

            // override endpoint
            // endpoint = "http://192.168.122.1:8080/sony/index.html";

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
            

            if (endpoint == null)
            {
                Debug.WriteLine("error: endpoint is null");
                return;
            }

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
            int size = data.Length;
            Debug.WriteLine("Jpeg retrived. " + size + "bytes.");



        }

        public void OnLiveViewClosed()
        {
            Debug.WriteLine("liveView connection closed.");
        }


        private static void requestSearchDevices()
        {
            WPPMM.Ssdp.DeviceDiscovery.SearchScalarDevices(TIMEOUT, OnDDLocationFound, OnTimeout);
        }


        // callback methods (search)
        public static void OnDDLocationFound(String location)
        {
            dd_location = location;
            Debug.WriteLine("found dd_location: " + location);
            NoticeUpdate();

            // override dd_location for debug.
            // dd_location = "http://192.168.122.1:8080/sony/index.html";

            // get endpoint
            
            Ssdp.DeviceDiscovery.RetrieveEndpoints(dd_location, OnRetrieveEndpoints, OnError);
        }


        public static void OnRetrieveEndpoints(Dictionary <String, String> result)
        {
            Debug.WriteLine("retrived endpoint");

            if (result.ContainsKey("camera"))
            {
                endpoint = result["camera"];
                Debug.WriteLine("camera url found: " + endpoint);
            }
            else
            {
                Debug.WriteLine("camera url not found from retrived dictionary");
            }

            
        }

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
        public static String GetDDlocation()
        {
            return dd_location;
        }

        public static String GetLiveviewUrl()
        {
            return liveViewUrl;
        }

        // register callback for UI
        public void RegisterUpdateListener(Action listener)
        {
            UpdateListeners.Add(listener);                
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
