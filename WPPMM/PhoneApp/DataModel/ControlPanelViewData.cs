using System.ComponentModel;
using WPPMM.CameraManager;
using WPPMM.Utils;

namespace WPPMM.DataModel
{
    public class ControlPanelViewData : INotifyPropertyChanged
    {
        private readonly Status status;

        public ControlPanelViewData(Status status)
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
                        break;
                }
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
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
            get { return status.IsAvailable("setSelfTimer") && status.SelfTimerInfo != null; }
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
            get { return status.IsAvailable("setPostviewImageSize") && status.PostviewSizeInfo != null; }
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
            get { return status.IsAvailable("setShootMode") && status.ShootModeInfo != null; }
        }
    }
}
