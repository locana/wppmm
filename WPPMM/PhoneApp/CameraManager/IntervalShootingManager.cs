using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using WPPMM.DataModel;

namespace WPPMM.CameraManager
{
    class IntervalShootingManager : INotifyPropertyChanged
    {
        private readonly AppStatus status;

        public IntervalShootingManager(AppStatus status)
        {
            this.status = status;
            _Init();
        }

        public Action ActTakePicture
        {
            get;
            set;
        }

        private int _IntervalTime = 5;
        public int IntervalTime
        {
            get { return _IntervalTime; }
            set
            {
                if (value != _IntervalTime)
                {
                    _IntervalTime = value;
                    OnPropertyChanged("IntervalStatusTime");
                }
            }
        }

        private int _ShootCount = 0;
        public int ShootCount
        {
            get { return _ShootCount; }
            set
            {
                if (value != _ShootCount)
                {
                    _ShootCount = value;
                    OnPropertyChanged("IntervalStatusCount");
                }
            }
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

        public Visibility IntervalStatusPanelVisibility
        {
            get
            {
                Debug.WriteLine("get interval panel visibility: " + this.IsRunning);
                return this.IsRunning ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public String IntervalStatusTime
        {
            get
            {
                return "Interval (sec.): " + IntervalTime;
            }
        }

        public String IntervalStatusCount
        {
            get
            {
                return "Count: " + ShootCount;
            }
        }

        private DispatcherTimer Timer;

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
            status.IsIntervalShootingActivated = true;
            _Init();
            Timer.Interval = TimeSpan.FromSeconds(IntervalTime);
            Timer.Tick += new EventHandler(RequestActTakePicture);
            Timer.Start();
            OnPropertyChanged("IntervalStatusPanelVisibility");
        }

        public void Start(int interval)
        {
            IntervalTime = interval;
            this.Start();
        }

        public void Stop()
        {
            status.IsIntervalShootingActivated = false;
            Timer.Stop();
            OnPropertyChanged("IntervalStatusPanelVisibility");
        }

        public void RequestActTakePicture(object sender, EventArgs e)
        {
            if (ActTakePicture != null)
            {
                ActTakePicture();
                ShootCount++;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            Debug.WriteLine("OnPropertyChanged(interval): " + name);
            if (PropertyChanged != null)
            {
                try
                {
                    Debug.WriteLine("calling OnPropertyChanged(interval): " + name);
                    Deployment.Current.Dispatcher.BeginInvoke(() => { PropertyChanged(this, new PropertyChangedEventArgs(name)); });
                }
                catch (COMException)
                {
                }
            }
        }
    }
}
