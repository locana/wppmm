using System;
using System.Windows.Threading;

namespace WPPMM.CameraManager
{
    class IntervalShootingManager
    {

        public Action ActTakePicture
        {
            get;
            set;
        }

        public int IntervalTime
        {
            get;
            set;
        }

        public int ShootCount
        {
            get;
            set;
        }

        public bool IsRunning
        {
            get
            {
                if (Timer == null)
                {
                    return false;
                }
                return Timer.IsEnabled;
            }
        }

        private DispatcherTimer Timer;

        public IntervalShootingManager()
        {
            IntervalTime = 5;
            _Init();
        }

        public void Init()
        {
            _Init();
        }

        private void _Init()
        {
            Timer = new DispatcherTimer();
            ShootCount = 0;
        }

        public void Start()
        {
            if (IsRunning)
            {
                return;
            }
            CameraManager.GetInstance().cameraStatus.IsIntervalShootingActivated = true;
            Timer.Interval = TimeSpan.FromSeconds(IntervalTime);
            Timer.Tick += new EventHandler(RequestActTakePicture);
            Timer.Start();
        }

        public void Start(int interval)
        {
            IntervalTime = interval;
            this.Start();
        }

        public void Stop()
        {
            CameraManager.GetInstance().cameraStatus.IsIntervalShootingActivated = false;
            Timer.Stop();
        }

        public void RequestActTakePicture(object sender, EventArgs e)
        {
            if (ActTakePicture != null)
            {
                ActTakePicture();
            }
        }
    }
}
