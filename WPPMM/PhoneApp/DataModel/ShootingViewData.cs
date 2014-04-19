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
                        OnPropertyChanged("ShootButtonImage");
                        break;
                    case "ExposureMode":
                        OnPropertyChanged("ExposureModeVisibility");
                        OnPropertyChanged("ExposureModeDisplayName");
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
                    case "": // todo:
                        OnPropertyChanged("TouchAFPointerStrokeBrush");
                        OnPropertyChanged("TouchAFPointerVisibility");
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
                });
            }
        }

        public Visibility ShootFunctionVisibility
        {
            get
            {
                return (cameraStatus.IsSupported("actTakePicture") || cameraStatus.IsSupported("startMovieRec") || cameraStatus.IsSupported("startAudioRec"))
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
            get { return (cameraStatus.IsSupported("actZoom")) ? Visibility.Visible : Visibility.Collapsed; }
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
                if (cameraStatus.IsAvailable("setTouchAFPosition"))
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
            get {
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

        public Brush TouchAFPointerStrokeBrush
        {
            get { return (Brush)Application.Current.Resources["PhoneForegroundBrush"]; }
        }
    }
}
