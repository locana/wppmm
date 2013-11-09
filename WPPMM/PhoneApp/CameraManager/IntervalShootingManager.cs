using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        DispatcherTimer Timer;

        public IntervalShootingManager()
        {
            IntervalTime = 5;
            _Init();
        }

        public void Init()
        {
            this.Init();
        }

        private void _Init()
        {
            Timer = new DispatcherTimer();
            ShootCount = 0;
        }

        public void Start()
        {
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
