using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Threading.Tasks;
#if WINDOWS_PHONE
using System.Net.Sockets;
using System.Windows;
using System.Windows.Threading;
#elif NETFX_CORE
using Windows.Networking.Sockets;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using System.Threading;
using Windows.Networking;
using Windows.UI.Core;
#endif

namespace WPPMM.DeviceDiscovery
{
    public class DeviceFinder
    {
        private const string multicast_address = "239.255.255.250";
        private const int ssdp_port = 1900;
        private const int result_buffer = 8192;

        /// <summary>
        /// Search devices and retrieve their device info.
        /// </summary>
        /// <param name="timeoutSec">Seconds to wait before invokation of OnTimeout.</param>
        /// <param name="OnServerFound">Success callback. This will be invoked for each devices until OnTimeout is invoked.</param>
        /// <param name="OnTimeout">Timeout callback.</param>
        public async void SearchDevices(int timeoutSec, Action<DeviceInfo> OnServerFound, Action OnTimeout)
        {
            if (OnServerFound == null || OnTimeout == null)
            {
                throw new ArgumentNullException();
            }

            Debug.WriteLine("DeviceFinder.SearchDevices");

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
            byte[] data_byte = Encoding.UTF8.GetBytes(ssdp_data);
            //Debug.WriteLine(ssdp_data);

            bool timeout_called = false;

            var DD_Handler = new AsyncCallback(ar =>
            {
                if (timeout_called)
                {
                    return;
                }

                var req = ar.AsyncState as HttpWebRequest;

                try
                {
                    var res = req.EndGetResponse(ar) as HttpWebResponse;
                    using (var reader = new StreamReader(res.GetResponseStream(), Encoding.UTF8))
                    {
                        try
                        {
                            var info = AnalyzeDD(reader.ReadToEnd());
                            NotifyFoundAsync(info, OnServerFound);
                        }
                        catch (Exception)
                        {
                            //Invalid XML.
                        }
                    }
                }
                catch (WebException)
                {
                    //Invalid DD location or network error.
                }
            });

#if WINDOWS_PHONE
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SendBufferSize = data_byte.Length;

            SocketAsyncEventArgs snd_event_args = new SocketAsyncEventArgs();
            snd_event_args.RemoteEndPoint = new IPEndPoint(IPAddress.Parse(multicast_address), ssdp_port);
            snd_event_args.SetBuffer(data_byte, 0, data_byte.Length);

            SocketAsyncEventArgs rcv_event_args = new SocketAsyncEventArgs();
            rcv_event_args.SetBuffer(new byte[result_buffer], 0, result_buffer);

            var SND_Handler = new EventHandler<SocketAsyncEventArgs>((sender, e) =>
            {
                if (e.SocketError == SocketError.Success && e.LastOperation == SocketAsyncOperation.SendTo)
                {
                    socket.ReceiveBufferSize = result_buffer;
                    socket.ReceiveAsync(rcv_event_args);
                }
            });
            snd_event_args.Completed += SND_Handler;

            var RCV_Handler = new EventHandler<SocketAsyncEventArgs>((sender, e) =>
            {
                if (e.SocketError == SocketError.Success && e.LastOperation == SocketAsyncOperation.Receive)
                {
                    string result = Encoding.UTF8.GetString(e.Buffer, 0, e.BytesTransferred);
                    //Debug.WriteLine(result);

                    GetDDAsync(DD_Handler, result);

                    socket.ReceiveAsync(e);
                }
            });
            rcv_event_args.Completed += RCV_Handler;
            socket.SendToAsync(snd_event_args);
#else
            var sock = new DatagramSocket();
            sock.MessageReceived += (sender, args) =>
            {
                if (timeout_called || args == null)
                {
                    return;
                }
                var reader = args.GetDataReader();
                string data = reader.ReadString(reader.UnconsumedBufferLength);
                Debug.WriteLine(data);

                GetDDAsync(DD_Handler, data);
            };
            try
            {
                await sock.BindServiceNameAsync(ssdp_port.ToString());
            }
            catch (Exception)
            {
                Debug.WriteLine("Duplicate search is not supported");
                return;
            }
            var host = new HostName(multicast_address);
            sock.JoinMulticastGroup(host);
            var output = await sock.GetOutputStreamAsync(host, ssdp_port.ToString());
            await output.WriteAsync(data_byte.AsBuffer());
            await sock.OutputStream.FlushAsync();
#endif

            await RunTimeoutInvokerAsync(timeoutSec, () =>
            {
                Debug.WriteLine("SSDP Timeout");
                timeout_called = true;
#if WINDOWS_PHONE
                snd_event_args.Completed -= SND_Handler;
                rcv_event_args.Completed -= RCV_Handler;
                socket.Close();
#else
                sock.Dispose();
#endif
                OnTimeout.Invoke();
            });
        }

        private async Task RunTimeoutInvokerAsync(int TimeoutSec, Action OnTimeout)
        {
            await Task.Delay(TimeSpan.FromSeconds(TimeoutSec));

#if WINDOWS_PHONE
            Deployment.Current.Dispatcher.BeginInvoke(() =>
#else
            await CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
#endif
            { OnTimeout.Invoke(); });
        }

        private async void NotifyFoundAsync(DeviceInfo info, Action<DeviceInfo> OnServerFound)
        {
#if WINDOWS_PHONE
            await Task.Run(() => { }); // avoid warning for async directive.
            Deployment.Current.Dispatcher.BeginInvoke(() =>
#else
            await CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
#endif
            { OnServerFound.Invoke(info); });
        }

        private static void GetDDAsync(AsyncCallback ac, string data)
        {
            var dd_location = ParseDDLocation(data);
            if (dd_location != null)
            {
                try
                {
                    var req = HttpWebRequest.Create(new Uri(dd_location)) as HttpWebRequest;
                    req.Method = "GET";
                    req.BeginGetResponse(ac, req);
                }
                catch (Exception)
                {
                    //Invalid DD location.
                }
            }
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

        private const string upnp_ns = "{urn:schemas-upnp-org:device-1-0}";
        private const string sony_ns = "{urn:schemas-sony-com:av}";

        private static DeviceInfo AnalyzeDD(string response)
        {
            //Debug.WriteLine(response);
            var endpoints = new Dictionary<string, string>();

            var xml = XDocument.Parse(response);
            var device = xml.Root.Element(upnp_ns + "device");
            var f_name = device.Element(upnp_ns + "friendlyName").Value;
            var m_name = device.Element(upnp_ns + "modelName").Value;
            var udn = device.Element(upnp_ns + "UDN").Value;
            var info = device.Element(sony_ns + "X_ScalarWebAPI_DeviceInfo");
            var list = info.Element(sony_ns + "X_ScalarWebAPI_ServiceList");

            foreach (var service in list.Elements())
            {
                var name = service.Element(sony_ns + "X_ScalarWebAPI_ServiceType").Value;
                var url = service.Element(sony_ns + "X_ScalarWebAPI_ActionList_URL").Value;
                if (name == null || url == null)
                    continue;

                string endpoint;
                if (url.EndsWith("/"))
                    endpoint = url + name;
                else
                    endpoint = url + "/" + name;

                endpoints.Add(name, endpoint);
            }

            if (endpoints.Count == 0)
            {
                throw new XmlException("No endoint found in XML");
            }

            return new DeviceInfo(udn, m_name, f_name, endpoints);
        }
    }
}
