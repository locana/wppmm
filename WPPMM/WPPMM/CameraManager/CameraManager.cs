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

        private static String cameraUrl = null;
        private static String liveViewUrl = null;
        private Liveview.LVProcessor lvProcessor = null;

        private static Action<String> wifiStatusListener = null;

        private CameraManager()
        {
           
        }

        public static CameraManager GetInstance()
        {
            return cameraManager;
        }

        public void InitializeConnection()
        {
            requestSearchDevices();
        }


        // live view
        public void StartLiveView()
        {
            String requestJson = Json.Request.startLiveview();
            Debug.WriteLine("requestJson: " + requestJson);


        }

        // callback methods (liveview)
        public void OnJpegRetrieved(byte[] data)
        {
            Debug.WriteLine("Jpeg retrived.");
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
            Deployment.Current.Dispatcher.BeginInvoke(() => { wifiStatusListener("found dd_location" + location); });
            

            // get endpoint
            Ssdp.DeviceDiscovery.RetrieveEndpoints(dd_location, OnRetrieveEndpoints, OnError);
        }

        public static void OnRetrieveUrl(String url)
        {
            liveViewUrl = url;
            Debug.WriteLine("retrived url: " + url);

        }

        public static void OnRetrieveEndpoints(Dictionary <String, String> result)
        {
            Debug.WriteLine("retrived endpoint");

            if (result.ContainsKey("camera"))
            {
                cameraUrl = result["camera"];
                Debug.WriteLine("camera url found: " + cameraUrl);
            }
            else
            {
                Debug.WriteLine("camera url not found from retrived dictionary");
            }

            
        }

        public static void OnTimeout()
        {
            Debug.WriteLine("request timeout.");
            Deployment.Current.Dispatcher.BeginInvoke(
                () => { wifiStatusListener("hoge"); });
            
        }

        public static void OnError()
        {
            Debug.WriteLine("Error, something wrong.");
        }

        public static void OnError(int errno)
        {
            Debug.WriteLine("Error: " + errno.ToString());
        }



        // callback for UI
        public void SetWiFiStatusListener(Action<String> listener)
        {
            wifiStatusListener = listener;
                
        }

    }
}
