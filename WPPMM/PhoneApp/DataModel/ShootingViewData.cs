using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WPPMM.CameraManager;
using WPPMM.RemoteApi;
using WPPMM.Utils;

namespace WPPMM.DataModel
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
                        break;
                    case "ExposureMode":
                        OnPropertyChanged("ExposureModeVisibility");
                        OnPropertyChanged("ExposureModeDisplayName");
                        OnPropertyChanged("ExposureModeImage");
                        break;
                    case "ShutterSpeed":
                        OnPropertyChanged("ShutterSpeedVisibility");
                        OnPropertyChanged("ShutterSpeedDisplayValue");
                        break;
                    case "ISOSpeedRate":
                        OnPropertyChanged("ISOVisibility");
                        OnPropertyChanged("ISODisplayValue");
                        break;
                    case "FNumber":
                        OnPropertyChanged("FnumberVisibility");
                        OnPropertyChanged("FnumberDisplayValue");
                        break;
                    case "EvInfo":
                        break;
                    case "FocusStatus":
                        OnPropertyChanged("TouchAFPointerStrokeBrush");
                        OnPropertyChanged("TouchAFPointerVisibility");
                        OnPropertyChanged("HalfPressedAFVisibility");
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

        private static readonly BitmapImage PhotoModeImage = new BitmapImage(new Uri("/Assets/Screen/mode_photo.png", UriKind.Relative));
        private static readonly BitmapImage MovieModeImage = new BitmapImage(new Uri("/Assets/Screen/mode_movie.png", UriKind.Relative));
        private static readonly BitmapImage AudioModeImage = new BitmapImage(new Uri("/Assets/Screen/mode_audio.png", UriKind.Relative));

        private static readonly BitmapImage ExModeImage_IA = new BitmapImage(new Uri("/Assets/Screen/ExposureMode_iA.png", UriKind.Relative));
        private static readonly BitmapImage ExModeImage_IAPlus = new BitmapImage(new Uri("/Assets/Screen/ExposureMode_iAPlus.png", UriKind.Relative));
        private static readonly BitmapImage ExModeImage_A = new BitmapImage(new Uri("/Assets/Screen/ExposureMode_A.png", UriKind.Relative));
        private static readonly BitmapImage ExModeImage_S = new BitmapImage(new Uri("/Assets/Screen/ExposureMode_S.png", UriKind.Relative));
        private static readonly BitmapImage ExModeImage_P = new BitmapImage(new Uri("/Assets/Screen/ExposureMode_P.png", UriKind.Relative));

        public BitmapImage ShootButtonImage
        {
            get
            {
                if (cameraStatus.ShootModeInfo == null || cameraStatus.ShootModeInfo.current == null)
                {
                    return null;
                }
                if (appStatus.IsIntervalShootingActivated)
                {
                    return StopImage;
                }

                switch (cameraStatus.ShootModeInfo.current)
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
                        case RemoteApi.FocusState.Focused:
                        case RemoteApi.FocusState.Failed:
                        case RemoteApi.FocusState.InProgress:
                            return Visibility.Visible;

                        case RemoteApi.FocusState.Released:
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
                // Debug.WriteLine("focusStatus: " + cameraStatus.FocusStatus);
                switch (cameraStatus.FocusStatus)
                {
                    case RemoteApi.FocusState.Focused:
                        return (Brush)Application.Current.Resources["PhoneAccentBrush"];
                    case RemoteApi.FocusState.Failed:
                        return (Brush)Application.Current.Resources["PhoneBackgroundBrush"];
                    case RemoteApi.FocusState.Released:
                    case RemoteApi.FocusState.InProgress:
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

                if (cameraStatus.AfType == CameraStatus.AutoFocusType.HalfPress && cameraStatus.FocusStatus == RemoteApi.FocusState.Focused)
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
                if (cameraStatus == null || cameraStatus.ExposureMode == null || cameraStatus.ExposureMode.current == null) { return Visibility.Collapsed; }
                else { return Visibility.Visible; }
            }
        }

        public String ExposureModeDisplayName
        {
            get
            {
                if (cameraStatus == null || cameraStatus.ExposureMode == null || cameraStatus.ExposureMode.current == null)
                {
                    return "-";
                }
                else
                {
                    switch (cameraStatus.ExposureMode.current)
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

        public Visibility ShutterSpeedVisibility
        {
            get
            {
                if (cameraStatus == null || cameraStatus.ShutterSpeed == null || cameraStatus.ShutterSpeed.current == null) { return Visibility.Collapsed; }
                else { return Visibility.Visible; }
            }
        }

        public String ShutterSpeedDisplayValue
        {
            get
            {
                if (cameraStatus == null || cameraStatus.ShutterSpeed == null || cameraStatus.ShutterSpeed.current == null)
                {
                    return "--";
                }
                else
                {
                    return cameraStatus.ShutterSpeed.current;
                }
            }
        }

        public Visibility ISOVisibility
        {
            get
            {
                if (cameraStatus == null || cameraStatus.ISOSpeedRate == null || cameraStatus.ISOSpeedRate.current == null) { return Visibility.Collapsed; }
                else { return Visibility.Visible; }
            }
        }

        public string ISODisplayValue
        {
            get
            {
                if (cameraStatus == null || cameraStatus.ISOSpeedRate == null || cameraStatus.ISOSpeedRate.current == null) { return "ISO: --"; }
                else { return "ISO: " + cameraStatus.ISOSpeedRate.current; }
            }
        }

        public Visibility FnumberVisibility
        {
            get
            {
                if (cameraStatus == null || cameraStatus.FNumber == null || cameraStatus.FNumber.current == null) { return Visibility.Collapsed; }
                else { return Visibility.Visible; }
            }
        }

        public string FnumberDisplayValue
        {
            get
            {
                if (cameraStatus == null || cameraStatus.FNumber == null || cameraStatus.FNumber.current == null) { return "F--"; }
                else { return "F" + cameraStatus.FNumber.current; }
            }
        }

        public BitmapImage ModeImage
        {
            get
            {
                if (cameraStatus.ShootModeInfo == null || cameraStatus.ShootModeInfo.current == null)
                {
                    return null;
                }

                switch (cameraStatus.ShootModeInfo.current)
                {
                    case ShootModeParam.Still:
                        return PhotoModeImage;
                    case ShootModeParam.Movie:
                        return MovieModeImage;
                    case ShootModeParam.Audio:
                        return AudioModeImage;
                    default:
                        return null;
                }
            }
        }

        public BitmapImage ExposureModeImage
        {
            get
            {
                if (cameraStatus == null || cameraStatus.ExposureMode == null || cameraStatus.ExposureMode.current == null)
                {
                    return null;
                }
                else
                {
                    switch (cameraStatus.ExposureMode.current)
                    {

                        case ExposureMode.Aperture:
                            return ExModeImage_A;
                        case ExposureMode.SS:
                            return ExModeImage_S;
                        case ExposureMode.Program:
                            return ExModeImage_P;
                        case ExposureMode.Intelligent:
                            return ExModeImage_IA;
                        case ExposureMode.Superior:
                            return ExModeImage_IAPlus;
                        default:
                            return null;
                    }
                }
            }
        }

    }
}
