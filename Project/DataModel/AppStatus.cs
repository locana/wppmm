using Kazyx.WPMMM.Utils;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace Kazyx.WPPMM.DataModel
{
    public class AppStatus : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(name));
                    }
                }
                catch (COMException)
                {
                    DebugUtil.Log("Caught COMException: AppStatus");
                }
            });
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
            IsDownloadingImages = false;
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


        private bool _IsInShootingDisplay = false;
        public bool IsInShootingDisplay
        {
            set
            {
                _IsInShootingDisplay = value;
            }
            get { return _IsInShootingDisplay; }
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

        private bool _IsDownloadingImages = false;
        public bool IsDownloadingImages
        {
            set
            {
                if (_IsDownloadingImages != value)
                {
                    _IsDownloadingImages = value;
                    OnPropertyChanged("IsDownloadingImages");
                }
            }
            get { return _IsDownloadingImages; }
        }
    }
}
