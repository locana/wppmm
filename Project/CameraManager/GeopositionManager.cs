using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

        private Geolocator geolocator;
        private DispatcherTimer Timer;
        private const int AcquiringInterval = 5; // min.
        private const int MaximumAge = 1; // min.
        private const int Timeout = 20; // sec.

        private bool _Enable = false;
        internal bool Enable
        {
            get { return _Enable; }
            set
            {
                _Enable = value;
                if (value)
                {
                    UpdateGeoposition(new object(), new EventArgs());
                    Timer.Interval = TimeSpan.FromMinutes(AcquiringInterval);
                    Timer.Tick += new EventHandler(UpdateGeoposition);
                    Timer.Start();
                }
                else
                {
                    LatestPosition = null;
                    Timer.Stop();
                }
            }
        }

        private async void UpdateGeoposition(object sender, EventArgs e)
        {
            await _UpdateGeoposition();
        }

        private async Task _UpdateGeoposition()
        {
            Debug.WriteLine("Starting to acquire geo location");
            if (GeopositionUpdated != null)
            {
                GeopositionUpdated(new GeopositionEventArgs() { UpdatedPosition = LatestPosition, Status = GeopositiomManagerStatus.Acquiring });
            }

            IAsyncOperation<Geoposition> locationTask = null;

            try
            {
                locationTask = geolocator.GetGeopositionAsync(
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
                    //  in case of location is turned off.
                    // todo: show error message
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
            await _UpdateGeoposition();
            return LatestPosition;
        }

        private GeopositionManager()
        {
            geolocator = new Geolocator();
            Timer = new DispatcherTimer();
            geolocator.DesiredAccuracy = PositionAccuracy.Default;
            geolocator.MovementThreshold = 10;
            geolocator.StatusChanged += geolocator_StatusChanged;
            geolocator.PositionChanged += geolocator_PositionChanged;
        }

        void geolocator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            Debug.WriteLine("Position changed: " + args.Position.Coordinate.Latitude);
            if (GeopositionUpdated != null && LatestPosition != null)
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
        Idle,
        Failed,
        Acquiring,
        OK,
    };
}
