using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPPMM.CameraManager
{
    class SearchDevices
    {

        public SearchDevices()
        {
        }

        private static int TIMEOUT = 10;
        private static String dd_location = null;


        public static void RequestSearchDevices()
        {
            WPPMM.Ssdp.DeviceDiscovery.SearchScalarDevices(TIMEOUT, OnDDLocationFound, OnTimeout);
        }

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
