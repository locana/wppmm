using Kazyx.RemoteApi;
using Kazyx.WPPMM.CameraManager;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Kazyx.WPPMM.DataModel
{
    public class ShootingViewData : INotifyPropertyChanged
    {
        private readonly AppStatus appStatus;

        private readonly CameraStatus cameraStatus;

        public ShootingViewData(AppStatus aStatus, CameraStatus cStatus)
        {
            this.appStatus = aStatus;
            appStatus.PropertyChanged += (sender, e) =>
            {
                switch (e.PropertyName)
                {
                    case "IsTryingToConnectLiveview":
                        OnPropertyChanged("ShootingProgressVisibility");
                        break;
                    case "IsSearchingDevice":
                        OnPropertyChanged("ShootingProgressVisibility");
                        break;
                    case "IsTakingPicture":
                        OnPropertyChanged("ShootingProgressVisibility");
                        OnPropertyChanged("ShootButtonStatus");
                        break;
                    case "IsIntervalShootingActivated":
                        OnPropertyChanged("ShootButtonImage");
                        OnPropertyChanged("ShootButtonStatus");
                        break;
                }
            };
            this.cameraStatus = cStatus;
            cStatus.PropertyChanged += (sender, e) =>
            {
                switch (e.PropertyName)
                {
                    case "MethodTypes":
                    case "AvailableApis":
                        OnPropertyChanged("ShootFunctionVisibility");
                        OnPropertyChanged("ZoomElementVisibility");
                        OnPropertyChanged("ShutterSpeedVisibility");
                        OnPropertyChanged("ShutterSpeedDisplayValue");
                        OnPropertyChanged("ISOVisibility");
                        OnPropertyChanged("ISODisplayValue");
                        OnPropertyChanged("FnumberVisibility");
                        OnPropertyChanged("FnumberDisplayValue");
                        OnPropertyChanged("EvVisibility");
                        OnPropertyChanged("FNumberSliderVisibility");
                        OnPropertyChanged("ShutterSpeedSliderVisibility");
                        OnPropertyChanged("FNumberBrush");
                        OnPropertyChanged("ShutterSpeedBrush");
                        OnPropertyChanged("EvBrush");
                        OnPropertyChanged("IsoBrush");
                        OnPropertyChanged("IsoSliderVisibility");
                        OnPropertyChanged("EvSliderVisibility");
                        OnPropertyChanged("SlidersVisibility");
                        OnPropertyChanged("SliderButtonVisibility");
                        break;
                    case "Status":
                        OnPropertyChanged("ShootButtonImage");
                        OnPropertyChanged("ShootButtonStatus");
                        OnPropertyChanged("RecordingStatusVisibility");
                        OnPropertyChanged("TouchAFPointerVisibility");
                        break;
                    case "ShootModeInfo":
                        OnPropertyChanged("ShootFunctionVisibility");
                        OnPropertyChanged("ShootButtonImage");
                        OnPropertyChanged("ModeImage");
                        OnPropertyChanged("ExposureModeImage");
                        break;
                    case "ExposureMode":
                        OnPropertyChanged("ModeImage");
                        OnPropertyChanged("ExposureModeImage");
                        OnPropertyChanged("ShutterSpeedVisibility");
                        OnPropertyChanged("ShutterSpeedDisplayValue");
                        OnPropertyChanged("ISOVisibility");
                        OnPropertyChanged("ISODisplayValue");
                        OnPropertyChanged("FnumberVisibility");
                        OnPropertyChanged("FnumberDisplayValue");
                        OnPropertyChanged("SlidersVisibility");
                        break;
                    case "ShutterSpeed":
                        OnPropertyChanged("ShutterSpeedVisibility");
                        OnPropertyChanged("ShutterSpeedDisplayValue");
                        OnPropertyChanged("MaxShutterSpeedLabel");
                        OnPropertyChanged("MinShutterSpeedLabel");
                        if (cameraStatus.IsAvailable("setShutterSpeed"))
                        {
                            OnPropertyChanged("MaxShutterSpeedIndex");
                            OnPropertyChanged("CurrentShutterSpeedIndex");
                        }
                        OnPropertyChanged("SlidersVisibility");
                        break;
                    case "ISOSpeedRate":
                        OnPropertyChanged("ISOVisibility");
                        OnPropertyChanged("ISODisplayValue");
                        OnPropertyChanged("MinIsoLabel");
                        OnPropertyChanged("MaxIsoLabel");
                        if (cameraStatus.IsAvailable("setIsoSpeedRate"))
                        {
                            OnPropertyChanged("MaxIsoIndex");
                            OnPropertyChanged("CurrentIsoIndex");
                        }
                        OnPropertyChanged("SlidersVisibility");
                        break;
                    case "FNumber":
                        OnPropertyChanged("FnumberVisibility");
                        OnPropertyChanged("FnumberDisplayValue");
                        OnPropertyChanged("MaxFNumberLabel");
                        OnPropertyChanged("MinFNumberLabel");
                        if (cameraStatus.IsAvailable("setFNumber"))
                        {
                            OnPropertyChanged("MaxFNumberIndex");
                            OnPropertyChanged("CurrentFNumberIndex");
                        }
                        OnPropertyChanged("SlidersVisibility");
                        break;
                    case "EvInfo":
                        OnPropertyChanged("EvVisibility");
                        OnPropertyChanged("EvDisplayValue");
                        OnPropertyChanged("MinEvLabel");
                        OnPropertyChanged("MaxEvLabel");
                        if (cameraStatus.IsAvailable("setExposureCompensation"))
                        {
                            OnPropertyChanged("MinEvIndex");
                            OnPropertyChanged("MaxEvIndex");
                            OnPropertyChanged("CurrentEvIndex");
                        }
                        OnPropertyChanged("SlidersVisibility");
                        break;
                    case "FocusStatus":
                        OnPropertyChanged("TouchAFPointerStrokeBrush");
                        OnPropertyChanged("TouchAFPointerVisibility");
                        OnPropertyChanged("HalfPressedAFVisibility");
                        break;
                    case "Storages":
                        OnPropertyChanged("StorageImage");
                        OnPropertyChanged("RecordbaleAmount");
                        break;
                }
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
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
                        Debug.WriteLine("Caught COMException: ShootingViewData");
                    }
                    catch (NullReferenceException e)
                    {
                        Debug.WriteLine(e.StackTrace);
                    }
                });
            }
        }

        public Visibility ShootFunctionVisibility
        {
            get
            {
                return ((cameraStatus.IsSupported("actTakePicture") || cameraStatus.IsSupported("startMovieRec") || cameraStatus.IsSupported("startAudioRec")) && ShootButtonImage != null)
                    ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Visibility ShootingProgressVisibility
        {
            get
            {
                return (appStatus.IsTakingPicture || appStatus.IsTryingToConnectLiveview || appStatus.IsSearchingDevice)
                    ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public bool ShootButtonStatus
        {
            get
            {
                if (appStatus.IsIntervalShootingActivated)
                {
                    return true;
                }
                if (appStatus.IsTakingPicture)
                {
                    return false;
                }

                switch (cameraStatus.Status)
                {
                    case EventParam.Idle:
                    case EventParam.MvRecording:
                    case EventParam.AuRecording:
                    case EventParam.ItvRecording:
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
        private static readonly BitmapImage IntervalStillImage = new BitmapImage(new Uri("/Assets/Button/IntervalStillRecButton.png", UriKind.Relative));

        private static readonly BitmapImage PhotoModeImage = new BitmapImage(new Uri("/Assets/Screen/mode_photo.png", UriKind.Relative));
        private static readonly BitmapImage MovieModeImage = new BitmapImage(new Uri("/Assets/Screen/mode_movie.png", UriKind.Relative));
        private static readonly BitmapImage AudioModeImage = new BitmapImage(new Uri("/Assets/Screen/mode_audio.png", UriKind.Relative));
        private static readonly BitmapImage IntervalStillModeImage = new BitmapImage(new Uri("/Assets/Screen/mode_interval.png", UriKind.Relative));

        private static readonly BitmapImage ExModeImage_IA = new BitmapImage(new Uri("/Assets/Screen/ExposureMode_iA.png", UriKind.Relative));
        private static readonly BitmapImage ExModeImage_IAPlus = new BitmapImage(new Uri("/Assets/Screen/ExposureMode_iAPlus.png", UriKind.Relative));
        private static readonly BitmapImage ExModeImage_A = new BitmapImage(new Uri("/Assets/Screen/ExposureMode_A.png", UriKind.Relative));
        private static readonly BitmapImage ExModeImage_S = new BitmapImage(new Uri("/Assets/Screen/ExposureMode_S.png", UriKind.Relative));
        private static readonly BitmapImage ExModeImage_P = new BitmapImage(new Uri("/Assets/Screen/ExposureMode_P.png", UriKind.Relative));
        private static readonly BitmapImage ExModeImage_M = new BitmapImage(new Uri("/Assets/Screen/ExposureMode_M.png", UriKind.Relative));

        private static readonly BitmapImage MemoryCard = new BitmapImage(new Uri("/Assets/Screen/memory_card.png", UriKind.Relative));
        private static readonly BitmapImage NoMemoryCard = new BitmapImage(new Uri("/Assets/Screen/no_memory_card.png", UriKind.Relative));

        public BitmapImage ShootButtonImage
        {
            get
            {
                if (cameraStatus.ShootModeInfo == null || cameraStatus.ShootModeInfo.Current == null)
                {
                    return null;
                }
                if (appStatus.IsIntervalShootingActivated)
                {
                    return StopImage;
                }

                switch (cameraStatus.ShootModeInfo.Current)
                {
                    case ShootModeParam.Still:
                        return StillImage;
                    case ShootModeParam.Movie:
                        if (cameraStatus.Status == EventParam.MvRecording)
                            return StopImage;
                        else
                            return CamImage;
                    case ShootModeParam.Audio:
                        if (cameraStatus.Status == EventParam.AuRecording)
                            return StopImage;
                        else
                            return AudioImage;
                    case ShootModeParam.Interval:
                        if (cameraStatus.Status == EventParam.ItvRecording)
                            return StopImage;
                        else
                        return IntervalStillImage;
                    default:
                        return null;
                }
            }
        }

        public Visibility RecordingStatusVisibility
        {
            get
            {
                if (cameraStatus.Status == EventParam.MvRecording || cameraStatus.Status == EventParam.AuRecording)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        public Visibility ZoomElementVisibility
        {
            get { return (cameraStatus.IsAvailable("actZoom")) ? Visibility.Visible : Visibility.Collapsed; }
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

        public Visibility TouchAFPointerVisibility
        {
            get
            {
                if (cameraStatus == null || cameraStatus.FocusStatus == null || cameraStatus.AfType != CameraStatus.AutoFocusType.Touch)
                {
                    return Visibility.Collapsed;
                }

                Debug.WriteLine("V focusStatus: " + cameraStatus.FocusStatus);

                if (cameraStatus.IsAvailable("setTouchAFPosition"))
                {
                    switch (cameraStatus.FocusStatus)
                    {
                        case FocusState.Focused:
                        case FocusState.Failed:
                        case FocusState.InProgress:
                            return Visibility.Visible;

                        case FocusState.Released:
                        default:
                            return Visibility.Collapsed;
                    }
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        public Brush TouchAFPointerStrokeBrush
        {
            get
            {
                if (cameraStatus == null || cameraStatus.FocusStatus == null)
                {
                    return (Brush)Application.Current.Resources["PhoneForegroundBrush"];
                }
                Debug.WriteLine("focusStatus: " + cameraStatus.FocusStatus);
                switch (cameraStatus.FocusStatus)
                {
                    case FocusState.Focused:
                        return (Brush)Application.Current.Resources["PhoneAccentBrush"];
                    case FocusState.Failed:
                        return (Brush)Application.Current.Resources["PhoneBackgroundBrush"];
                    case FocusState.Released:
                    case FocusState.InProgress:
                    default:
                        return (Brush)Application.Current.Resources["PhoneForegroundBrush"];
                }
            }
        }

        public Visibility HalfPressedAFVisibility
        {
            get
            {
                if (cameraStatus == null || cameraStatus.FocusStatus == null)
                {
                    return Visibility.Collapsed;
                }

                if (cameraStatus.AfType == CameraStatus.AutoFocusType.HalfPress && cameraStatus.FocusStatus == FocusState.Focused)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        public Visibility ExposureModeVisibility
        {
            get
            {
                if (cameraStatus == null || cameraStatus.ExposureMode == null || cameraStatus.ExposureMode.Current == null) { return Visibility.Collapsed; }
                else { return Visibility.Visible; }
            }
        }

        public String ExposureModeDisplayName
        {
            get
            {
                if (cameraStatus == null || cameraStatus.ExposureMode == null || cameraStatus.ExposureMode.Current == null)
                {
                    return "-";
                }
                else
                {
                    switch (cameraStatus.ExposureMode.Current)
                    {

                        case ExposureMode.Aperture:
                            return "A";
                        case ExposureMode.SS:
                            return "S";
                        case ExposureMode.Program:
                            return "P";
                        case ExposureMode.Intelligent:
                            return "iAuto";
                        case ExposureMode.Superior:
                            return "iAuto+";
                        default:
                            return "-";
                    }
                }
            }

        }

        public Visibility SlidersVisibility
        {
            get
            {
                if (ShutterSpeedVisibility == Visibility.Visible || ISOVisibility == Visibility.Visible || FnumberVisibility == Visibility.Visible || EvVisibility == Visibility.Visible)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        public Visibility SliderButtonVisibility
        {
            get
            {
                if (cameraStatus != null && cameraStatus.Version.IsLiberated)
                {
                    if (ShutterSpeedSliderVisibility == Visibility.Visible || IsoSliderVisibility == Visibility.Visible || FNumberSliderVisibility == Visibility.Visible || EvSliderVisibility == Visibility.Visible)
                    {
                        return Visibility.Visible;
                    }
                }
                return Visibility.Collapsed;
            }
        }

        public Visibility ShutterSpeedVisibility
        {
            get
            {
                if (cameraStatus == null || cameraStatus.ShutterSpeed == null || cameraStatus.ShutterSpeed.Current == null || !cameraStatus.IsAvailable("getShutterSpeed")) { return Visibility.Collapsed; }
                else { return Visibility.Visible; }
            }
        }

        public String ShutterSpeedDisplayValue
        {
            get
            {
                if (cameraStatus == null || cameraStatus.ShutterSpeed == null || cameraStatus.ShutterSpeed.Current == null)
                {
                    return "--";
                }
                else
                {
                    return cameraStatus.ShutterSpeed.Current;
                }
            }
        }

        public Visibility ISOVisibility
        {
            get
            {
                if (cameraStatus == null || cameraStatus.ISOSpeedRate == null || cameraStatus.ISOSpeedRate.Current == null || !cameraStatus.IsAvailable("getIsoSpeedRate")) { return Visibility.Collapsed; }
                else { return Visibility.Visible; }
            }
        }

        public string ISODisplayValue
        {
            get
            {
                if (cameraStatus == null || cameraStatus.ISOSpeedRate == null || cameraStatus.ISOSpeedRate.Current == null) { return "ISO: --"; }
                else { return "ISO " + cameraStatus.ISOSpeedRate.Current; }
            }
        }

        public Visibility FnumberVisibility
        {
            get
            {
                if (cameraStatus == null || cameraStatus.FNumber == null || cameraStatus.FNumber.Current == null || !cameraStatus.IsAvailable("getFNumber")) { return Visibility.Collapsed; }
                else { return Visibility.Visible; }
            }
        }

        public string FnumberDisplayValue
        {
            get
            {
                if (cameraStatus == null || cameraStatus.FNumber == null || cameraStatus.FNumber.Current == null) { return "F--"; }
                else { return "F" + cameraStatus.FNumber.Current; }
            }
        }

        public Visibility EvVisibility
        {
            get
            {
                if (cameraStatus == null || cameraStatus.EvInfo == null || !cameraStatus.IsAvailable("getExposureCompensation")) { return Visibility.Collapsed; }
                else { return Visibility.Visible; }
            }
        }

        public string EvDisplayValue
        {
            get
            {
                if (cameraStatus == null || cameraStatus.EvInfo == null)
                {
                    return "";
                }
                else
                {
                    var value = EvConverter.GetEv(cameraStatus.EvInfo.CurrentIndex, cameraStatus.EvInfo.Candidate.IndexStep);
                    var strValue = Math.Round(value, 1, MidpointRounding.AwayFromZero).ToString("0.0");

                    if (value < 0)
                    {
                        return "EV " + strValue;
                    }
                    else if (value == 0.0f)
                    {
                        return "EV " + strValue;
                    }
                    else
                    {
                        return "EV +" + strValue;
                    }
                }
            }
        }

        public BitmapImage ModeImage
        {
            get
            {
                if (cameraStatus == null || cameraStatus.ShootModeInfo == null || cameraStatus.ShootModeInfo.Current == null)
                {
                    return null;
                }

                switch (cameraStatus.ShootModeInfo.Current)
                {
                    case ShootModeParam.Still:
                        return PhotoModeImage;
                    case ShootModeParam.Movie:
                        return MovieModeImage;
                    case ShootModeParam.Audio:
                        return AudioModeImage;
                    case ShootModeParam.Interval:
                        return IntervalStillModeImage;
                    default:
                        return null;
                }
            }
        }

        public BitmapImage ExposureModeImage
        {
            get
            {
                if (cameraStatus == null || cameraStatus.ShootModeInfo == null || cameraStatus.ShootModeInfo.Current != ShootModeParam.Still)
                {
                    return null;
                }
                if (cameraStatus == null || cameraStatus.ExposureMode == null || cameraStatus.ExposureMode.Current == null)
                {
                    return null;
                }
                switch (cameraStatus.ExposureMode.Current)
                {
                    case ExposureMode.Aperture:
                        return ExModeImage_A;
                    case ExposureMode.SS:
                        return ExModeImage_S;
                    case ExposureMode.Program:
                        return ExModeImage_P;
                    case ExposureMode.Manual:
                        return ExModeImage_M;
                    case ExposureMode.Intelligent:
                        return ExModeImage_IA;
                    case ExposureMode.Superior:
                        return ExModeImage_IAPlus;
                    default:
                        return null;
                }
            }
        }

        public BitmapImage StorageImage
        {
            get
            {
                if (cameraStatus == null || cameraStatus.Storages == null)
                {
                    return null;
                }

                foreach (StorageInfo storage in cameraStatus.Storages)
                {
                    if (storage.RecordTarget)
                    {
                        switch (storage.StorageID)
                        {
                            case "No Media":
                                return NoMemoryCard;
                            case "Memory Card 1":
                            default:
                                return MemoryCard;
                        }
                    }
                }
                return NoMemoryCard;
            }
        }

        public Visibility FNumberSliderVisibility
        {
            get
            {
                if (cameraStatus == null || !cameraStatus.IsAvailable("setFNumber"))
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
        }

        public Visibility ShutterSpeedSliderVisibility
        {
            get
            {
                if (cameraStatus == null || !cameraStatus.IsAvailable("setShutterSpeed"))
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
        }

        public Visibility EvSliderVisibility
        {
            get
            {
                if (cameraStatus == null || !cameraStatus.IsAvailable("setExposureCompensation")) { return Visibility.Collapsed; }
                else { return Visibility.Visible; }
            }
        }

        public Visibility IsoSliderVisibility
        {
            get
            {
                if (cameraStatus == null || !cameraStatus.IsAvailable("setIsoSpeedRate")) { return Visibility.Collapsed; }
                else { return Visibility.Visible; }
            }
        }

        public Brush FNumberBrush
        {
            get
            {
                if (cameraStatus == null || !cameraStatus.IsAvailable("setFNumber"))
                {
                    return (Brush)Application.Current.Resources["PhoneForegroundBrush"];
                }
                else
                {
                    return (Brush)Application.Current.Resources["PhoneAccentBrush"];
                }
            }
        }

        public Brush ShutterSpeedBrush
        {
            get
            {
                if (cameraStatus == null || !cameraStatus.IsAvailable("setShutterSpeed"))
                {
                    return (Brush)Application.Current.Resources["PhoneForegroundBrush"];
                }
                else
                {
                    return (Brush)Application.Current.Resources["PhoneAccentBrush"];
                }
            }
        }

        public Brush EvBrush
        {
            get
            {
                if (cameraStatus == null || !cameraStatus.IsAvailable("setExposureCompensation"))
                {
                    return (Brush)Application.Current.Resources["PhoneForegroundBrush"];
                }
                else
                {
                    return (Brush)Application.Current.Resources["PhoneAccentBrush"];
                }
            }
        }

        public Brush IsoBrush
        {
            get
            {
                if (cameraStatus == null || !cameraStatus.IsAvailable("setIsoSpeedRate"))
                {
                    return (Brush)Application.Current.Resources["PhoneForegroundBrush"];
                }
                else
                {
                    return (Brush)Application.Current.Resources["PhoneAccentBrush"];
                }
            }
        }

        public int MaxFNumberIndex
        {
            get
            {
                if (cameraStatus == null || cameraStatus.FNumber == null)
                {
                    return 0;
                }
                return cameraStatus.FNumber.Candidates.Length - 1;
            }
        }

        public int CurrentFNumberIndex
        {
            get
            {
                if (cameraStatus == null || cameraStatus.FNumber == null)
                {
                    return 0;
                }

                for (int i = 0; i < cameraStatus.FNumber.Candidates.Length; i++)
                {
                    if (cameraStatus.FNumber.Current == cameraStatus.FNumber.Candidates[i])
                    {
                        return i;
                    }
                }
                return 0;
            }
        }

        public string MaxFNumberLabel
        {
            get
            {
                if (cameraStatus == null || cameraStatus.FNumber == null || cameraStatus.FNumber.Candidates.Length == 0) { return ""; }
                else { return cameraStatus.FNumber.Candidates[cameraStatus.FNumber.Candidates.Length - 1]; }
            }
        }

        public string MinFNumberLabel
        {
            get
            {
                if (cameraStatus == null || cameraStatus.FNumber == null || cameraStatus.FNumber.Candidates.Length == 0) { return ""; }
                else { return cameraStatus.FNumber.Candidates[0]; }
            }
        }

        public int MaxShutterSpeedIndex
        {
            get
            {
                if (cameraStatus == null || cameraStatus.ShutterSpeed == null)
                {
                    return 0;
                }
                return cameraStatus.ShutterSpeed.Candidates.Length - 1;
            }
        }

        public int CurrentShutterSpeedIndex
        {
            get
            {
                if (cameraStatus == null || cameraStatus.ShutterSpeed == null)
                {
                    return 0;
                }

                for (int i = 0; i < cameraStatus.ShutterSpeed.Candidates.Length; i++)
                {
                    if (cameraStatus.ShutterSpeed.Current == cameraStatus.ShutterSpeed.Candidates[i])
                    {
                        return i;
                    }
                }
                return 0;
            }
        }

        public string MaxShutterSpeedLabel
        {
            get
            {
                if (cameraStatus == null || cameraStatus.ShutterSpeed == null || cameraStatus.ShutterSpeed.Candidates.Length == 0) { return ""; }
                else { return cameraStatus.ShutterSpeed.Candidates[cameraStatus.ShutterSpeed.Candidates.Length - 1]; }
            }
        }

        public string MinShutterSpeedLabel
        {
            get
            {
                if (cameraStatus == null || cameraStatus.ShutterSpeed == null || cameraStatus.ShutterSpeed.Candidates.Length == 0) { return ""; }
                else { return cameraStatus.ShutterSpeed.Candidates[0]; }
            }
        }

        public int MaxEvIndex
        {
            get
            {
                if (cameraStatus == null || cameraStatus.EvInfo == null || !cameraStatus.IsSupported("setExposureCompensation")) { return 0; }
                return cameraStatus.EvInfo.Candidate.MaxIndex;
            }
        }

        public int MinEvIndex
        {
            get
            {
                if (cameraStatus == null || cameraStatus.EvInfo == null || !cameraStatus.IsSupported("setExposureCompensation")) { return 0; }
                return cameraStatus.EvInfo.Candidate.MinIndex;
            }
        }

        public string MaxEvLabel
        {
            get
            {
                if (cameraStatus == null || cameraStatus.EvInfo == null || !cameraStatus.IsSupported("setExposureCompensation")) { return ""; }
                var value = EvConverter.GetEv(cameraStatus.EvInfo.Candidate.MaxIndex, cameraStatus.EvInfo.Candidate.IndexStep);
                return Math.Round(value, 1, MidpointRounding.AwayFromZero).ToString("0.0");
            }
        }

        public string MinEvLabel
        {
            get
            {
                if (cameraStatus == null || cameraStatus.EvInfo == null || !cameraStatus.IsSupported("setExposureCompensation")) { return ""; }
                var value = EvConverter.GetEv(cameraStatus.EvInfo.Candidate.MinIndex, cameraStatus.EvInfo.Candidate.IndexStep);
                return Math.Round(value, 1, MidpointRounding.AwayFromZero).ToString("0.0");
            }
        }

        public int CurrentEvIndex
        {
            get
            {
                if (cameraStatus == null || cameraStatus.EvInfo == null || !cameraStatus.IsSupported("setExposureCompensation")) { return 0; }
                return cameraStatus.EvInfo.CurrentIndex;
            }
            set
            {
                if (cameraStatus.EvInfo != null)
                {
                    if (value <= cameraStatus.EvInfo.Candidate.MaxIndex && value >= cameraStatus.EvInfo.Candidate.MinIndex)
                    {
                        cameraStatus.EvInfo.CurrentIndex = value;
                    }
                    else
                    {
                        cameraStatus.EvInfo.CurrentIndex = 0;
                    }
                }
            }
        }

        public int MaxIsoIndex
        {
            get
            {
                if (cameraStatus == null || cameraStatus.ISOSpeedRate == null || !cameraStatus.IsAvailable("setIsoSpeedRate")) { return 0; }
                return cameraStatus.ISOSpeedRate.Candidates.Length - 1;
            }
        }

        public int CurrentIsoIndex
        {
            get
            {
                if (cameraStatus == null || cameraStatus.ISOSpeedRate == null || !cameraStatus.IsAvailable("setIsoSpeedRate")) { return 0; }
                for (int i = 0; i < cameraStatus.ISOSpeedRate.Candidates.Length; i++)
                {
                    if (cameraStatus.ISOSpeedRate.Current == cameraStatus.ISOSpeedRate.Candidates[i])
                    {
                        return i;
                    }
                }
                return 0;
            }
        }

        public string MaxIsoLabel
        {
            get
            {
                if (cameraStatus == null || cameraStatus.ISOSpeedRate == null || cameraStatus.ISOSpeedRate.Candidates.Length == 0) { return ""; }
                else return cameraStatus.ISOSpeedRate.Candidates[cameraStatus.ISOSpeedRate.Candidates.Length - 1];
            }
        }

        public string MinIsoLabel
        {
            get
            {
                if (cameraStatus == null || cameraStatus.ISOSpeedRate == null || cameraStatus.ISOSpeedRate.Candidates.Length == 0) { return ""; }
                else return cameraStatus.ISOSpeedRate.Candidates[0];
            }
        }

        public string RecordbaleAmount
        {
            get
            {
                if (cameraStatus == null || cameraStatus.Storages == null || cameraStatus.ShootModeInfo == null || cameraStatus.ShootModeInfo.Current == null)
                {
                    return "";
                }
                foreach (StorageInfo storage in cameraStatus.Storages)
                {
                    if (storage.RecordTarget)
                    {
                        switch (cameraStatus.ShootModeInfo.Current)
                        {
                            case ShootModeParam.Still:
                            case ShootModeParam.Interval:
                                if (storage.RecordableImages == -1) { return ""; }
                                return storage.RecordableImages.ToString();
                            case ShootModeParam.Movie:
                            case ShootModeParam.Audio:
                                if (storage.RecordableMovieLength == -1) { return ""; }
                                return storage.RecordableMovieLength.ToString();
                            default:
                                break;
                        }
                    }
                }
                return "";
            }
        }
    }
}
