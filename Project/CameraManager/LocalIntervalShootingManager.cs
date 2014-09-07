using Kazyx.WPMMM.Resources;
using Kazyx.WPPMM.DataModel;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace Kazyx.WPPMM.CameraManager
{
    class LocalIntervalShootingManager : INotifyPropertyChanged
    {
        private readonly AppStatus status;
        internal event Action<bool> OnIntervalRecStatusChanged;

        public LocalIntervalShootingManager(AppStatus status)
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

        public string IntervalStatusTime
        {
            get
            {
                return AppResources.IntervalTimePrefix + ": " + IntervalTime;
            }
        }

        public string IntervalStatusCount
        {
            get
            {
                return AppResources.IntervalCountPrefix + ": " + ShootCount;
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
            ShootCount = 1;
        }

        public void Start()
        {
            if (IsRunning)
            {
                return;
            }
            status.IsIntervalShootingActivated = true;
            _Init();
            ActTakePicture();
            Timer.Interval = TimeSpan.FromSeconds(IntervalTime);
            Timer.Tick += new EventHandler(RequestActTakePicture);
            Timer.Start();
            OnPropertyChanged("IntervalStatusPanelVisibility");

            if (OnIntervalRecStatusChanged != null)
            {
                OnIntervalRecStatusChanged(this.IsRunning);
            }
        }

        public void Start(int interval)
        {
            if (IsRunning)
            {
                return;
            }
            IntervalTime = interval;
            this.Start();
        }

        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }
            status.IsIntervalShootingActivated = false;
            Timer.Stop();
            OnPropertyChanged("IntervalStatusPanelVisibility");

            if (OnIntervalRecStatusChanged != null)
            {
                OnIntervalRecStatusChanged(this.IsRunning);
            }
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
            // Debug.WriteLine("OnPropertyChanged(interval): " + name);
            if (PropertyChanged != null)
            {
                try
                {
                    // Debug.WriteLine("calling OnPropertyChanged(interval): " + name);
                    Deployment.Current.Dispatcher.BeginInvoke(() => { PropertyChanged(this, new PropertyChangedEventArgs(name)); });
                }
                catch (COMException)
                {
                }
            }
        }
    }
}
