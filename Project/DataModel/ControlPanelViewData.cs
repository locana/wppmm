using Kazyx.RemoteApi;
using Kazyx.RemoteApi.Camera;
using Kazyx.WPPMM.CameraManager;
using Kazyx.WPPMM.Utils;
using System;
using System.ComponentModel;
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
                        OnPropertyChanged("CpIsAvailableFlashMode");
                        OnPropertyChanged("CpIsAvailableFocusMode");
                        OnPropertyChanged("CpIsVisibleColorTemperture");
                        OnPropertyChanged("CpDisplayValueExposureCompensation");
                        OnPropertyChanged("CpSelectedIndexExposureCompensation");
                        OnPropertyChanged("CpIsAvailableZoomSetting");
                        OnPropertyChanged("CpIsAvailableStillQuality");
                        OnPropertyChanged("CpIsAvailableContShootingMode");
                        OnPropertyChanged("CpIsAvailableContShootingSpeed");
                        OnPropertyChanged("CpIsAvailableContShootingResult");
                        OnPropertyChanged("CpIsAvailableFlipMode");
                        OnPropertyChanged("CpIsAvailableSceneSelection");
                        OnPropertyChanged("CpIsAvailableIntervalTime");
                        OnPropertyChanged("CpIsAvailableColorSetting");
                        OnPropertyChanged("CpIsAvailableMovieFileFormat");
                        OnPropertyChanged("CpIsAvailableInfraredRemoteControl");
                        OnPropertyChanged("CpIsAvailableTvColorSystem");
                        OnPropertyChanged("CpIsAvailableTrackingFocusStatus");
                        OnPropertyChanged("CpIsAvailableTrackingFocus");
                        OnPropertyChanged("CpIsAvailableBatteryInfo");
                        OnPropertyChanged("CpIsAvailableRecordingTimeSec");
                        OnPropertyChanged("CpIsAvailableNumberOfShots");
                        OnPropertyChanged("CpIsAvailableAutoPowerOff");
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
                    case "FlashMode":
                    case "FocusMode":
                    case "ZoomSetting":
                    case "StillQuality":
                    case "ContShootingMode":
                    case "ContShootingSpeed":
                    case "FlipMode":
                    case "SceneSelection":
                    case "IntervalTime":
                    case "ColorSetting":
                    case "MovieFileFormat":
                    case "InfraredRemoteControl":
                    case "TvColorSystem":
                    case "TrackingFocus":
                    case "AutoPowerOff":
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
                    DebugUtil.Log("Caught COMException: ControlPanelViewData");
                }
                catch (NullReferenceException)
                {
                    DebugUtil.Log("Caught NullReferenceException: ControlPanelViewData");
                }
                catch (InvalidOperationException e)
                {
                    DebugUtil.Log(e.StackTrace);
                }
            });
        }

        public int CpSelectedIndexSelfTimer
        {
            get
            {
                return SettingsValueConverter.GetSelectedIndex(status.SelfTimer);
            }
            set
            {
                SetSelectedAsCurrent(status.SelfTimer, value);
            }
        }

        public string[] CpCandidatesSelfTimer
        {
            get
            {
                return SettingsValueConverter.FromSelfTimer(status.SelfTimer).Candidates.ToArray();
            }
        }

        public bool CpIsAvailableSelfTimer
        {
            get
            {
                return status.IsAvailable("setSelfTimer") &&
                    status.SelfTimer != null &&
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
                return SettingsValueConverter.FromPostViewSize(status.PostviewSizeInfo).Candidates.ToArray();
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
                return SettingsValueConverter.GetSelectedIndex(status.ShootMode);
            }
            set
            {
                SetSelectedAsCurrent(status.ShootMode, value);
            }
        }

        public string[] CpCandidatesShootMode
        {
            get
            {
                return SettingsValueConverter.FromShootMode(status.ShootMode).Candidates.ToArray();
            }
        }

        public bool CpIsAvailableShootMode
        {
            get
            {
                return status.IsAvailable("setShootMode") &&
                    status.ShootMode != null &&
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
            get { return SettingsValueConverter.FromExposureMode(status.ExposureMode).Candidates.ToArray(); }
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
                return SettingsValueConverter.FromBeepMode(status.BeepMode).Candidates.ToArray();
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
                return SettingsValueConverter.FromStillImageSize(status.StillImageSize).Candidates.ToArray();
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
                return SettingsValueConverter.FromWhiteBalance(status.WhiteBalance).Candidates.ToArray();
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
                    status.WhiteBalance.Current != null &&
                    status.ColorTempertureCandidates.ContainsKey(status.WhiteBalance.Current) &&
                    status.ColorTempertureCandidates[status.WhiteBalance.Current].Length != 0 &&
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
                return SettingsValueConverter.FromViewAngle(status.ViewAngle).Candidates.ToArray();
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
                return SettingsValueConverter.FromSteadyMode(status.SteadyMode).Candidates.ToArray();
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
                return SettingsValueConverter.FromMovieQuality(status.MovieQuality).Candidates.ToArray();
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

        public int CpSelectedIndexFlashMode
        {
            get
            {
                return SettingsValueConverter.GetSelectedIndex(status.FlashMode);
            }
            set
            {
                SetSelectedAsCurrent(status.FlashMode, value);
            }
        }

        public string[] CpCandidatesFlashMode
        {
            get
            {
                return SettingsValueConverter.FromFlashMode(status.FlashMode).Candidates.ToArray();
            }
        }

        public bool CpIsAvailableFlashMode
        {
            get
            {
                return status.IsAvailable("setFlashMode") &&
                    status.FlashMode != null &&
                    manager != null &&
                    !manager.IntervalManager.IsRunning;
            }
        }

        public int CpSelectedIndexFocusMode
        {
            get
            {
                return SettingsValueConverter.GetSelectedIndex(status.FocusMode);
            }
            set
            {
                SetSelectedAsCurrent(status.FocusMode, value);
            }
        }

        public string[] CpCandidatesFocusMode
        {
            get
            {
                return SettingsValueConverter.FromFocusMode(status.FocusMode).Candidates.ToArray();
            }
        }

        public bool CpIsAvailableFocusMode
        {
            get
            {
                return status.IsAvailable("setFocusMode") &&
                    status.FocusMode != null &&
                    manager != null &&
                    !manager.IntervalManager.IsRunning;
            }
        }

        public bool CpIsAvailableZoomSetting
        {
            get
            {
                return status.IsAvailable("setZoomSetting") &&
                status.ZoomSetting != null &&
                manager != null &&
                !manager.IntervalManager.IsRunning;
            }
        }
        public int CpSelectedIndexZoomSetting
        {
            get
            {
                return SettingsValueConverter.GetSelectedIndex(status.ZoomSetting);
            }
            set
            {
                SetSelectedAsCurrent(status.ZoomSetting, value);
            }
        }
        public string[] CpCandidatesZoomSetting
        {
            get
            {
                return SettingsValueConverter.FromZoomSetting(status.ZoomSetting).Candidates.ToArray();
            }
        }
        public bool CpIsAvailableStillQuality
        {
            get
            {
                return status.IsAvailable("setStillQuality") &&
                status.StillQuality != null &&
                manager != null &&
                !manager.IntervalManager.IsRunning;
            }
        }
        public int CpSelectedIndexStillQuality
        {
            get
            {
                if (status.StillQuality != null) { DebugUtil.Log("[get] " + status.StillQuality.Current); }
                return SettingsValueConverter.GetSelectedIndex(status.StillQuality);
            }
            set
            {
                DebugUtil.Log("[set] " + value);
                SetSelectedAsCurrent(status.StillQuality, value);
            }
        }
        public string[] CpCandidatesStillQuality
        {
            get
            {
                return SettingsValueConverter.FromStillQuality(status.StillQuality).Candidates.ToArray();
            }
        }
        public bool CpIsAvailableContShootingMode
        {
            get
            {
                return status.IsAvailable("setContShootingMode") &&
                status.ContShootingMode != null &&
                manager != null &&
                !manager.IntervalManager.IsRunning;
            }
        }
        public int CpSelectedIndexContShootingMode
        {
            get
            {
                var index = SettingsValueConverter.GetSelectedIndex(status.ContShootingMode);
                DebugUtil.Log("[get] " + index);
                return index;
            }
            set
            {
                DebugUtil.Log("[set] " + value);
                SetSelectedAsCurrent(status.ContShootingMode, value);
            }
        }
        public string[] CpCandidatesContShootingMode
        {
            get
            {
                var candidates = SettingsValueConverter.FromContShootingMode(status.ContShootingMode).Candidates.ToArray();
                foreach (string s in candidates)
                {
                    DebugUtil.Log(s);
                }
                return candidates;
            }
        }
        public bool CpIsAvailableContShootingSpeed
        {
            get
            {
                return status.IsAvailable("setContShootingSpeed") &&
                status.ContShootingSpeed != null &&
                manager != null &&
                !manager.IntervalManager.IsRunning;
            }
        }
        public int CpSelectedIndexContShootingSpeed
        {
            get
            {
                return SettingsValueConverter.GetSelectedIndex(status.ContShootingSpeed);
            }
            set
            {
                SetSelectedAsCurrent(status.ContShootingSpeed, value);
            }
        }
        public string[] CpCandidatesContShootingSpeed
        {
            get
            {
                return SettingsValueConverter.FromContShootingSpeed(status.ContShootingSpeed).Candidates.ToArray();
            }
        }
        public bool CpIsAvailableFlipMode
        {
            get
            {
                return status.IsAvailable("setFlipMode") &&
                status.FlipMode != null &&
                manager != null &&
                !manager.IntervalManager.IsRunning;
            }
        }
        public int CpSelectedIndexFlipMode
        {
            get
            {
                return SettingsValueConverter.GetSelectedIndex(status.FlipMode);
            }
            set
            {
                SetSelectedAsCurrent(status.FlipMode, value);
            }
        }
        public string[] CpCandidatesFlipMode
        {
            get
            {
                return SettingsValueConverter.FromFlipMode(status.FlipMode).Candidates.ToArray();
            }
        }
        public bool CpIsAvailableSceneSelection
        {
            get
            {
                return status.IsAvailable("setSceneSelection") &&
                status.SceneSelection != null &&
                manager != null &&
                !manager.IntervalManager.IsRunning;
            }
        }
        public int CpSelectedIndexSceneSelection
        {
            get
            {
                return SettingsValueConverter.GetSelectedIndex(status.SceneSelection);
            }
            set
            {
                SetSelectedAsCurrent(status.SceneSelection, value);
            }
        }
        public string[] CpCandidatesSceneSelection
        {
            get
            {
                return SettingsValueConverter.FromSceneSelection(status.SceneSelection).Candidates.ToArray();
            }
        }
        public bool CpIsAvailableIntervalTime
        {
            get
            {
                return status.IsAvailable("setIntervalTime") &&
                status.IntervalTime != null &&
                manager != null &&
                !manager.IntervalManager.IsRunning;
            }
        }
        public int CpSelectedIndexIntervalTime
        {
            get
            {
                return SettingsValueConverter.GetSelectedIndex(status.IntervalTime);
            }
            set
            {
                SetSelectedAsCurrent(status.IntervalTime, value);
            }
        }
        public string[] CpCandidatesIntervalTime
        {
            get
            {
                return SettingsValueConverter.FromIntervalTime(status.IntervalTime).Candidates.ToArray();
            }
        }
        public bool CpIsAvailableColorSetting
        {
            get
            {
                return status.IsAvailable("setColorSetting") &&
                status.ColorSetting != null &&
                manager != null &&
                !manager.IntervalManager.IsRunning;
            }
        }
        public int CpSelectedIndexColorSetting
        {
            get
            {
                return SettingsValueConverter.GetSelectedIndex(status.ColorSetting);
            }
            set
            {
                SetSelectedAsCurrent(status.ColorSetting, value);
            }
        }
        public string[] CpCandidatesColorSetting
        {
            get
            {
                return SettingsValueConverter.FromColorSetting(status.ColorSetting).Candidates.ToArray();
            }
        }
        public bool CpIsAvailableMovieFileFormat
        {
            get
            {
                return status.IsAvailable("setMovieFileFormat") &&
                status.MovieFileFormat != null &&
                manager != null &&
                !manager.IntervalManager.IsRunning;
            }
        }
        public int CpSelectedIndexMovieFileFormat
        {
            get
            {
                return SettingsValueConverter.GetSelectedIndex(status.MovieFileFormat);
            }
            set
            {
                SetSelectedAsCurrent(status.MovieFileFormat, value);
            }
        }
        public string[] CpCandidatesMovieFileFormat
        {
            get
            {
                return SettingsValueConverter.FromMovieFileFormat(status.MovieFileFormat).Candidates.ToArray();
            }
        }
        public bool CpIsAvailableInfraredRemoteControl
        {
            get
            {
                return status.IsAvailable("setInfraredRemoteControl") &&
                status.InfraredRemoteControl != null &&
                manager != null &&
                !manager.IntervalManager.IsRunning;
            }
        }
        public int CpSelectedIndexInfraredRemoteControl
        {
            get
            {
                return SettingsValueConverter.GetSelectedIndex(status.InfraredRemoteControl);
            }
            set
            {
                SetSelectedAsCurrent(status.InfraredRemoteControl, value);
            }
        }
        public string[] CpCandidatesInfraredRemoteControl
        {
            get
            {
                return SettingsValueConverter.FromInfraredRemoteControl(status.InfraredRemoteControl).Candidates.ToArray();
            }
        }
        public bool CpIsAvailableTvColorSystem
        {
            get
            {
                return status.IsAvailable("setTvColorSystem") &&
                status.TvColorSystem != null &&
                manager != null &&
                !manager.IntervalManager.IsRunning;
            }
        }
        public int CpSelectedIndexTvColorSystem
        {
            get
            {
                return SettingsValueConverter.GetSelectedIndex(status.TvColorSystem);
            }
            set
            {
                SetSelectedAsCurrent(status.TvColorSystem, value);
            }
        }
        public string[] CpCandidatesTvColorSystem
        {
            get
            {
                return SettingsValueConverter.FromTvColorSystem(status.TvColorSystem).Candidates.ToArray();
            }
        }
        public bool CpIsAvailableTrackingFocus
        {
            get
            {
                return status.IsAvailable("setTrackingFocus") &&
                status.TrackingFocus != null &&
                manager != null &&
                !manager.IntervalManager.IsRunning;
            }
        }
        public int CpSelectedIndexTrackingFocus
        {
            get
            {
                return SettingsValueConverter.GetSelectedIndex(status.TrackingFocus);
            }
            set
            {
                SetSelectedAsCurrent(status.TrackingFocus, value);
            }
        }
        public string[] CpCandidatesTrackingFocus
        {
            get
            {
                return SettingsValueConverter.FromTrackingFocus(status.TrackingFocus).Candidates.ToArray();
            }
        }
        public bool CpIsAvailableAutoPowerOff
        {
            get
            {
                return status.IsAvailable("setAutoPowerOff") &&
                status.AutoPowerOff != null &&
                manager != null &&
                !manager.IntervalManager.IsRunning;
            }
        }
        public int CpSelectedIndexAutoPowerOff
        {
            get
            {
                return SettingsValueConverter.GetSelectedIndex(status.AutoPowerOff);
            }
            set
            {
                SetSelectedAsCurrent(status.AutoPowerOff, value);
            }
        }
        public string[] CpCandidatesAutoPowerOff
        {
            get
            {
                return SettingsValueConverter.FromAutoPowerOff(status.AutoPowerOff).Candidates.ToArray();
            }
        }

        public bool CpIsAvailableStillImageFunctions
        {
            get
            {
                if (status == null || status.ShootMode == null)
                {
                    return false;
                }
                return status.ShootMode.Current == ShootModeParam.Still &&
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
                if (capability.Candidates.Count > index)
                {
                    DebugUtil.Log("updated: " + index + " " + capability.Candidates[index]);
                    capability.Current = capability.Candidates[index];
                }
                else
                {

                    capability.Current = default(T);
                }
            }
        }
    }
}
