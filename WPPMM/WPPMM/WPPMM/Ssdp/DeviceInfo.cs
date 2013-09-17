using System.Collections.Generic;

namespace WPPMM.Ssdp
{
    public class DeviceInfo
    {
        internal DeviceInfo(string udn, string mname, string fname, Dictionary<string, string> ep)
        {
            UDN = udn;
            ModelName = mname;
            FriendlyName = fname;
            Endpoints = ep;
        }

        private Dictionary<string, string> _Endpoints;
        public Dictionary<string, string> Endpoints
        {
            private set { _Endpoints = value; }
            get { return new Dictionary<string, string>(_Endpoints); }
        }

        public string FriendlyName { private set; get; }

        public string ModelName { private set; get; }

        public string UDN { private set; get; }
    }
}
