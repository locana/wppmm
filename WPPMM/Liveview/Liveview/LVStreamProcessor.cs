using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace WPPMM.Liveview
{
    public class LvStreamProcessor
    {
        private const int fps_interval = 5000;
        private int packet_counter = 0;

        /// <summary>
        /// Connection status of this LVProcessor.
        /// </summary>
        public bool IsOpen
        {
            get { return _IsOpen; }
            private set
            {
                _IsOpen = value;
                if (!value)
                {
                    ConnectedStream = null;
                }
            }
        }

        private bool _IsOpen = false;

        private bool IsDisposed = false;

        private HttpWebRequest Request = null;

        private HttpWebResponse Response = null;

        private Stream ConnectedStream = null;

        /// <summary>
        /// Open stream connection for Liveview.
        /// </summary>
        /// <remarks>
        /// <para>Success callbacks are invoked for each retrieved jpeg data until Close callback is invoked.</para>
        /// <para>All of callbacks are invoked on the worker thread.</para>
        /// <para>InvalidOperationException is thrown if stream is already open.</para>
        /// <para>If you've called CloseConnection or OnClose is already invoked, ObjectDisposedException is thrown.</para>
        /// </remarks>
        /// <param name="url">URL of the liveview. Get this via startLiveview API.</param>
        /// <param name="OnJpegRetrieved">Success callback.</param>
        /// <param name="OnClosed">Connection close callback.</param>
        public void OpenConnection(string url, Action<byte[]> OnJpegRetrieved, Action OnClosed)
        {
            Debug.WriteLine("LiveviewStreamProcessor.OpenConnection");
            if (IsDisposed)
            {
                throw new ObjectDisposedException("This LvStreamProcessor is already disposed");
            }
            if (url == null || OnJpegRetrieved == null | OnClosed == null)
            {
                throw new ArgumentNullException();
            }

            if (IsOpen)
            {
                throw new InvalidOperationException("Liveview stream is already open");
            }

            IsOpen = true;

            Request = HttpWebRequest.Create(new Uri(url)) as HttpWebRequest;
            Request.Method = "GET";
            Request.AllowReadStreamBuffering = false;

            var JpegStreamHandler = new AsyncCallback((ar) =>
            {
                try
                {
                    var req = ar.AsyncState as HttpWebRequest;
                    using (Response = req.EndGetResponse(ar) as HttpWebResponse)
                    {
                        if (Response.StatusCode == HttpStatusCode.OK)
                        {
                            Debug.WriteLine("Connected Jpeg stream");
                            using (var str = Response.GetResponseStream())
                            {
                                RunFpsDetector();

                                while (IsOpen)
                                {
                                    ConnectedStream = str;
                                    try
                                    {
                                        OnJpegRetrieved(Next(str));
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
                    CloseConnection();
                    OnClosed.Invoke();
                }
            });

            Request.BeginGetResponse(JpegStreamHandler, Request);
        }

        private async void RunFpsDetector()
        {
            await Task.Delay(TimeSpan.FromMilliseconds(fps_interval));
            var fps = packet_counter * 1000 / fps_interval;
            packet_counter = 0;
            Debug.WriteLine("- - - - " + fps + " FPS - - - -");
            if (IsOpen)
            {
                RunFpsDetector();
            }
        }

        /// <summary>
        /// Forcefully close this connection.
        /// </summary>
        public void CloseConnection()
        {
            IsDisposed = true;
            if (ConnectedStream != null)
            {
                ConnectedStream.Dispose();
            }
            if (Response != null)
            {
                Response.Dispose();
            }
            if (Request != null)
            {
                Request.Abort();
            }
            IsOpen = false;
        }

        private const int CHeaderLength = 8;
        private const int PHeaderLength = 128;

        private byte[] Next(Stream str)
        {
            var CHeader = BlockingRead(str, CHeaderLength);
            if (CHeader[0] != (byte)0xFF || CHeader[1] != (byte)0x01) // Check fixed data
            {
                Debug.WriteLine("Unexpected common header");
                throw new IOException("Unexpected common header");
            }

            var PHeader = BlockingRead(str, PHeaderLength);
            if (PHeader[0] != (byte)0x24 || PHeader[1] != (byte)0x35 || PHeader[2] != (byte)0x68 || PHeader[3] != (byte)0x79) // Check fixed data
            {
                Debug.WriteLine("Unexpected payload header");
                throw new IOException("Unexpected payload header");
            }
            int data_size = ReadIntFromByteArray(PHeader, 4, 3);
            int padding_size = ReadIntFromByteArray(PHeader, 7, 1);

            var data = BlockingRead(str, data_size);
            BlockingRead(str, padding_size); // discard padding from stream

            packet_counter++;

            return data;
        }

        private byte[] ReadBuffer = new byte[8192];

        private byte[] BlockingRead(Stream str, int numBytes)
        {
            var remainBytes = numBytes;
            int read;
            using (var output = new MemoryStream())
            {
                while (remainBytes > 0)
                {
                    if (!IsOpen)
                    {
                        throw new IOException("Force finish reading");
                    }
                    try
                    {
                        read = str.Read(ReadBuffer, 0, Math.Min(ReadBuffer.Length, remainBytes));
                    }
                    catch (ObjectDisposedException)
                    {
                        throw new IOException("Stream forcefully disposed");
                    }
                    if (read < 0)
                    {
                        throw new IOException("End of stream");
                    }
                    remainBytes -= read;
                    output.Write(ReadBuffer, 0, read);
                }
                return output.ToArray();
            }
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
