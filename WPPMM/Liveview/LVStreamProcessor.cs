using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace WPPMM.Liveview
{
    public class LvStreamProcessor
    {
        /// <summary>
        /// Connection status of this LVProcessor.
        /// </summary>
        public bool IsOpen
        {
            get
            {
                if (core != null)
                {
                    return core.IsOpen;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool IsProcessing
        {
            get
            {
                return state != State.Closed;
            }
        }

        private StreamAnalizer core;

        private const int DEFAULT_REQUEST_TIMEOUT = 5000;

        private State state = State.Closed;

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
        public async void OpenConnection(string url, TimeSpan? timeout = null)
        {
            Log("OpenConnection");
            if (url == null)
            {
                throw new ArgumentNullException();
            }

            if (state != State.Closed)
            {
                return;
            }

            state = State.TryingConnection;

            var to = (timeout == null) ? TimeSpan.FromMilliseconds(DEFAULT_REQUEST_TIMEOUT) : timeout;

            var Request = HttpWebRequest.Create(new Uri(url)) as HttpWebRequest;
            Request.Method = "GET";
            Request.AllowReadStreamBuffering = false;

            var JpegStreamHandler = new AsyncCallback((ar) =>
            {
                state = State.Connected;
                if (core != null)
                {
                    core.Dispose();
                }
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
                                core = new StreamAnalizer(str);
                                core.RunFpsDetector();

                                while (IsOpen)
                                {
                                    try
                                    {
                                        OnJpegRetrieved(new JpegEventArgs(core.Next()));
                                    }
                                    catch (IOException)
                                    {
                                        Log("Caught IOException: finish reading loop");
                                        break;
                                    }
                                }
                                Log("End of reading loop");
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

            await Task.Delay((int)to.Value.TotalMilliseconds);
            if (state == State.TryingConnection)
            {
                Log("Open request timeout: aborting request.");
                Request.Abort();
            }
        }

        /// <summary>
        /// Forcefully close this connection.
        /// </summary>
        public void CloseConnection()
        {
            Log("CloseConnection");
            if (core != null)
            {
                core.Dispose();
            }
            core = null;
            state = State.Closed;
        }

        private static void Log(string message)
        {
            Debug.WriteLine("[LVSProcessor] " + message);
        }
    }

    internal enum State
    {
        Closed,
        TryingConnection,
        Connected
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
