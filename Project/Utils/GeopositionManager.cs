using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Windows.Devices.Geolocation;
using Windows.Foundation;

namespace Kazyx.WPPMM.Utils
{
    class GeopositionManager
    {
        private static GeopositionManager _GeopositionManager = new GeopositionManager();

        internal Geoposition LatestPosition { get; set; }
        internal Action<GeopositionEventArgs> GeopositionUpdated;

        private Geolocator _Geolocator;
        private DispatcherTimer _Timer;
        private const int AcquiringInterval = 1; // min.
        private const int MaximumAge = 15; // min.
        private const int Timeout = 20; // sec.

        private bool _LocationAllowed = true;
        internal bool LocationAllowed
        {
            set { _LocationAllowed = value; }
            get { return _LocationAllowed; }
        }

        private bool _Enable = false;
        internal bool Enable
        {
            get { return _Enable; }
            set
            {
                _Enable = value;
                if (value)
                {
                    Task.Factory.StartNew(() => // Not to await in the set property
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            Start(); // this must be called on the UI thread.
                        });
                    });
                }
                else
                {
                    Stop();
                }
            }
        }

        private void Stop()
        {
            // _Geolocator.StatusChanged -= geolocator_StatusChanged;
            // _Geolocator.PositionChanged -= geolocator_PositionChanged;
            _Timer.Stop();
            LatestPosition = null;
        }

        private async void Start()
        {
            if (_Timer.IsEnabled)
            {
                await UpdateGeoposition();
                return;
            }
            _Geolocator.DesiredAccuracy = PositionAccuracy.Default;
            _Geolocator.MovementThreshold = 10;
            _Geolocator.ReportInterval = 60000;
            // _Geolocator.StatusChanged += geolocator_StatusChanged;
            // _Geolocator.PositionChanged += geolocator_PositionChanged;
            await UpdateGeoposition();
            _Timer.Start();
        }

        private async void OnTimerTick(object sender, EventArgs e)
        {
            await UpdateGeoposition();
        }

        private async Task UpdateGeoposition()
        {
            DebugUtil.Log("Starting to acquire geo location");
            OnGeopositionUpdated(new GeopositionEventArgs() { UpdatedPosition = LatestPosition, Status = GeopositiomManagerStatus.Acquiring });

            IAsyncOperation<Geoposition> locationTask = null;

            try
            {
                locationTask = _Geolocator.GetGeopositionAsync(
                    TimeSpan.FromMinutes(MaximumAge),
                    TimeSpan.FromSeconds(Timeout)
                    );
                LatestPosition = await locationTask;
            }
            catch (Exception ex)
            {
                if ((uint)ex.HResult == 0x80004004)
                {
                    DebugUtil.Log("Failed due to permission problem.");
                    OnGeopositionUpdated(new GeopositionEventArgs() { UpdatedPosition = null, Status = GeopositiomManagerStatus.Unauthorized });
                }
                DebugUtil.Log("Caught exception from GetGeopositionAsync");
                LatestPosition = null;
            }
            finally
            {
                if (locationTask != null)
                {
                    if (locationTask.Status == AsyncStatus.Started)
                    {
                        locationTask.Cancel();
                    }
                    locationTask.Close();
                }
            }

            if (LatestPosition == null)
            {
                OnGeopositionUpdated(new GeopositionEventArgs() { UpdatedPosition = null, Status = GeopositiomManagerStatus.Failed });
            }
            else
            {
                OnGeopositionUpdated(new GeopositionEventArgs() { UpdatedPosition = LatestPosition, Status = GeopositiomManagerStatus.OK });
            }
        }

        protected void OnGeopositionUpdated(GeopositionEventArgs e)
        {
            if (GeopositionUpdated != null)
            {
                GeopositionUpdated(e);
            }
        }

        internal async Task<Geoposition> AcquireGeoPosition()
        {
            if (LatestPosition != null)
            {
                return LatestPosition;
            }
            await UpdateGeoposition();
            return LatestPosition;
        }

        private GeopositionManager()
        {
            _Geolocator = new Geolocator();
            _Timer = new DispatcherTimer();
            _Timer.Interval = TimeSpan.FromMinutes(AcquiringInterval);
            _Timer.Tick += new EventHandler(OnTimerTick);
        }

        public static GeopositionManager GetInstance()
        {
            return _GeopositionManager;
        }
    }

    internal class GeopositionEventArgs : EventArgs
    {
        public Geoposition UpdatedPosition { get; set; }
        public GeopositiomManagerStatus Status { get; set; }
    }

    internal enum GeopositiomManagerStatus
    {
        Unauthorized,
        Failed,
        Acquiring,
        OK,
    };
}
