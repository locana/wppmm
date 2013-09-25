using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace WPPMM.Liveview
{
    public class LVProcessor
    {
        private const int ReadTimeout = 10000; //msec

        /// <summary>
        /// Connection status of this LVProcessor.
        /// </summary>
        public bool IsOpen
        {
            get { return _IsOpen; }
            private set { _IsOpen = value; }
        }

        private bool _IsOpen = false;

        /// <summary>
        /// Open stream connection for Liveview.
        /// </summary>
        /// <remarks>
        /// <para>Success callback will be invoked for each retrieved jpeg data until Close callback is invoked.</para> 
        /// <para>InvalidOperationException will be thrown when stream is already open.</para>
        /// </remarks>
        /// <param name="url">URL of the liveview. Get this via startLiveview API.</param>
        /// <param name="OnJpegRetrieved">Success callback.</param>
        /// <param name="OnClosed">Connection close callback.</param>
        public void OpenConnection(string url, Action<byte[]> OnJpegRetrieved, Action OnClosed)
        {
            if (url == null || OnJpegRetrieved == null | OnClosed == null)
            {
                throw new ArgumentNullException();
            }

            if (IsOpen)
            {
                throw new InvalidOperationException();
            }

            IsOpen = true;

            var request = HttpWebRequest.Create(new Uri(url)) as HttpWebRequest;
            request.Method = "GET";

            var JpegStreamHandler = new AsyncCallback(async (ar) =>
            {
                try
                {
                    var req = ar.AsyncState as HttpWebRequest;
                    using (var res = req.EndGetResponse(ar) as HttpWebResponse)
                    {
                        if (res.StatusCode == HttpStatusCode.OK)
                        {
                            Debug.WriteLine("Connected Jpeg stream");
                            using (var str = res.GetResponseStream())
                            {
                                str.ReadTimeout = ReadTimeout;
                                while (IsOpen)
                                {
                                    try
                                    {
                                        var data = await Next(str);
                                        if (data != null)
                                        {
                                            OnJpegRetrieved.Invoke(data);
                                        }
                                    }
                                    catch (IOException)
                                    {
                                        IsOpen = false;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (WebException)
                {
                    Debug.WriteLine("WebException");
                }
                finally
                {
                    Debug.WriteLine("Disconnected Jpeg stream");
                    IsOpen = false;
                    OnClosed.Invoke();
                }
            });

            request.BeginGetResponse(JpegStreamHandler, request);
        }

        /// <summary>
        /// Forcefully close this connection.
        /// </summary>
        public void CloseConnection()
        {
            IsOpen = false;
        }

        private const int CHeaderLength = 8;
        private const int PHeaderLength = 128;

        private async static Task<byte[]> Next(Stream str)
        {
            byte[] CHeader = new byte[CHeaderLength];
            await str.ReadAsync(CHeader, 0, CHeaderLength);
            if (CHeader[0] != (byte)0xFF || CHeader[1] != (byte)0x01) // Check fixed data
            {
                Debug.WriteLine("Unexpected common header");
                throw new IOException("Unexpected common header");
            }

            byte[] PHeader = new byte[PHeaderLength];
            str.Read(PHeader, 0, PHeaderLength);
            if (PHeader[0] != (byte)0x24 || PHeader[1] != (byte)0x35 || PHeader[2] != (byte)0x68 || PHeader[3] != (byte)0x79) // Check fixed data
            {
                Debug.WriteLine("Unexpected payload header");
                throw new IOException("Unexpected payload header");
            }
            int data_size = ReadIntFromByteArray(PHeader, 4, 3);
            int padding_size = ReadIntFromByteArray(PHeader, 7, 1);

            byte[] data = new byte[data_size];
            byte[] padding = new byte[padding_size];

            str.Read(data, 0, data_size);
            str.Read(padding, 0, padding_size); // discard padding from stream

            return data;
        }

        private static int ReadIntFromByteArray(byte[] bytearray, int index, int length)
        {
            int int_data = 0;
            for (int i = 0; i < length; i++)
            {
                int_data = (int_data << 8) | (bytearray[index + i] & 0xff);
            }
            return int_data;
        }
    }

}
