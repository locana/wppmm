using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
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
        private Liveview.LVProcessor lvProcessor = null;

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

            if (requestJson == null)
            {
                Debug.WriteLine("startLiveView returns null....");
                return;
            }

            lvProcessor = new LVProcessor();
            lvProcessor.OpenConnection(dd_location, OnJpegRetrieved, OnLiveViewClosed);

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

            // get endpoint
            Ssdp.DeviceDiscovery.RetrieveEndpoints(dd_location, OnRetrieveEndpoints, OnError);
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
        }

        public static void OnError()
        {
            Debug.WriteLine("Error, something wrong.");
        }
    }
}
