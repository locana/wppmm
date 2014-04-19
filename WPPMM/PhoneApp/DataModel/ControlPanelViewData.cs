using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using WPPMM.CameraManager;
using WPPMM.RemoteApi;
using WPPMM.Utils;

namespace WPPMM.DataModel
{
    public class ControlPanelViewData : INotifyPropertyChanged
    {
        private readonly CameraStatus status;
        private CameraManager.CameraManager manager;

        public ControlPanelViewData(CameraStatus status)
        {
            this.status = status;
            this.manager = CameraManager.CameraManager.GetInstance();

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
                        break;
                    case "PostviewSizeInfo":
                        OnPropertyChanged("CpCandidatesPostviewSize");
                        OnPropertyChanged("CpSelectedIndexPostviewSize");
                        break;
                    case "SelfTimerInfo":
                        OnPropertyChanged("CpCandidatesSelfTimer");
                        OnPropertyChanged("CpSelectedIndexSelfTimer");
                        break;
                    case "ShootModeInfo":
                        OnPropertyChanged("CpCandidatesShootMode");
                        OnPropertyChanged("CpSelectedIndexShootMode");
                        OnPropertyChanged("CpIsAvailableStillImageFunctions");
                        break;
                    case "ExposureMode":
                        OnPropertyChanged("CpSelectedIndexExposureMode");
                        OnPropertyChanged("CpCandidatesExposureMode");
                       
                        break;
                    default:
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
                        Debug.WriteLine("Caught COMException: ControlPanelViewData");
                    }
                    catch (NullReferenceException)
                    {
                        Debug.WriteLine("Caught NullReferenceException: ControlPanelViewData");
                    }
                    catch (System.InvalidOperationException e)
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
                if (status.SelfTimerInfo != null)
                    status.SelfTimerInfo.current = status.SelfTimerInfo.candidates[value];
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
                if (status.PostviewSizeInfo != null)
                    status.PostviewSizeInfo.current = status.PostviewSizeInfo.candidates[value];
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
                if (status.ShootModeInfo != null)
                    status.ShootModeInfo.current = status.ShootModeInfo.candidates[value];
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
                if (status.ExposureMode != null)
                    status.ExposureMode.current = status.ExposureMode.candidates[value];
            }
        }

        public string[] CpCandidatesExposureMode
        {
            get { return SettingsValueConverter.FromExposureMode(status.ExposureMode).candidates; }
        }

        public bool CpIsAvailableExposureMode
        {
            get{
                return status.IsAvailable("setExposureMode") && status.ExposureMode != null && manager != null && !manager.IntervalManager.IsRunning;
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
    }
}
