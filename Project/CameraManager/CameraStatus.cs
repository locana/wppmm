using Kazyx.RemoteApi;
using Kazyx.RemoteApi.Camera;
using Kazyx.WPPMM.CameraManager;
using Kazyx.WPPMM.Utils;
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

        private DeviceType _DeviceType = DeviceType.UNDEFINED;
        public DeviceType DeviceType
        {
            set { _DeviceType = value; }
            get { return _DeviceType; }
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
                if (DeviceType == DeviceType.DSC_QX10) // QX10 firmware v3.00 has bug in the response of getMethodTypes.
                {
                    value.Remove("setFocusMode");
                }
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
            ShootMode = null;
            SelfTimer = null;
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
                DebugUtil.Log("isLiveViewAvailableSet: " + value);
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
                OnPropertyChanged("PostviewSize");
            }
            get { return _PostviewSizeInfo; }
        }

        private Capability<int> _SelfTimer;
        public Capability<int> SelfTimer
        {
            set
            {
                _SelfTimer = value;
                OnPropertyChanged("SelfTimer");
            }
            get { return _SelfTimer; }
        }

        private ExtendedInfo<string> _ShootMode;
        public ExtendedInfo<string> ShootMode
        {
            set
            {
                string previous = null;
                if (_ShootMode != null)
                {
                    previous = _ShootMode.Current;
                }
                _ShootMode = value;
                if (_ShootMode != null)
                {
                    _ShootMode.previous = previous;
                }
                OnPropertyChanged("ShootMode");
                OnPropertyChanged("LiveviewScreenVisibility");
                OnPropertyChanged("AudioScreenVisibility");

                if (value != null && value.Current != null & CurrentShootModeNotifier != null)
                {
                    CurrentShootModeNotifier.Invoke(value.Current);
                }
            }
            get { return _ShootMode; }
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

        private Capability<StillImageSize> _StillSize;
        public Capability<StillImageSize> StillImageSize
        {
            set
            {
                _StillSize = value;
                OnPropertyChanged("StillImageSize");
            }
            get { return _StillSize; }
        }

        private Capability<string> _FlashMode;
        public Capability<string> FlashMode
        {
            set
            {
                _FlashMode = value;
                OnPropertyChanged("FlashMode");
            }
            get
            {
                return _FlashMode;
            }
        }

        private Capability<string> _FocusMode;
        public Capability<string> FocusMode
        {
            set
            {
                _FocusMode = value;
                OnPropertyChanged("FocusMode");
            }
            get
            {
                return _FocusMode;
            }
        }

        private TouchFocusStatus _TouchFocusStatus;
        public TouchFocusStatus TouchFocusStatus
        {
            set
            {
                _TouchFocusStatus = value;
                OnPropertyChanged("TouchFocusStatus");
            }
            get
            {
                return _TouchFocusStatus;
            }
        }

        private Capability<string> _WhiteBalance;
        public Capability<string> WhiteBalance
        {
            set
            {
                _WhiteBalance = value;
                OnPropertyChanged("WhiteBalance");
                OnPropertyChanged("ColorTemperture");
            }
            get { return _WhiteBalance; }
        }

        private int _ColorTemperture = -1;
        public int ColorTemperture
        {
            set
            {
                _ColorTemperture = value;
                OnPropertyChanged("ColorTemperture");
            }
            get { return _ColorTemperture; }
        }

        private Dictionary<string, int[]> _ColorTempertureCandidates;
        public Dictionary<string, int[]> ColorTempertureCandidates
        {
            set
            {
                _ColorTempertureCandidates = value;
                OnPropertyChanged("ColorTemperture");
            }
            get { return _ColorTempertureCandidates; }
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

        private bool _ProgramShiftActivated = false;
        public bool ProgramShiftActivated
        {
            set
            {
                _ProgramShiftActivated = value;
                OnPropertyChanged("ProgramShiftActivated");
            }
            get { return _ProgramShiftActivated; }
        }

        private ProgramShiftRange _ProgramShiftRange;
        public ProgramShiftRange ProgramShiftRange
        {
            set
            {
                _ProgramShiftRange = value;
                OnPropertyChanged("ProgramShiftRange");
            }
            get
            {
                return _ProgramShiftRange;
            }
        }

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

        private Capability<string> _ZoomSetting;
        public Capability<string> ZoomSetting
        {
            set
            {
                _ZoomSetting = value;
                OnPropertyChanged("ZoomSetting");
            }
            get { return _ZoomSetting; }
        }
        private Capability<string> _StillQuality;
        public Capability<string> StillQuality
        {
            set
            {
                _StillQuality = value;
                OnPropertyChanged("StillQuality");
            }
            get { return _StillQuality; }
        }
        private Capability<string> _ContShootingMode;
        public Capability<string> ContShootingMode
        {
            set
            {
                _ContShootingMode = value;
                OnPropertyChanged("ContShootingMode");
            }
            get { return _ContShootingMode; }
        }
        private Capability<string> _ContShootingSpeed;
        public Capability<string> ContShootingSpeed
        {
            set
            {
                _ContShootingSpeed = value;
                OnPropertyChanged("ContShootingSpeed");
            }
            get { return _ContShootingSpeed; }
        }
        private List<ContShootingResult> _ContShootingResult;
        public List<ContShootingResult> ContShootingResult
        {
            set
            {
                _ContShootingResult = value;
                OnPropertyChanged("ContShootingResult");
            }
            get { return _ContShootingResult; }
        }
        private Capability<string> _FlipMode;
        public Capability<string> FlipMode
        {
            set
            {
                _FlipMode = value;
                OnPropertyChanged("FlipMode");
            }
            get { return _FlipMode; }
        }
        private Capability<string> _SceneSelection;
        public Capability<string> SceneSelection
        {
            set
            {
                _SceneSelection = value;
                OnPropertyChanged("SceneSelection");
            }
            get { return _SceneSelection; }
        }
        private Capability<string> _IntervalTime;
        public Capability<string> IntervalTime
        {
            set
            {
                _IntervalTime = value;
                OnPropertyChanged("IntervalTime");
            }
            get { return _IntervalTime; }
        }
        private Capability<string> _ColorSetting;
        public Capability<string> ColorSetting
        {
            set
            {
                _ColorSetting = value;
                OnPropertyChanged("ColorSetting");
            }
            get { return _ColorSetting; }
        }
        private Capability<string> _MovieFormat;
        public Capability<string> MovieFormat
        {
            set
            {
                _MovieFormat = value;
                OnPropertyChanged("MovieFormat");
            }
            get { return _MovieFormat; }
        }
        private Capability<string> _InfraredRemoteControl;
        public Capability<string> InfraredRemoteControl
        {
            set
            {
                _InfraredRemoteControl = value;
                OnPropertyChanged("InfraredRemoteControl");
            }
            get { return _InfraredRemoteControl; }
        }
        private Capability<string> _TvColorSystem;
        public Capability<string> TvColorSystem
        {
            set
            {
                _TvColorSystem = value;
                OnPropertyChanged("TvColorSystem");
            }
            get { return _TvColorSystem; }
        }
        private string _TrackingFocusStatus;
        public string TrackingFocusStatus
        {
            set
            {
                _TrackingFocusStatus = value;
                OnPropertyChanged("TrackingFocusStatus");
            }
            get { return _TrackingFocusStatus; }
        }
        private Capability<string> _TrackingFocus;
        public Capability<string> TrackingFocus
        {
            set
            {
                _TrackingFocus = value;
                OnPropertyChanged("TrackingFocus");
            }
            get { return _TrackingFocus; }
        }
        private List<BatteryInfo> _BatteryInfo;
        public List<BatteryInfo> BatteryInfo
        {
            set
            {
                _BatteryInfo = value;
                OnPropertyChanged("BatteryInfo");
            }
            get { return _BatteryInfo; }
        }
        private int _RecordingTimeSec;
        public int RecordingTimeSec
        {
            set
            {
                _RecordingTimeSec = value;
                OnPropertyChanged("RecordingTimeSec");
            }
            get { return _RecordingTimeSec; }
        }
        private int _NumberOfShots;
        public int NumberOfShots
        {
            set
            {
                _NumberOfShots = value;
                OnPropertyChanged("NumberOfShots");
            }
            get { return _NumberOfShots; }
        }
        private Capability<int> _AutoPowerOff;
        public Capability<int> AutoPowerOff
        {
            set
            {
                _AutoPowerOff = value;
                OnPropertyChanged("AutoPowerOff");
            }
            get { return _AutoPowerOff; }
        }

        public Visibility LiveviewScreenVisibility
        {
            get
            {
                if (_ShootMode == null)
                {
                    return Visibility.Collapsed;
                }

                if (_ShootMode.Current == ShootModeParam.Audio)
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
                if (_ShootMode == null)
                {
                    return Visibility.Collapsed;
                }

                if (_ShootMode.Current == ShootModeParam.Audio)
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

            // DebugUtil.Log("OnPropertyChanged: " + name);
            try
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(name));
                    }
                });
            }
            catch (COMException)
            {
            }
        }
    }

    public class ExtendedInfo<T> : Capability<T>
    {
        public T previous { set; get; }

        public ExtendedInfo(Capability<T> basic)
        {
            this.Candidates = basic.Candidates;
            this.Current = basic.Current;
        }

        public ExtendedInfo(Capability<T> basic, T previous)
        {
            this.Candidates = basic.Candidates;
            this.Current = basic.Current;
            this.previous = previous;
        }
    }
}
