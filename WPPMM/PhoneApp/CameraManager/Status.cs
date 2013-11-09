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

        private bool _IsTryingToConnectLieview = false;
        public bool IsTryingToConnectLiveview
        {
            get { return _IsTryingToConnectLieview; }
            set
            {
                if (_IsTryingToConnectLieview != value)
                {
                    _IsTryingToConnectLieview = value;
                    OnPropertyChanged("ShootingProgressVisibility");
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
                    OnPropertyChanged("ShootingProgressVisibility");
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

        private bool _IsIntervalShootingActivated = false;
        public bool IsIntervalShootingActivated
        {
            set
            {
                if (_IsIntervalShootingActivated != value)
                {
                    _IsIntervalShootingActivated = value;
                    OnPropertyChanged("ShootButtonImage");
                    OnPropertyChanged("ShootButtonStatus");
                }
            }
            get
            {
                return _IsIntervalShootingActivated;
            }
        }

        private bool _IsToastVisible = false;
        public bool IsToastVisible
        {
            get { return _IsToastVisible; }
            set
            {
                if (_IsToastVisible != value)
                {
                    _IsToastVisible = value;
                    OnPropertyChanged("ToastVisibility");
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
            InitEventParams();
        }

        private void _init()
        {
            isAvailableConnecting = false;
            IsTryingToConnectLiveview = false;
            IsTakingPicture = false;
            MethodTypes = new List<string>();
        }

        public void InitEventParams()
        {
            ProgramShiftActivated = false;
            EvInfo = null;
            ISOSpeedRate = null;
            ShutterSpeed = null;
            ExposureMode = null;
            ShootModeInfo = null;
            SelfTimerInfo = null;
            PostviewSizeInfo = null;
            IsLiveviewAvailable = false;
            ZoomInfo = null;
            CameraStatus = ApiParams.EventNotReady;
            AvailableApis = null;
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

                OnPropertyChanged("CpIsAvailableSelfTimer");
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

        private bool _IsLiveviewAvailable = false;
        public bool IsLiveviewAvailable
        {
            set
            {
                if (_IsLiveviewAvailable != value)
                {
                    _IsLiveviewAvailable = value;
                    if (LiveviewAvailabilityNotifier != null)
                    {
                        LiveviewAvailabilityNotifier.Invoke(value);
                    }
                }
            }
            get
            {
                return _IsLiveviewAvailable;
            }
        }
        public Action<bool> LiveviewAvailabilityNotifier;

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
                _ShootModeInfo = value;
                OnPropertyChanged("ShootButtonImage");
                OnPropertyChanged("CpCandidatesShootMode");
                OnPropertyChanged("CpSelectedIndexShootMode");
                if (value != null && value.current != null & CurrentShootModeNotifier != null)
                {
                    CurrentShootModeNotifier.Invoke(value.current);
                }
            }
            get { return _ShootModeInfo; }
        }

        public Action<string> CurrentShootModeNotifier;

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
                return (IsSupported("actTakePicture") || IsSupported("startMovieRec") || IsSupported("startAudioRec"))
                    ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Visibility ShootingProgressVisibility
        {
            get { return (IsTakingPicture || IsTryingToConnectLiveview || IsSearchingDevice) ? Visibility.Visible : Visibility.Collapsed; }
        }

        public bool ShootButtonStatus
        {
            get
            {
                if (IsIntervalShootingActivated)
                {
                    return true;
                }
                if (IsTakingPicture)
                {
                    return false;
                }

                switch (CameraStatus)
                {
                    case ApiParams.EventIdle:
                    case ApiParams.EventMvRecording:
                    case ApiParams.EventAuRecording:
                        return true;
                    default:
                        return false;
                }
            }
        }

        private static readonly BitmapImage StillImage = new BitmapImage(new Uri("/Assets/Button/Camera.png", UriKind.Relative));
        private static readonly BitmapImage CamImage = new BitmapImage(new Uri("/Assets/Button/Camcorder.png", UriKind.Relative));
        private static readonly BitmapImage AudioImage = new BitmapImage(new Uri("/Assets/Button/Music.png", UriKind.Relative));
        private static readonly BitmapImage StopImage = new BitmapImage(new Uri("/Assets/Button/Stop.png", UriKind.Relative));

        public BitmapImage ShootButtonImage
        {
            get
            {
                if (ShootModeInfo == null || ShootModeInfo.current == null)
                {
                    return null;
                }
                if (IsIntervalShootingActivated)
                {
                    return StopImage;
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
                    case ApiParams.ShootModeAudio:
                        if (CameraStatus == ApiParams.EventAuRecording)
                            return StopImage;
                        else
                            return AudioImage;
                    default:
                        return null;
                }
            }
        }

        public Visibility ZoomElementVisibility
        {
            get { return (IsSupported("actZoom")) ? Visibility.Visible : Visibility.Collapsed; }
        }

        public Visibility ToastVisibility
        {
            set
            {
                if (value == Visibility.Collapsed)
                {
                    _IsToastVisible = false;
                }
                else if (value == Visibility.Visible)
                {
                    _IsToastVisible = true;
                }
            }
            get { return IsToastVisible ? Visibility.Visible : Visibility.Collapsed; }
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
                return SettingsValueConverter.GetSelectedIndex(SelfTimerInfo);
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
                return SettingsValueConverter.FromSelfTimer(SelfTimerInfo).candidates;
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
                return SettingsValueConverter.GetSelectedIndex(PostviewSizeInfo);
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
                return SettingsValueConverter.FromPostViewSize(PostviewSizeInfo).candidates;
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
                return SettingsValueConverter.GetSelectedIndex(ShootModeInfo);
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
                return SettingsValueConverter.FromShootMode(ShootModeInfo).candidates;
            }
        }

        public bool CpIsAvailableShootMode
        {
            get { return IsAvailable("setShootMode") && ShootModeInfo != null; }
        }
    }
}
