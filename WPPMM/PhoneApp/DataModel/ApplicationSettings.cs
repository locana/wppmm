using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using WPPMM.Utils;

namespace WPPMM.DataModel
{
    public class ApplicationSettings : INotifyPropertyChanged
    {
        private static ApplicationSettings sSettings = new ApplicationSettings();

        private ApplicationSettings()
        {
            IsPostviewTransferEnabled = Preference.IsPostviewTransferEnabled();
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
                    OnPropertyChanged("SelectedIndexPostviewTransferEnabled");
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
                    OnPropertyChanged("IsIntervalShootingEnabled");
                    OnPropertyChanged("SelectedIndexIntervalShootingEnabled");
                }
            }
            get
            {
                return _IsIntervalShootingEnabled;
            }
        }

        private int _IntervalTime = 5;

        public int IntervalTime
        {
            set
            {
                if (_IntervalTime != value)
                {
                    Preference.SetIntervalTime(value);
                    _IntervalTime = value;
                    Debug.WriteLine("IntervalTime changed: " + value);
                    OnPropertyChanged("IntervalTime");
                    OnPropertyChanged("SelectedIntervalTime");
                }
            }
            get
            {
                return _IntervalTime;
            }
        }


        public string[] CandidatesPostviewTransferEnabled
        {
            get
            {
                return new string[] { Resources.AppResources.On, Resources.AppResources.Off };
            }
        }

        public int SelectedIndexPostviewTransferEnabled
        {
            get
            {
                return IsPostviewTransferEnabled ? 0 : 1;
            }
        }

        public string[] CandidatesIntervalShootingEnabled
        {
            get
            {
                return new string[] { Resources.AppResources.On, Resources.AppResources.Off };
            }
        }

        public int SelectedIndexIntervalShootingEnabled
        {
            get
            {
                return IsIntervalShootingEnabled ? 0 : 1;
            }
        }

        public string[] CandidatesIntervalTime
        {
            get {
                return new string[] { "5", "7", "10", "15", "20", "30" };
            }
        }

        public int SelectedIntervalTime
        {
            get
            {
                int ret = 0;
                switch (IntervalTime)
                {
                    case 5:
                        return 0;
                    case 7:
                        return 1;
                    case 10:
                        return 2;
                    case 15:
                        return 3;
                    case 20:
                        return 4;
                    case 30:
                        return 5;
                    default:
                        return ret;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            Debug.WriteLine("OnProperty changed: " + name);
            if (PropertyChanged != null)
            {
                try
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(name));
                }
                catch (COMException)
                {
                }
            }
        }
    }
}
