using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using WPPMM.RemoteApi;

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

        private List<string> _MethodTypes = null;
        public List<String> MethodTypes
        {
            get { return (_MethodTypes == null) ? new List<string>() : _MethodTypes; }
            set
            {
                _MethodTypes = value;
                OnPropertyChanged("MethodTypes");
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

                OnPropertyChanged("AvailableApis");
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
                    OnPropertyChanged("CameraStatus");
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
                OnPropertyChanged("PostviewSizeInfo");
            }
            get { return _PostviewSizeInfo; }
        }

        private BasicInfo<int> _SelfTimerInfo;
        public BasicInfo<int> SelfTimerInfo
        {
            set
            {
                _SelfTimerInfo = value;
                OnPropertyChanged("SelfTimerInfo");
            }
            get { return _SelfTimerInfo; }
        }

        private BasicInfo<string> _ShootModeInfo;
        public BasicInfo<string> ShootModeInfo
        {
            set
            {
                _ShootModeInfo = value;
                OnPropertyChanged("ShootModeInfo");
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
    }
}
