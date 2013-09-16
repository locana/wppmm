using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPPMM.CameraManager
{
    class CameraManager
    {

        // singleton instance
        private static CameraManager cameraManager = new CameraManager();


        private static int TIMEOUT = 10;
        private static String dd_location = null;


        private CameraManager()
        {

        }

        public static CameraManager GetInstance()
        {
            return cameraManager;
        }


        public void RequestSearchDevices()
        {
            requestSearchDevices();
        }


        private static void requestSearchDevices()
        {
            WPPMM.Ssdp.DeviceDiscovery.SearchScalarDevices(TIMEOUT, OnDDLocationFound, OnTimeout);
        }


        // callback methods
        public static void OnDDLocationFound(String location)
        {
            dd_location = location;
            Debug.WriteLine("found location: " + location);
        }

        public static void OnTimeout()
        {
            Debug.WriteLine("request timeout.");
        }

    }
}
