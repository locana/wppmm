using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using WPPMM.Utils;
using WPPMM.CameraManager;
using WPPMM.RemoteApi;
using System.Windows;

namespace WPPMM.DataModel
{
    public class ApplicationSettings : INotifyPropertyChanged
    {
        private static ApplicationSettings sSettings = new ApplicationSettings();
        private CameraManager.CameraManager manager;


        private ApplicationSettings()
        {
            manager = CameraManager.CameraManager.GetInstance();
            IsPostviewTransferEnabled = Preference.IsPostviewTransferEnabled();
            IsIntervalShootingEnabled = Preference.IsIntervalShootingEnabled();
            IntervalTime = Preference.IntervalTime();
            IsShootButtonDisplayed = Preference.IsShootButtonDisplayed();
        }

        public static ApplicationSettings GetInstance()
        {
            return sSettings;
        }

        private bool _IsPostviewTransferEnabled = true;

        public bool IsPostviewTransferEnabled
        {
            set
            {
                if (_IsPostviewTransferEnabled != value)
                {
                    Preference.SetPostviewTransferEnabled(value);
                    _IsPostviewTransferEnabled = value;
                    OnPropertyChanged("IsPostviewTransferEnabled");
                }
            }
            get
            {
                return _IsPostviewTransferEnabled;
            }
        }

        private bool _IsIntervalShootingEnabled = false;

        public bool IsIntervalShootingEnabled
        {
            set
            {
                if (_IsIntervalShootingEnabled != value)
                {
                    Preference.SetIntervalShootingEnabled(value);
                    _IsIntervalShootingEnabled = value;

                    // exclusion
                    if (value)
                    {
                        if (manager.cameraStatus.IsAvailable("setSelfTimer") && manager.IntervalManager != null)
                        {
                            manager.SetSelfTimerAsync(SelfTimerParam.Off);
                        }
                    }
                }
            }
            get
            {
                return _IsIntervalShootingEnabled;
            }
        }

        private int _IntervalTime = 10;

        public int IntervalTime
        {
            set
            {
                if (_IntervalTime != value)
                {
                    Preference.SetIntervalTime(value);
                    _IntervalTime = value;
                    // Debug.WriteLine("IntervalTime changed: " + value);
                    OnPropertyChanged("IntervalTime");
                }
            }
            get
            {
                return _IntervalTime;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            // Debug.WriteLine("OnProperty changed: " + name);
            if (PropertyChanged != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(name));
                    }
                    catch (COMException)
                    {
                        Debug.WriteLine("Caught COMException: ApplicationSettings");
                    }
                });
            }
        }

        private bool _IsShootButtonDisplayed = false;
        public bool IsShootButtonDisplayed
        {
            set
            {
                if (_IsShootButtonDisplayed != value)
                {
                    Preference.SetShootButtonDisplayed(value);
                    _IsShootButtonDisplayed = value;
                    OnPropertyChanged("ShootButtonVisibility");
                    Debug.WriteLine("ShootbuttonVisibility updated: " + value.ToString());
                }
            }
            get
            {
                return _IsShootButtonDisplayed;
            }
        }

        public Visibility ShootButtonVisibility
        {
            get
            {
                if (_IsShootButtonDisplayed)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }
    }
}
