using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;
using WPPMM.RemoteApi;
using WPPMM.Utils;

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

        public bool IsSupported(string apiName)
        {
            return MethodTypes.Contains(apiName);
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
        }

        private string[] _AvailableApis;
        public string[] AvailableApis
        {
            set
            {
                _AvailableApis = value;
                if (value != null)
                    AvailableApiList = new List<string>(value);
                else
                    AvailableApiList = new List<string>();

                OnPropertyChanged("CpIsAvailableSelfTime");
                OnPropertyChanged("CpIsAvailableShootMode");
                OnPropertyChanged("CpIsAvailablePostviewSize");
            }
            get { return _AvailableApis; }
        }

        private List<string> AvailableApiList = new List<string>();

        public bool IsAvailable(string apiName)
        {
            return AvailableApiList.Contains(apiName);
        }

        private string _CameraStatus = ApiParams.EventNotReady;
        public string CameraStatus
        {
            set
            {
                if (value != _CameraStatus)
                {
                    _CameraStatus = value;
                    OnPropertyChanged("ShootButtonImage");
                    OnPropertyChanged("ShootButtonStatus");
                }
            }
            get { return _CameraStatus; }
        }

        public ZoomInfo ZoomInfo { set; get; }
        public bool LiveviewAvailable { set; get; }

        private BasicInfo<string> _PostviewSizeInfo;
        public BasicInfo<string> PostviewSizeInfo
        {
            set
            {
                _PostviewSizeInfo = value;
                OnPropertyChanged("CpCandidatesPostviewSize");
                OnPropertyChanged("CpSelectedIndexPostviewSize");
            }
            get { return _PostviewSizeInfo; }
        }

        private BasicInfo<int> _SelfTimerInfo;
        public BasicInfo<int> SelfTimerInfo
        {
            set
            {
                _SelfTimerInfo = value;
                OnPropertyChanged("CpCandidatesSelfTimer");
                OnPropertyChanged("CpSelectedIndexSelfTimer");
            }
            get { return _SelfTimerInfo; }
        }

        private BasicInfo<string> _ShootModeInfo;
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

                OnPropertyChanged("CpCandidatesShootMode");
                OnPropertyChanged("CpSelectedIndexShootMode");
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
            get
            {
                return (IsSupported("actTakePicture") || IsSupported("startMovieRec"))
                    ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Visibility ShootingProgressVisibility
        {
            get { return (IsTakingPicture || !IsAvailableShooting) ? Visibility.Visible : Visibility.Collapsed; }
        }

        public bool ShootButtonStatus
        {
            //get { return !IsTakingPicture && IsAvailableShooting; }
            get
            {
                switch (CameraStatus)
                {
                    case ApiParams.EventIdle:
                    case ApiParams.EventMvRecording:
                        return true;
                    default:
                        return false;
                }
            }
        }

        private static readonly BitmapImage StillImage = new BitmapImage(new Uri("/Assets/Button/Camera.png", UriKind.Relative));
        private static readonly BitmapImage CamImage = new BitmapImage(new Uri("/Assets/Button/Camcorder.png", UriKind.Relative));
        private static readonly BitmapImage StopImage = new BitmapImage(new Uri("/Assets/Button/Stop.png", UriKind.Relative));

        public BitmapImage ShootButtonImage
        {
            get
            {
                if (ShootModeInfo == null || ShootModeInfo.current == null || StillImage == null || CamImage == null)
                {
                    return null;
                }
                switch (ShootModeInfo.current)
                {
                    case ApiParams.ShootModeStill:
                        return StillImage;
                    case ApiParams.ShootModeMovie:
                        if (CameraStatus == ApiParams.EventMvRecording)
                            return StopImage;
                        else
                            return CamImage;
                    default:
                        return null;
                }
            }
        }

        public Visibility ZoomElementVisibility
        {
            get { return (IsSupported("actZoom")) ? Visibility.Visible : Visibility.Collapsed; }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            //Debug.WriteLine("OnPropertyChanged: " + name);
            if (PropertyChanged != null)
            {
                try
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() => { PropertyChanged(this, new PropertyChangedEventArgs(name)); });
                }
                catch (COMException)
                {
                }
            }
        }


        public int CpSelectedIndexSelfTimer
        {
            get
            {
                if (SelfTimerInfo == null) return 1;
                else return SettingsValueConverter.GetSelectedIndex(SelfTimerInfo);
            }
            set
            {
                if (SelfTimerInfo != null)
                    SelfTimerInfo.current = SelfTimerInfo.candidates[value];
            }
        }

        public string[] CpCandidatesSelfTimer
        {
            get
            {
                if (SelfTimerInfo == null) return new string[] { Resources.AppResources.Disabled };
                else return SettingsValueConverter.FromSelfTimer(SelfTimerInfo).candidates;
            }
        }

        public bool CpIsAvailableSelfTimer
        {
            get { return IsAvailable("setSelfTimer") && SelfTimerInfo != null; }
        }

        public int CpSelectedIndexPostviewSize
        {
            get
            {
                if (PostviewSizeInfo == null) return 1;
                else return SettingsValueConverter.GetSelectedIndex(PostviewSizeInfo);
            }
            set
            {
                if (PostviewSizeInfo != null)
                    PostviewSizeInfo.current = PostviewSizeInfo.candidates[value];
            }
        }

        public string[] CpCandidatesPostviewSize
        {
            get
            {
                if (PostviewSizeInfo == null) return new string[] { Resources.AppResources.Disabled };
                else return SettingsValueConverter.FromPostViewSize(PostviewSizeInfo).candidates;
            }
        }

        public bool CpIsAvailablePostviewSize
        {
            get { return IsAvailable("setPostviewImageSize") && PostviewSizeInfo != null; }
        }

        public int CpSelectedIndexShootMode
        {
            get
            {
                if (ShootModeInfo == null) return 1;
                else return SettingsValueConverter.GetSelectedIndex(ShootModeInfo);
            }
            set
            {
                if (ShootModeInfo != null)
                    ShootModeInfo.current = ShootModeInfo.candidates[value];
            }
        }

        public string[] CpCandidatesShootMode
        {
            get
            {
                if (ShootModeInfo == null) return new string[] { Resources.AppResources.Disabled };
                else return SettingsValueConverter.FromShootMode(ShootModeInfo).candidates;
            }
        }

        public bool CpIsAvailableShootMode
        {
            get { return IsAvailable("setShootMode") && ShootModeInfo != null; }
        }
    }
}
