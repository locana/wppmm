using System;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace WPPMM.Liveview
{
    public class LVProcessor
    {
        private bool processing = false;

        public void OpenConnection(string url, Action<byte[]> OnJpegRetrieved, Action OnClosed)
        {
            if (url == null || OnJpegRetrieved == null | OnClosed == null)
            {
                throw new ArgumentNullException();
            }

            if (processing)
            {
                throw new InvalidOperationException();
            }

            processing = true;

            var request = HttpWebRequest.Create(new Uri(url)) as HttpWebRequest;
            request.Method = "GET";

            var JpegStreamHandler = new AsyncCallback((ar) =>
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
                                while (processing)
                                {
                                    try
                                    {
                                        var data = Next(str);
                                        if (data != null)
                                        {
                                            OnJpegRetrieved.Invoke(data);
                                        }
                                    }
                                    catch (IOException)
                                    {
                                        processing = false;
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
                    Debug.WriteLine("DisConnected Jpeg stream");
                    processing = false;
                    OnClosed.Invoke();
                }
            });

            request.BeginGetResponse(JpegStreamHandler, request);
        }

        public void CloseConnection()
        {
            processing = false;
        }

        private const int CHeaderLength = 8;
        private const int PHeaderLength = 128;

        private static byte[] Next(Stream str)
        {
            byte[] CHeader = new byte[CHeaderLength];
            str.Read(CHeader, 0, CHeaderLength);
            if (CHeader[0] != (byte)0xFF || CHeader[1] != (byte)0x01) // Check fixed data
            {
                throw new IOException();
            }

            byte[] PHeader = new byte[PHeaderLength];
            str.Read(PHeader, 0, PHeaderLength);
            if (PHeader[0] != (byte)0x24 || PHeader[1] != (byte)0x35 || PHeader[2] != (byte)0x68 || PHeader[3] != (byte)0x79) // Check fixed data
            {
                throw new IOException();
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
            int data = 0;
            for (int i = 0; i < length; i++)
            {
                data += (bytearray[index + i] & 0xff) << 8 * length;
            }
            return data;
        }
    }

}
