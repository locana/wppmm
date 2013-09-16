using System.Collections.Generic;
using System.Diagnostics;
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

        /*
        public void testDDXmlParse()
        {
            string xml = "<?xml version=\"1.0\"?>\n<root xmlns=\"urn:schemas-upnp-org:device-1-0\">\n  <specVersion>\n    <major>1</major>\n    <minor>0</minor>\n  </specVersion>\n  <device>\n    <deviceType>urn:schemas-upnp-org:device:Basic:1</deviceType>\n    <friendlyName>NEX-5R</friendlyName>\n    <manufacturer>Sony Corporation</manufacturer>\n    <manufacturerURL>http://www.sony.net/</manufacturerURL>\n    <modelDescription>SonyDigitalMediaServer</modelDescription>\n    <modelName>SonyImagingDevice</modelName>\n    <UDN>uuid:000000001000-1010-8000-2202AF108974</UDN>\n    <serviceList>\n      <service>\n        <serviceType>urn:schemas-sony-com:service:ScalarWebAPI:1</serviceType>\n        <serviceId>urn:schemas-sony-com:serviceId:ScalarWebAPI</serviceId>\n        <SCPDURL></SCPDURL>\n        <controlURL></controlURL>\n        <eventSubURL></eventSubURL>\n      </service>\n    </serviceList>\n    <av:X_ScalarWebAPI_DeviceInfo xmlns:av=\"urn:schemas-sony-com:av\">\n      <av:X_ScalarWebAPI_Version>1.0</av:X_ScalarWebAPI_Version>\n      <av:X_ScalarWebAPI_ServiceList>\n        <av:X_ScalarWebAPI_Service>\n          <av:X_ScalarWebAPI_ServiceType>camera</av:X_ScalarWebAPI_ServiceType>\n          <av:X_ScalarWebAPI_ActionList_URL>http://192.168.122.1:8080/sony</av:X_ScalarWebAPI_ActionList_URL>\n          <av:X_ScalarWebAPI_AccessType></av:X_ScalarWebAPI_AccessType>\n        </av:X_ScalarWebAPI_Service>\n      </av:X_ScalarWebAPI_ServiceList>\n    </av:X_ScalarWebAPI_DeviceInfo>\n  </device>\n</root>\n"﻿;
            try
            {
                var dic = DeviceDiscovery.GetEndpointsFromDD(xml);
                Debug.WriteLine("camera endoint: " + dic["camera"]);
            }
            catch (XmlException)
            {
                Debug.WriteLine("XML Exception");
            }
        }
        */

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
