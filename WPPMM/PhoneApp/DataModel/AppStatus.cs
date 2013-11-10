using System.ComponentModel;
using System.Windows;

namespace WPPMM.DataModel
{
    public class AppStatus : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() => { PropertyChanged(this, new PropertyChangedEventArgs(name)); });
            }
        }

        private static AppStatus sStatus = new AppStatus();

        public static AppStatus GetInstance()
        {
            return sStatus;
        }

        public void Init()
        {
            IsTryingToConnectLiveview = false;
            IsSearchingDevice = false;
            IsTakingPicture = false;
            IsIntervalShootingActivated = false;
        }

        private bool _IsTryingToConnectLieview = false;
        public bool IsTryingToConnectLiveview
        {
            get { return _IsTryingToConnectLieview; }
            set
            {
                if (_IsTryingToConnectLieview != value)
                {
                    _IsTryingToConnectLieview = value;
                    OnPropertyChanged("IsTryingToConnectLiveview");
                }
            }
        }

        private bool _IsSearchingDevice = false;
        public bool IsSearchingDevice
        {
            get { return _IsSearchingDevice; }
            set
            {
                if (_IsSearchingDevice != value)
                {
                    _IsSearchingDevice = value;
                    OnPropertyChanged("IsSearchingDevice");
                }
            }
        }

        private bool _IsTakingPicture = false;
        /// <summary>
        /// true during taking picture
        /// </summary>
        public bool IsTakingPicture
        {
            get { return _IsTakingPicture; }
            set
            {
                if (_IsTakingPicture != value)
                {
                    _IsTakingPicture = value;
                    OnPropertyChanged("IsTakingPicture");
                }
            }
        }

        private bool _IsIntervalShootingActivated = false;
        public bool IsIntervalShootingActivated
        {
            set
            {
                if (_IsIntervalShootingActivated != value)
                {
                    _IsIntervalShootingActivated = value;
                    OnPropertyChanged("IsIntervalShootingActivated");
                }
            }
            get
            {
                return _IsIntervalShootingActivated;
            }
        }
    }
}
