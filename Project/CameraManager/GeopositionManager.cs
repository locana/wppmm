using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Windows.Devices.Geolocation;
using Windows.Foundation;

namespace Kazyx.WPMMM.CameraManager
{
    class GeopositionManager
    {
        private static GeopositionManager _GeopositionManager = new GeopositionManager();

        internal Geoposition LatestPosition { get; set; }
        internal Action<GeopositionEventArgs> GeopositionUpdated;

        private Geolocator _Geolocator;
        private DispatcherTimer _Timer;
        private const int AcquiringInterval = 5; // min.
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
                    Start();
                }
                else
                {
                    Stop();
                }
            }
        }

        private void Stop()
        {
            _Geolocator.StatusChanged -= geolocator_StatusChanged;
            _Geolocator.PositionChanged -= geolocator_PositionChanged;
            _Timer.Stop();
            LatestPosition = null;
        }

        private void Start()
        {
            if (_Timer.IsEnabled)
            {
                return;
            }
            _Geolocator.DesiredAccuracy = PositionAccuracy.Default;
            _Geolocator.MovementThreshold = 10;
            _Geolocator.ReportInterval = 60000;
            _Geolocator.StatusChanged += geolocator_StatusChanged;
            _Geolocator.PositionChanged += geolocator_PositionChanged;
            
            Task.Factory.StartNew(async () =>
            {
                await UpdateGeoposition();
            });
            _Timer.Start();
        }

        private async void OnTimerTick(object sender, EventArgs e)
        {
            await UpdateGeoposition();
        }

        private async Task UpdateGeoposition()
        {
            Debug.WriteLine("Starting to acquire geo location");
            if (GeopositionUpdated != null)
            {
                GeopositionUpdated(new GeopositionEventArgs() { UpdatedPosition = LatestPosition, Status = GeopositiomManagerStatus.Acquiring });
            }

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
                    Debug.WriteLine("Failed due to permission problem.");
                    if (GeopositionUpdated != null)
                    {
                        GeopositionUpdated(new GeopositionEventArgs() { UpdatedPosition = null, Status = GeopositiomManagerStatus.Unauthorized });
                    }
                }
                Debug.WriteLine("Caught exception from GetGeopositionAsync");
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

            if (GeopositionUpdated != null)
            {
                if (LatestPosition == null)
                {
                    GeopositionUpdated(new GeopositionEventArgs() { UpdatedPosition = LatestPosition, Status = GeopositiomManagerStatus.Failed });
                }
                else
                {
                    GeopositionUpdated(new GeopositionEventArgs() { UpdatedPosition = LatestPosition, Status = GeopositiomManagerStatus.OK });
                }
            }
        }

        internal async Task<Geoposition> AcquireGeoPosition()
        {
            if (LatestPosition != null)
            {
                return LatestPosition;
            }
            await Task.Factory.StartNew(async () =>
            {
                await UpdateGeoposition();
            });
            return LatestPosition;
        }

        private GeopositionManager()
        {
            _Geolocator = new Geolocator();
            _Timer = new DispatcherTimer();
            _Timer.Interval = TimeSpan.FromMinutes(AcquiringInterval);
            _Timer.Tick += new EventHandler(OnTimerTick);
        }

        void geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            Debug.WriteLine("Position changed: " + args.Position.Coordinate.Latitude);
            if (GeopositionUpdated != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    GeopositionUpdated(new GeopositionEventArgs() { UpdatedPosition = LatestPosition, Status = GeopositiomManagerStatus.OK });
                });
            }
            LatestPosition = args.Position;
        }

        public void geolocator_StatusChanged(Geolocator sender, StatusChangedEventArgs args)
        {
            Debug.WriteLine("geolocator status changed: " + args.Status);
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
