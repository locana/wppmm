using Kazyx.RemoteApi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace Kazyx.WPPMM.CameraManager
{
    public class CameraStatus : INotifyPropertyChanged
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

        private ServerVersion version = ServerVersion.CreateDefault();

        public ServerVersion Version
        {
            set
            {
                version = value;
                OnPropertyChanged("IsRestrictedApiVisible");
                OnPropertyChanged("AvailableApis");
            }
            get
            {
                if (version == null)
                {
                    version = ServerVersion.CreateDefault();
                }
                return version;
            }
        }

        public Visibility IsRestrictedApiVisible
        {
            get { return Version.IsLiberated ? Visibility.Visible : Visibility.Collapsed; }
        }

        public enum AutoFocusType
        {
            None,
            HalfPress,
            Touch,
        }

        private Dictionary<string, List<string>> _SupportedApis = null;
        public Dictionary<string, List<string>> SupportedApis
        {
            get { return (_SupportedApis == null) ? new Dictionary<string, List<string>>() : _SupportedApis; }
            set
            {
                _SupportedApis = value;
                OnPropertyChanged("MethodTypes");
            }
        }

        public bool IsSupported(string apiName)
        {
            return SupportedApis.ContainsKey(apiName);
        }

        public bool IsSupported(string apiName, string version)
        {
            return SupportedApis.ContainsKey(apiName) && SupportedApis[apiName].Contains(version);
        }

        public void Init()
        {
            _init();
        }

        public CameraStatus()
        {
            _init();
            InitEventParams();
        }

        private void _init()
        {
            isAvailableConnecting = false;
            SupportedApis = new Dictionary<string, List<string>>();
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
            FocusStatus = null;
            IsLiveviewAvailable = false;
            ZoomInfo = null;
            Status = EventParam.NotReady;
            AvailableApis = null;
            AfType = AutoFocusType.None;
        }

        static readonly IEnumerable<string> RestrictedApiSet =
            new string[]{
                "actHalfPressShutter",
                "setExposureCompensation",
                "setTouchAFPosition",
                "setExposureMode",
                "setFNumber",
                "setShutterSpeed",
                "setIsoSpeedRate",
                "setWhiteBalance",
                "setStillSize",
                "setBeepMode",
                "setMovieQuality",
                "setViewAngle",
                "setSteadyMode",
                "setCurrentTime",
            };

        public bool IsRestrictedApi(string apiName)
        {
            foreach (var api in RestrictedApiSet)
            {
                if (apiName == api)
                {
                    return true;
                }
            }
            return false;
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

                OnPropertyChanged("AvailableApis");
            }
            get { return _AvailableApis; }
        }

        private List<string> AvailableApiList = new List<string>();

        public bool IsAvailable(string apiName)
        {
            if (!Version.IsLiberated && IsRestrictedApi(apiName))
            {
                return false;
            }
            return AvailableApiList.Contains(apiName);
        }

        private string _Status = EventParam.NotReady;
        public string Status
        {
            set
            {
                if (value != _Status)
                {
                    _Status = value;
                    OnPropertyChanged("Status");
                }
            }
            get { return _Status; }
        }

        private ZoomInfo _ZoomInfo = null;
        public ZoomInfo ZoomInfo
        {
            set
            {
                _ZoomInfo = value;
                OnPropertyChanged("ZoomInfo");
            }
            get { return _ZoomInfo; }
        }

        private bool _IsLiveviewAvailable = false;
        public bool IsLiveviewAvailable
        {
            set
            {
                Debug.WriteLine("isLiveViewAvailableSet: " + value);
                if (_IsLiveviewAvailable != value)
                {
                    OnPropertyChanged("IsLiveviewAvailable");
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

        private Capability<string> _PostviewSizeInfo;
        public Capability<string> PostviewSizeInfo
        {
            set
            {
                _PostviewSizeInfo = value;
                OnPropertyChanged("PostviewSizeInfo");
            }
            get { return _PostviewSizeInfo; }
        }

        private Capability<int> _SelfTimerInfo;
        public Capability<int> SelfTimerInfo
        {
            set
            {
                _SelfTimerInfo = value;
                OnPropertyChanged("SelfTimerInfo");
            }
            get { return _SelfTimerInfo; }
        }

        private ExtendedInfo<string> _ShootModeInfo;
        public ExtendedInfo<string> ShootModeInfo
        {
            set
            {
                string previous = null;
                if (_ShootModeInfo != null)
                {
                    previous = _ShootModeInfo.current;
                }
                _ShootModeInfo = value;
                if (_ShootModeInfo != null)
                {
                    _ShootModeInfo.previous = previous;
                }
                OnPropertyChanged("ShootModeInfo");
                OnPropertyChanged("LiveviewScreenVisibility");
                OnPropertyChanged("AudioScreenVisibility");

                if (value != null && value.current != null & CurrentShootModeNotifier != null)
                {
                    CurrentShootModeNotifier.Invoke(value.current);
                }
            }
            get { return _ShootModeInfo; }
        }

        private Capability<string> _ExposureMode;
        public Capability<string> ExposureMode
        {
            set
            {
                _ExposureMode = value;
                OnPropertyChanged("ExposureMode");
            }
            get { return _ExposureMode; }
        }

        private Capability<string> _ShutterSpeed;
        public Capability<string> ShutterSpeed
        {
            set
            {
                _ShutterSpeed = value;
                OnPropertyChanged("ShutterSpeed");
            }
            get { return _ShutterSpeed; }
        }

        private Capability<string> _ISOSpeedRate;
        public Capability<string> ISOSpeedRate
        {
            set
            {
                _ISOSpeedRate = value;
                OnPropertyChanged("ISOSpeedRate");
            }
            get { return _ISOSpeedRate; }
        }

        private Capability<string> _FNumber;
        public Capability<string> FNumber
        {
            set
            {
                _FNumber = value;
                OnPropertyChanged("FNumber");
            }
            get { return _FNumber; }
        }

        private Capability<string> _BeepMode;
        public Capability<string> BeepMode
        {
            set
            {
                _BeepMode = value;
                OnPropertyChanged("BeepMode");
            }
            get { return _BeepMode; }
        }

        private Capability<string> _SteadyMode;
        public Capability<string> SteadyMode
        {
            set
            {
                _SteadyMode = value;
                OnPropertyChanged("SteadyMode");
            }
            get { return _SteadyMode; }
        }

        private Capability<int> _ViewAngle;
        public Capability<int> ViewAngle
        {
            set
            {
                _ViewAngle = value;
                OnPropertyChanged("ViewAngle");
            }
            get { return _ViewAngle; }
        }

        private Capability<string> _MovieQuality;
        public Capability<string> MovieQuality
        {
            set
            {
                _MovieQuality = value;
                OnPropertyChanged("MovieQuality");
            }
            get { return _MovieQuality; }
        }

        private EvCapability _EvInfo;
        public EvCapability EvInfo
        {
            set
            {
                _EvInfo = value;
                OnPropertyChanged("EvInfo");
            }
            get { return _EvInfo; }
        }

        private StorageInfo[] _Storages;
        public StorageInfo[] Storages
        {
            set
            {
                _Storages = value;
                OnPropertyChanged("Storages");
            }
            get { return _Storages; }
        }

        private string _LiveviewOrientation;
        public string LiveviewOrientation
        {
            set
            {
                _LiveviewOrientation = value;
                OnPropertyChanged("LiveviewOrientation");
            }
            get { return _LiveviewOrientation == null ? Orientation.Straight : _LiveviewOrientation; }
        }

        private string[] _PictureUrls;
        public string[] PictureUrls
        {
            set
            {
                _PictureUrls = value;
                OnPropertyChanged("PictureUrls");
            }
            get { return _PictureUrls; }
        }

        public bool ProgramShiftActivated { set; get; }

        public void ClearFocusStatus()
        {
            FocusStatus = FocusState.Released;
        }

        private string _FocusStatus;
        public string FocusStatus
        {
            set
            {
                _FocusStatus = value;
                OnPropertyChanged("FocusStatus");
            }
            get { return _FocusStatus; }
        }


        public AutoFocusType AfType { get; set; }

        public Visibility LiveviewScreenVisibility
        {
            get
            {
                if (_ShootModeInfo == null)
                {
                    return Visibility.Collapsed;
                }

                if (_ShootModeInfo.current == ShootModeParam.Audio)
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
        }

        public Visibility AudioScreenVisibility
        {
            get
            {
                if (_ShootModeInfo == null)
                {
                    return Visibility.Collapsed;
                }

                if (_ShootModeInfo.current == ShootModeParam.Audio)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        public Action<string> CurrentShootModeNotifier;


        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {

            // Debug.WriteLine("OnPropertyChanged: " + name);
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
    }

    public class ExtendedInfo<T> : Capability<T>
    {
        public T previous { set; get; }

        public ExtendedInfo(Capability<T> basic)
        {
            this.candidates = basic.candidates;
            this.current = basic.current;
        }

        public ExtendedInfo(Capability<T> basic, T previous)
        {
            this.candidates = basic.candidates;
            this.current = basic.current;
            this.previous = previous;
        }
    }
}
