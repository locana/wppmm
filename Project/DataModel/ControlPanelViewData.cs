using Kazyx.RemoteApi;
using Kazyx.WPPMM.CameraManager;
using Kazyx.WPPMM.Utils;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace Kazyx.WPPMM.DataModel
{
    public class ControlPanelViewData : INotifyPropertyChanged
    {
        private readonly CameraStatus status;
        private readonly CameraManager.CameraManager manager = CameraManager.CameraManager.GetInstance();
        private readonly ApplicationSettings setting = ApplicationSettings.GetInstance();

        public ControlPanelViewData(CameraStatus status)
        {
            this.status = status;

            status.PropertyChanged += (sender, e) =>
            {
                switch (e.PropertyName)
                {
                    case "AvailableApis":
                        OnPropertyChanged("CpIsAvailableSelfTimer");
                        OnPropertyChanged("CpIsAvailableShootMode");
                        OnPropertyChanged("CpIsAvailablePostviewSize");
                        OnPropertyChanged("CpIsAvailableStillImageFunctions");
                        OnPropertyChanged("CpIsAvailableExposureMode");
                        OnPropertyChanged("CpIsAvailableExposureCompensation");
                        OnPropertyChanged("CpIsAvailableBeepMode");
                        OnPropertyChanged("CpIsAvailableSteadyMode");
                        OnPropertyChanged("CpIsAvailableViewAngle");
                        OnPropertyChanged("CpIsAvailableMovieQuality");
                        OnPropertyChanged("CpIsAvailableStillImageSize");
                        OnPropertyChanged("CpIsAvailableWhiteBalance");
                        OnPropertyChanged("CpIsVisibleColorTemperture");
                        OnPropertyChanged("CpDisplayValueExposureCompensation");
                        OnPropertyChanged("CpSelectedIndexExposureCompensation");
                        break;
                    case "ShootMode":
                        OnPropertyChanged("CpCandidatesShootMode");
                        OnPropertyChanged("CpSelectedIndexShootMode");
                        OnPropertyChanged("CpIsAvailableStillImageFunctions");
                        break;
                    case "PostviewSize":
                    case "SelfTimer":
                    case "ExposureMode":
                    case "BeepMode":
                    case "SteadyMode":
                    case "ViewAngle":
                    case "MovieQuality":
                    case "StillImageSize":
                    case "WhiteBalance":
                        GenericPropertyChanged(e.PropertyName);
                        break;
                    case "ColorTemperture":
                        OnPropertyChanged("CpIsVisibleColorTemperture");
                        break;
                    default:
                        break;
                }
            };

            setting.PropertyChanged += (sender, e) =>
            {
                switch (e.PropertyName)
                {
                    case "IsIntervalShootingEnabled":
                        GenericPropertyChanged("SelfTimer");
                        break;
                    default:
                        break;
                }
            };
        }

        private void GenericPropertyChanged(string name)
        {
            OnPropertyChanged("CpCandidates" + name);
            OnPropertyChanged("CpSelectedIndex" + name);
            OnPropertyChanged("CpIsAvailable" + name);
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
                        Debug.WriteLine("Caught COMException: ControlPanelViewData");
                    }
                    catch (NullReferenceException)
                    {
                        Debug.WriteLine("Caught NullReferenceException: ControlPanelViewData");
                    }
                    catch (InvalidOperationException e)
                    {
                        Debug.WriteLine(e.StackTrace);
                    }
                });
            }
        }

        public int CpSelectedIndexSelfTimer
        {
            get
            {
                return SettingsValueConverter.GetSelectedIndex(status.SelfTimerInfo);
            }
            set
            {
                SetSelectedAsCurrent(status.SelfTimerInfo, value);
            }
        }

        public string[] CpCandidatesSelfTimer
        {
            get
            {
                return SettingsValueConverter.FromSelfTimer(status.SelfTimerInfo).candidates;
            }
        }

        public bool CpIsAvailableSelfTimer
        {
            get
            {
                return status.IsAvailable("setSelfTimer") &&
                    status.SelfTimerInfo != null &&
                    !setting.IsIntervalShootingEnabled &&
                    manager != null &&
                    !manager.IntervalManager.IsRunning;
            }
        }

        public int CpSelectedIndexPostviewSize
        {
            get
            {
                return SettingsValueConverter.GetSelectedIndex(status.PostviewSizeInfo);
            }
            set
            {
                SetSelectedAsCurrent(status.PostviewSizeInfo, value);
            }
        }

        public string[] CpCandidatesPostviewSize
        {
            get
            {
                return SettingsValueConverter.FromPostViewSize(status.PostviewSizeInfo).candidates;
            }
        }

        public bool CpIsAvailablePostviewSize
        {
            get
            {
                return status.IsAvailable("setPostviewImageSize") &&
                    status.PostviewSizeInfo != null &&
                    manager != null &&
                    !manager.IntervalManager.IsRunning;
            }
        }

        public int CpSelectedIndexShootMode
        {
            get
            {
                return SettingsValueConverter.GetSelectedIndex(status.ShootModeInfo);
            }
            set
            {
                SetSelectedAsCurrent(status.ShootModeInfo, value);
            }
        }

        public string[] CpCandidatesShootMode
        {
            get
            {
                return SettingsValueConverter.FromShootMode(status.ShootModeInfo).candidates;
            }
        }

        public bool CpIsAvailableShootMode
        {
            get
            {
                return status.IsAvailable("setShootMode") &&
                    status.ShootModeInfo != null &&
                    manager != null &&
                    !manager.IntervalManager.IsRunning;
            }
        }

        public int CpSelectedIndexExposureMode
        {
            get
            {
                return SettingsValueConverter.GetSelectedIndex(status.ExposureMode);
            }
            set
            {
                SetSelectedAsCurrent(status.ExposureMode, value);
            }
        }

        public string[] CpCandidatesExposureMode
        {
            get { return SettingsValueConverter.FromExposureMode(status.ExposureMode).candidates; }
        }

        public bool CpIsAvailableExposureMode
        {
            get
            {
                return status.IsAvailable("setExposureMode") && status.ExposureMode != null && manager != null && !manager.IntervalManager.IsRunning;
            }
        }

        public bool CpIsAvailableExposureCompensation
        {
            get
            {
                return status.IsAvailable("setExposureCompensation") && status.EvInfo != null && manager != null && !manager.IntervalManager.IsRunning;
            }
        }

        public int CpSelectedIndexExposureCompensation
        {
            get
            {
                if (status == null || status.EvInfo == null || !status.IsAvailable("setExposureCompensation"))
                {
                    return 0;
                }
                return SettingsValueConverter.GetSelectedIndex(status.EvInfo);
            }
            set
            {
                if (status.EvInfo != null)
                {
                    if (value <= status.EvInfo.Candidate.MaxIndex && value >= status.EvInfo.Candidate.MinIndex)
                    {
                        status.EvInfo.CurrentIndex = value;
                    }
                    else
                    {
                        status.EvInfo.CurrentIndex = 0;
                    }
                }
            }
        }

        public string[] CpCandidatesExposureCompensation
        {
            get { return SettingsValueConverter.FromExposureCompensation(status.EvInfo); }
        }

        public int CpMaxExposureCompensation
        {
            get
            {
                if (status == null || status.EvInfo == null)
                {
                    return 0;
                }
                return status.EvInfo.Candidate.MaxIndex;
            }
        }

        public int CpMinExposureCompensation
        {
            get
            {
                if (status == null || status.EvInfo == null)
                {
                    return 0;
                }
                return status.EvInfo.Candidate.MinIndex;
            }
        }

        public string CpDisplayValueExposureCompensation
        {
            get
            {
                if (status == null || status.EvInfo == null || !status.IsAvailable("setExposureCompensation"))
                {
                    return "--";
                }
                var value = EvConverter.GetEv(status.EvInfo.CurrentIndex, status.EvInfo.Candidate.IndexStep);
                if (value > 0)
                {
                    return "+" + Math.Round(value, 1, MidpointRounding.AwayFromZero).ToString("0.0");
                }
                else
                {
                    return Math.Round(value, 1, MidpointRounding.AwayFromZero).ToString("0.0");
                }
            }
        }

        public int CpSelectedIndexBeepMode
        {
            get
            {
                return SettingsValueConverter.GetSelectedIndex(status.BeepMode);
            }
            set
            {
                SetSelectedAsCurrent(status.BeepMode, value);
            }
        }

        public string[] CpCandidatesBeepMode
        {
            get
            {
                return SettingsValueConverter.FromBeepMode(status.BeepMode).candidates;
            }
        }

        public bool CpIsAvailableBeepMode
        {
            get
            {
                return status.IsAvailable("setBeepMode") &&
                    status.BeepMode != null &&
                    manager != null &&
                    !manager.IntervalManager.IsRunning;
            }
        }

        public int CpSelectedIndexStillImageSize
        {
            get
            {
                return SettingsValueConverter.GetSelectedIndex(status.StillImageSize);
            }
            set
            {
                SetSelectedAsCurrent(status.StillImageSize, value);
            }
        }

        public string[] CpCandidatesStillImageSize
        {
            get
            {
                return SettingsValueConverter.FromStillImageSize(status.StillImageSize).candidates;
            }
        }

        public bool CpIsAvailableStillImageSize
        {
            get
            {
                return status.IsAvailable("setStillSize") &&
                    status.StillImageSize != null &&
                    manager != null &&
                    !manager.IntervalManager.IsRunning;
            }
        }

        public int CpSelectedIndexWhiteBalance
        {
            get
            {
                return SettingsValueConverter.GetSelectedIndex(status.WhiteBalance);
            }
            set
            {
                SetSelectedAsCurrent(status.WhiteBalance, value);
            }
        }

        public string[] CpCandidatesWhiteBalance
        {
            get
            {
                return SettingsValueConverter.FromWhiteBalance(status.WhiteBalance).candidates;
            }
        }

        public bool CpIsAvailableWhiteBalance
        {
            get
            {
                return status.IsAvailable("setWhiteBalance") &&
                    status.WhiteBalance != null &&
                    manager != null &&
                    !manager.IntervalManager.IsRunning;
            }
        }

        public bool CpIsAvailableColorTemperture
        {
            get
            {
                return CpIsAvailableWhiteBalance &&
                    status.ColorTempertureCandidates != null &&
                    status.ColorTempertureCandidates.ContainsKey(status.WhiteBalance.current) &&
                    status.ColorTempertureCandidates[status.WhiteBalance.current].Length != 0 &&
                    status.ColorTemperture != -1;
            }
        }

        public Visibility CpIsVisibleColorTemperture
        {
            get
            {
                return CpIsAvailableColorTemperture ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public int CpSelectedIndexViewAngle
        {
            get
            {
                return SettingsValueConverter.GetSelectedIndex(status.ViewAngle);
            }
            set
            {
                SetSelectedAsCurrent(status.ViewAngle, value);
            }
        }

        public string[] CpCandidatesViewAngle
        {
            get
            {
                return SettingsValueConverter.FromViewAngle(status.ViewAngle).candidates;
            }
        }

        public bool CpIsAvailableViewAngle
        {
            get
            {
                return status.IsAvailable("setViewAngle") &&
                    status.BeepMode != null &&
                    manager != null &&
                    !manager.IntervalManager.IsRunning;
            }
        }

        public int CpSelectedIndexSteadyMode
        {
            get
            {
                return SettingsValueConverter.GetSelectedIndex(status.SteadyMode);
            }
            set
            {
                SetSelectedAsCurrent(status.SteadyMode, value);
            }
        }

        public string[] CpCandidatesSteadyMode
        {
            get
            {
                return SettingsValueConverter.FromSteadyMode(status.SteadyMode).candidates;
            }
        }

        public bool CpIsAvailableSteadyMode
        {
            get
            {
                return status.IsAvailable("setSteadyMode") &&
                    status.SteadyMode != null &&
                    manager != null &&
                    !manager.IntervalManager.IsRunning;
            }
        }

        public int CpSelectedIndexMovieQuality
        {
            get
            {
                return SettingsValueConverter.GetSelectedIndex(status.MovieQuality);
            }
            set
            {
                SetSelectedAsCurrent(status.MovieQuality, value);
            }
        }

        public string[] CpCandidatesMovieQuality
        {
            get
            {
                return SettingsValueConverter.FromMovieQuality(status.MovieQuality).candidates;
            }
        }

        public bool CpIsAvailableMovieQuality
        {
            get
            {
                return status.IsAvailable("setMovieQuality") &&
                    status.MovieQuality != null &&
                    manager != null &&
                    !manager.IntervalManager.IsRunning;
            }
        }

        public bool CpIsAvailableStillImageFunctions
        {
            get
            {
                if (status == null || status.ShootModeInfo == null)
                {
                    return false;
                }
                return status.ShootModeInfo.current == ShootModeParam.Still &&
                    manager != null && !manager.IntervalManager.IsRunning;
            }
        }

        public void OnControlPanelPropertyChanged(string name)
        {
            OnPropertyChanged(name);
        }

        private static void SetSelectedAsCurrent<T>(Capability<T> capability, int index)
        {
            if (capability != null)
            {
                if (capability.candidates.Length > index)
                {
                    capability.current = capability.candidates[index];
                }
                else
                {

                    capability.current = default(T);
                }
            }
        }
    }
}
