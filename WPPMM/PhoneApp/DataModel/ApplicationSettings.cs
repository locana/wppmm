using System.ComponentModel;
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

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
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
