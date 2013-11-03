using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;
using WPPMM.RemoteApi;

namespace WPPMM.CameraManager
{

    public class Status : INotifyPropertyChanged
    {

        /// <summary>
        /// returnes true if it's possible to connect.
        /// (device info has got correctly)
        /// </summary>
        public bool isAvailableConnecting
        {
            get;
            set;
        }

        /// <summary>
        /// Is this phone connected to target device (after getting URL of liveview)
        /// </summary>
        public bool isConnected
        {
            get;
            set;
        }

        private bool _IsAvailableShooting = false;
        /// <summary>
        /// Is available shooting (liveview running)
        /// </summary>
        public bool IsAvailableShooting
        {
            get { return _IsAvailableShooting; }
            set
            {
                if (_IsAvailableShooting != value)
                {
                    _IsAvailableShooting = value;
                    OnPropertyChanged("ShootingProgressVisibility");
                    OnPropertyChanged("ShootButtonStatus");
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
                    OnPropertyChanged("ShootingProgressVisibility");
                    OnPropertyChanged("ShootButtonStatus");
                }
            }
        }

        private List<string> _MethodTypes = null;
        public List<String> MethodTypes
        {
            get { return (_MethodTypes == null) ? new List<string>() : _MethodTypes; }
            set
            {
                _MethodTypes = value;
                OnPropertyChanged("ShootFunctionVisibility");
                OnPropertyChanged("ZoomElementVisibility");
            }
        }

        public List<String> AvailablePostViewSize
        {
            get;
            set;
        }

        public String PostViewImageSize
        {
            get;
            set;
        }

        public void Init()
        {
            _init();
        }

        public Status()
        {
            _init();
        }

        private void _init()
        {
            isAvailableConnecting = false;
            IsAvailableShooting = false;
            isConnected = false;
            IsTakingPicture = false;
            MethodTypes = new List<string>();
            AvailablePostViewSize = new List<String>();
            PostViewImageSize = "";
        }

        public string[] AvailableApis { set; get; }
        public string CameraStatus { set; get; }
        public ZoomInfo ZoomInfo { set; get; }
        public bool LiveviewAvailable { set; get; }
        public BasicInfo<string> PostviewSizeInfo { set; get; }
        public BasicInfo<int> SelfTimerInfo { set; get; }

        private BasicInfo<string> _ShootModeInfo = new BasicInfo<string>();
        public BasicInfo<string> ShootModeInfo
        {
            set
            {
                bool changed = false;
                if (value != null && _ShootModeInfo != null)
                    changed = _ShootModeInfo.current != value.current;
                _ShootModeInfo = value;
                if (changed)
                    OnPropertyChanged("ShootButtonImage");
            }
            get { return _ShootModeInfo; }
        }

        public BasicInfo<string> ExposureMode { set; get; }
        public BasicInfo<string> ShutterSpeed { set; get; }
        public BasicInfo<string> ISOSpeedRate { set; get; }
        public BasicInfo<string> FNumber { set; get; }
        public EvInfo EvInfo { set; get; }
        public bool ProgramShiftActivated { set; get; }

        public Visibility ShootFunctionVisibility
        {
            get { return (MethodTypes.Contains("actTakePicture")) ? Visibility.Visible : Visibility.Collapsed; }
        }

        public Visibility ShootingProgressVisibility
        {
            get { return (IsTakingPicture || !IsAvailableShooting) ? Visibility.Visible : Visibility.Collapsed; }
        }

        public bool ShootButtonStatus
        {
            get { return !IsTakingPicture && IsAvailableShooting; }
        }

        private static readonly BitmapImage StillImage = new BitmapImage(new Uri("/Assets/Button/Camera.png", UriKind.Relative));
        private static readonly BitmapImage CamImage = new BitmapImage(new Uri("/Assets/Button/Camcorder.png", UriKind.Relative));

        public BitmapImage ShootButtonImage
        {
            get
            {
                if (ShootModeInfo == null || ShootModeInfo.current == null)
                {
                    return null;
                }
                switch (ShootModeInfo.current)
                {
                    case ApiParams.ShootModeStill:
                        return StillImage;
                    case ApiParams.ShootModeMovie:
                        return CamImage;
                    default:
                        return null;
                }
            }
        }

        public Visibility ZoomElementVisibility
        {
            get { return (MethodTypes.Contains("actZoom")) ? Visibility.Visible : Visibility.Collapsed; }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            //Debug.WriteLine("OnPropertyChanged: " + name);
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
