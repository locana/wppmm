using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;
using System.Linq;
using System.Xml.Linq;

namespace WPPMM.Ssdp
{
    public class DeviceDiscovery
    {
        private const string multicast_address = "239.255.255.250";
        private const int ssdp_port = 1900;
        private const int result_buffer = 8192;

        public static void SearchScalarDevices(int timeoutSec, Action<string> OnDDLocationFound, Action OnTimeout)
        {
            if (OnDDLocationFound == null || OnTimeout == null)
            {
                throw new ArgumentNullException();
            }

            if (timeoutSec < 2)
            {
                timeoutSec = 2;
            }

            const int MX = 1;

            var ssdp_data = new StringBuilder()
                .Append("M-SEARCH * HTTP/1.1").Append("\r\n")
                .Append("HOST: ").Append(multicast_address).Append(":").Append(ssdp_port.ToString()).Append("\r\n")
                .Append("MAN: ").Append("\"ssdp:discover\"").Append("\r\n")
                .Append("MX: ").Append(MX.ToString()).Append("\r\n")
                .Append("ST: urn:schemas-sony-com:service:ScalarWebAPI:1").Append("\r\n")
                //.Append("ST: ssdp:all").Append("\r\n") // For debug
                .Append("\r\n")
                .ToString();

            Debug.WriteLine(ssdp_data);

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            byte[] data_byte = Encoding.UTF8.GetBytes(ssdp_data);
            socket.SendBufferSize = data_byte.Length;

            SocketAsyncEventArgs snd_event_args = new SocketAsyncEventArgs();
            snd_event_args.RemoteEndPoint = new IPEndPoint(IPAddress.Parse(multicast_address), ssdp_port);
            snd_event_args.SetBuffer(data_byte, 0, data_byte.Length);

            SocketAsyncEventArgs rcv_event_args = new SocketAsyncEventArgs();
            rcv_event_args.SetBuffer(new byte[result_buffer], 0, result_buffer);

            var snd_handler = new EventHandler<SocketAsyncEventArgs>((sender, e) =>
            {
                if (e.SocketError == SocketError.Success && e.LastOperation == SocketAsyncOperation.SendTo)
                {
                    socket.ReceiveBufferSize = result_buffer;
                    socket.ReceiveAsync(rcv_event_args);
                }
            });
            snd_event_args.Completed += snd_handler;

            var rcv_handler = new EventHandler<SocketAsyncEventArgs>((sender, e) =>
            {
                if (e.SocketError == SocketError.Success && e.LastOperation == SocketAsyncOperation.Receive)
                {
                    string result = Encoding.UTF8.GetString(e.Buffer, 0, e.BytesTransferred);
                    //Debug.WriteLine(result);

                    var dd_location = ParseDDLocation(result);
                    if (dd_location != null)
                    {
                        OnDDLocationFound(dd_location);
                    }

                    socket.ReceiveAsync(e);
                }
            });
            rcv_event_args.Completed += rcv_handler;

            TimerCallback cb = new TimerCallback((state) =>
            {
                Debug.WriteLine("SSDP Timeout");
                snd_event_args.Completed -= snd_handler;
                rcv_event_args.Completed -= rcv_handler;
                socket.Close();
                OnTimeout.Invoke();
            });
            Timer timer = new Timer(cb, null, TimeSpan.FromSeconds(timeoutSec), new TimeSpan(-1));

            socket.SendToAsync(snd_event_args);
        }

        private static string ParseDDLocation(string response)
        {
            var reader = new StringReader(response);
            var line = reader.ReadLine();
            if (line != "HTTP/1.1 200 OK")
            {
                return null;
            }

            while (true)
            {
                line = reader.ReadLine();
                if (line == null)
                    break;
                if (line == "")
                    continue;

                int divider = line.IndexOf(':');
                if (divider < 1)
                    continue;

                string name = line.Substring(0, divider).Trim();
                if (name == "LOCATION" || name == "location")
                {
                    return line.Substring(divider + 1).Trim();
                }
            }

            return null;
        }

        public static void RetrieveEndpoints(string dd_url, Action<Dictionary<string, string>> OnResult, Action OnError)
        {
            if (dd_url == null || OnResult == null || OnError == null)
            {
                throw new ArgumentNullException();
            }

            try
            {
                var req = HttpWebRequest.Create(new Uri(dd_url)) as HttpWebRequest;
                req.Method = "GET";
                req.BeginGetResponse(OnDDObtained, new DDRequestInfo { req = req, OnResult = OnResult, OnError = OnError });
            }
            catch (UriFormatException)
            {
                OnError.Invoke();
            }
        }

        private static void OnDDObtained(IAsyncResult ar)
        {
            var info = ar.AsyncState as DDRequestInfo;

            try
            {
                var res = info.req.EndGetResponse(ar) as HttpWebResponse;
                using (var reader = new StreamReader(res.GetResponseStream(), Encoding.UTF8))
                {
                    try
                    {
                        var dic = GetEndpointsFromDD(reader.ReadToEnd());
                        info.OnResult.Invoke(dic);
                    }
                    catch (XmlException)
                    {
                        info.OnError.Invoke();
                    }
                }
            }
            catch (WebException)
            {
                info.OnError.Invoke();
            }
        }

        private static Dictionary<string, string> GetEndpointsFromDD(string response)
        {
            var endpoints = new Dictionary<string, string>();

            XDocument xml = XDocument.Parse(response);
            var info = xml.Element("av:X_ScalarWebAPI_DeviceInfo");
            if (info == null)
                throw new XmlException("av:X_ScalarWebAPI_DeviceInfo");

            var list = info.Element("av:X_ScalarWebAPI_ServiceList");
            if (list == null)
                throw new XmlException("av:X_ScalarWebAPI_ServiceList");

            foreach (var service in list.Elements())
            {
                var name = service.Element("av:X_ScalarWebAPI_ServiceType").Value;
                var url = service.Element("av:X_ScalarWebAPI_ActionList_URL").Value;
                if (name == null || url == null)
                    continue;

                string endpoint;
                if (url.EndsWith("/"))
                    endpoint = url + name;
                else
                    endpoint = url + "/" + name;

                endpoints.Add(name, endpoint);
            }

            return endpoints;
        }
    }

    class DDRequestInfo
    {
        public HttpWebRequest req;
        public Action<Dictionary<string, string>> OnResult;
        public Action OnError;
    }
}
