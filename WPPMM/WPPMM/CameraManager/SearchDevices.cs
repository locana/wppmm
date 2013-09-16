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

        // Singleton
        public SearchDevices()
        {
        }

        private static int TIMEOUT = 30;



        public static void RequestSearchDevices()
        {
            WPPMM.Ssdp.DeviceDiscovery.SearchScalarDevices(TIMEOUT, OnDDLocationFound, OnTimeout);
        }

        public static void OnDDLocationFound(String location)
        {
            Debug.WriteLine("found location: " + location);
        }

        private static void OnTimeout()
        {
            Debug.WriteLine("request timeout.");
        }
    }
}
