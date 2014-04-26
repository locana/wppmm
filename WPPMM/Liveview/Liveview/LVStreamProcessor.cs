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
            }
        }

        private bool _IsOpen = false;

        private bool IsDisposed = false;

        public event EventHandler Closed;

        public delegate void LiveviewStreamHandler(object sender, JpegEventArgs e);

        public event LiveviewStreamHandler JpegRetrieved;

        protected void OnClosed(EventArgs e)
        {
            if (Closed != null)
            {
                Closed(this, e);
            }
        }

        protected void OnJpegRetrieved(JpegEventArgs e)
        {
            if (JpegRetrieved != null)
            {
                JpegRetrieved(this, e);
            }
        }

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
        public void OpenConnection(string url)
        {
            Log("OpenConnection");
            if (IsDisposed)
            {
                throw new ObjectDisposedException("This LvStreamProcessor is already disposed");
            }
            if (url == null)
            {
                throw new ArgumentNullException();
            }

            if (IsOpen)
            {
                return;
            }

            IsOpen = true;

            var Request = HttpWebRequest.Create(new Uri(url)) as HttpWebRequest;
            Request.Method = "GET";
            Request.AllowReadStreamBuffering = false;

            var JpegStreamHandler = new AsyncCallback((ar) =>
            {
                try
                {
                    var req = ar.AsyncState as HttpWebRequest;
                    using (var Response = req.EndGetResponse(ar) as HttpWebResponse)
                    {
                        if (Response.StatusCode == HttpStatusCode.OK)
                        {
                            Log("Connected Jpeg stream");
                            using (var str = Response.GetResponseStream())
                            {
                                RunFpsDetector();

                                while (IsOpen)
                                {
                                    try
                                    {
                                        OnJpegRetrieved(new JpegEventArgs(Next(str)));
                                    }
                                    catch (IOException)
                                    {
                                        Log("Caught IOException: finish while loop");
                                        IsOpen = false;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (WebException)
                {
                    Log("WebException inside StreamingHandler.");
                }
                catch (ObjectDisposedException)
                {
                    Log("Caught ObjectDisposedException inside StreamingHandler.");
                }
                catch (IOException)
                {
                    Log("Caught IOException inside StreamingHandler.");
                }
                finally
                {
                    Log("Disconnected Jpeg stream");
                    CloseConnection();
                    OnClosed(new EventArgs());
                }
            });

            Request.BeginGetResponse(JpegStreamHandler, Request);
        }

        private async void RunFpsDetector()
        {
            await Task.Delay(TimeSpan.FromMilliseconds(fps_interval));
            var fps = packet_counter * 1000 / fps_interval;
            packet_counter = 0;
            Log("- - - - " + fps + " FPS - - - -");
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
            Log("CloseConnection");
            IsDisposed = true;
            IsOpen = false;
        }

        private const int CHeaderLength = 8;
        private const int PHeaderLength = 128;

        private byte[] Next(Stream str)
        {
            var CHeader = BlockingRead(str, CHeaderLength);
            if (CHeader[0] != (byte)0xFF || CHeader[1] != (byte)0x01) // Check fixed data
            {
                Log("Unexpected common header");
                throw new IOException("Unexpected common header");
            }

            var PHeader = BlockingRead(str, PHeaderLength);
            if (PHeader[0] != (byte)0x24 || PHeader[1] != (byte)0x35 || PHeader[2] != (byte)0x68 || PHeader[3] != (byte)0x79) // Check fixed data
            {
                Log("Unexpected payload header");
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
                        Log("IsOpen false: Finish while loop");
                        throw new IOException("Force finish reading");
                    }
                    try
                    {
                        read = str.Read(ReadBuffer, 0, Math.Min(ReadBuffer.Length, remainBytes));
                    }
                    catch (ObjectDisposedException)
                    {
                        Log("Caught ObjectDisposedException while reading bytes: forcefully disposed.");
                        throw new IOException("Stream forcefully disposed");
                    }
                    if (read < 0)
                    {
                        Log("Detected end of stream.");
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

        private static void Log(string message)
        {
            Debug.WriteLine("[LVSProcessor] " + message);
        }
    }

    public class JpegEventArgs : EventArgs
    {
        private readonly byte[] jpegData;

        public JpegEventArgs(byte[] data)
        {
            this.jpegData = data;
        }

        public byte[] JpegData
        {
            get { return jpegData; }
        }
    }
}
