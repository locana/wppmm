using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPPMM.Json;
using WPPMM.Ssdp;

namespace WPPMM.Test
{
    public class Test
    {
        public void testRequestGenerator()
        {
            string req = Request.actTakePicture();
            Debug.WriteLine(req);

            req = Request.actZoom("in", "1shot");
            Debug.WriteLine(req);

            req = Request.setSelfTimer(1);
            Debug.WriteLine(req);
        }

        public void testResultHandler()
        {
            string json = "{\"result\": [[\"http://ip:port/postview/postview.jpg\"]],\"id\": 1}";
            ResultHandler.ActTakePicture(json, HandleError, HandleActTakePictureResult);
        }

        public void testSsdp()
        {
            DeviceDiscovery.SearchScalarDevices(10, HandleDDLocation, HandleSsdpError);
        }

        private void HandleError(int code)
        {
            Debug.WriteLine("Error: " + code);
        }

        private void HandleActTakePictureResult(string[] urls)
        {
            Debug.WriteLine("HandleActTakePictureResult");
            foreach (var url in urls)
            {
                Debug.WriteLine("URL: " + url);
            }
        }

        private void HandleDDLocation(string dd_url)
        {
            Debug.WriteLine("handle dd location: " + dd_url);
            DeviceDiscovery.RetrieveEndpoints(dd_url, HandleEndpoints, HandleSsdpError);
        }

        private void HandleSsdpError()
        {
            Debug.WriteLine("handle ssdp error");
        }

        private void HandleEndpoints(Dictionary<string, string> endpoints)
        {
            Debug.WriteLine("handle endpoints");
            foreach (var service in endpoints.Keys)
            {
                Debug.WriteLine("Endpoint of " + service + ": " + endpoints[service]);
            }
        }
    }
}
