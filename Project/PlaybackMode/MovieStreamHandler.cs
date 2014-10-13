using Kazyx.ImageStream;
using Kazyx.RemoteApi;
using Kazyx.RemoteApi.AvContent;
using Kazyx.WPPMM.DataModel;
using Kazyx.WPPMM.Utils;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Kazyx.WPPMM.PlaybackMode
{
    public class MovieStreamHandler
    {
        private static MovieStreamHandler instance = new MovieStreamHandler();

        public static MovieStreamHandler INSTANCE
        {
            get { return instance; }
        }

        public bool IsProcessing { get { return AvContent != null; } }

        private AvContentApiClient AvContent = null;

        private MoviePlaybackData _MoviePlaybackData = new MoviePlaybackData();
        public MoviePlaybackData MoviePlaybackData { get { return _MoviePlaybackData; } }

        private readonly StreamProcessor StreamProcessor = new StreamProcessor();

        private MovieStreamHandler()
        {
            StreamProcessor.JpegRetrieved += StreamProcessor_JpegRetrieved;
            StreamProcessor.PlaybackInfoRetrieved += StreamProcessor_PlaybackInfoRetrieved;
            StreamProcessor.Closed += StreamProcessor_Closed;
        }

        public async Task<bool> Start(AvContentApiClient api, PlaybackContent content, string name)
        {
            if (IsProcessing)
            {
                throw new InvalidOperationException("Already processing");
            }
            AvContent = api;

            try
            {
                var location = await api.SetStreamingContentAsync(content);
                await api.StartStreamingAsync();
                RunLoop(false);

                var success = await StreamProcessor.OpenConnection(new Uri(location.Url));
                if (!success)
                {
                    AvContent = null;
                }
                MoviePlaybackData.FileName = name;
                return success;
            }
            catch (Exception e)
            {
                DebugUtil.Log(e.StackTrace);
                AvContent = null;
                return false;
            }
        }

        public async void Finish()
        {
            StreamProcessor.CloseConnection();

            if (AvContent == null)
            {
                return;
            }

            try
            {
                await AvContent.StopStreamingAsync();
            }
            catch (Exception e)
            {
                DebugUtil.Log("Failed to stop movie stream");
            }
            finally
            {
                AvContent = null;
            }
        }

        private async void RunLoop(bool polling = true)
        {
            if (AvContent != null)
            {
                try
                {
                    var status = await AvContent.RequestToNotifyStreamingStatusAsync(new LongPollingFlag { ForLongPolling = polling });
                    OnStatusChanged(status);
                    RunLoop();
                }
                catch (RemoteApiException e)
                {
                    switch (e.code)
                    {
                        case StatusCode.Timeout:
                            DebugUtil.Log("RequestToNotifyStreamingStatus timeout without any event. Retry for the next event");
                            RunLoop();
                            return;
                        default:
                            DebugUtil.Log("RequestToNotifyStreamingStatus finished with unexpected error: " + e.code);
                            // Finish();
                            break;
                    }
                }
            }
        }

        public event EventHandler StreamClosed;
        public event StreamingStatusHandler StatusChanged;

        protected void OnStatusChanged(StreamingStatus status)
        {
            if (StatusChanged != null)
            {
                var arg = new StreamingStatusEventArgs();
                arg.Status = status;
                StatusChanged(this, arg);
            }
        }

        void StreamProcessor_Closed(object sender, EventArgs e)
        {
            DebugUtil.Log("StreamClosed. Finish MovieStreamHandler");
            Finish();
            if (StreamClosed != null)
            {
                StreamClosed(sender, e);
            }
        }

        void StreamProcessor_PlaybackInfoRetrieved(object sender, PlaybackInfoEventArgs e)
        {
            DebugUtil.Log("playback info: " + MoviePlaybackData.FileName + " " + e.Packet.Duration.TotalSeconds);
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                MoviePlaybackData.CurrentPosition = e.Packet.CurrentPosition;
                MoviePlaybackData.Duration = e.Packet.Duration;
            });
        }

        private bool IsRendering = false;

        BitmapImage ImageSource = new BitmapImage()
        {
            CreateOptions = BitmapCreateOptions.None,
        };

        void StreamProcessor_JpegRetrieved(object sender, JpegEventArgs e)
        {
            if (IsRendering) { return; }

            IsRendering = true;
            var size = e.Packet.ImageData.Length;
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                using (var stream = new MemoryStream(e.Packet.ImageData, 0, size))
                {
                    MoviePlaybackData.Image = null;
                    ImageSource.SetSource(stream);
                    MoviePlaybackData.Image = ImageSource;
                    IsRendering = false;
                }
            });
        }
    }

    public delegate void StreamingStatusHandler(object sender, StreamingStatusEventArgs e);

    public class StreamingStatusEventArgs : EventArgs
    {
        public StreamingStatus Status { get; internal set; }
    }

    public class MoviePlaybackData : LiveviewData
    {
        public MoviePlaybackData() { }

        private TimeSpan _CurrentPosition;
        public TimeSpan CurrentPosition
        {
            get { return _CurrentPosition; }
            set
            {
                _CurrentPosition = value;
                OnPropertyChanged("CurrentPosition");
            }
        }

        private TimeSpan _Duration;
        public TimeSpan Duration
        {
            get { return _Duration; }
            set
            {
                _Duration = value;
                OnPropertyChanged("Duration");
            }
        }

        private string _FileName;
        public string FileName
        {
            get { return _FileName; }
            set
            {
                if (_FileName != value)
                {
                    _FileName = value;
                    OnPropertyChanged("FileName");
                }
            }
        }
    }
}
