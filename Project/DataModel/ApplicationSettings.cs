using Kazyx.RemoteApi;
using Kazyx.RemoteApi.Camera;
using Kazyx.WPPMM.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Linq;

namespace Kazyx.WPPMM.DataModel
{

    public class ApplicationSettings : INotifyPropertyChanged
    {

        private static ApplicationSettings sSettings = new ApplicationSettings();
        private CameraManager.CameraManager manager;
        
        private SortedDictionary<string, string> GridTypeSettingNames = new SortedDictionary<string, string>()
        {
            { FramingGridTypes.Off, "off" },
            { FramingGridTypes.RuleOfThirds, "third" },
            { FramingGridTypes.Square, "square" },
            { FramingGridTypes.Crosshairs, "cross" },
            { FramingGridTypes.Fibonacci, "fibo" },
            { FramingGridTypes.GoldenRatio, "golden" },
        };

        private ApplicationSettings()
        {
            manager = CameraManager.CameraManager.GetInstance();
            IsPostviewTransferEnabled = Preference.IsPostviewTransferEnabled();
            IsIntervalShootingEnabled = Preference.IsIntervalShootingEnabled();
            IntervalTime = Preference.IntervalTime();
            IsShootButtonDisplayed = Preference.IsShootButtonDisplayed();
            IsHistogramDisplayed = Preference.IsHistogramDisplayed();
            GeotagEnabled = Preference.GeotagEnabled();
            GridType = Preference.FramingGridsType() ?? FramingGridTypes.Off;
        }

        public static ApplicationSettings GetInstance()
        {
            return sSettings;
        }

        private bool _IsPostviewTransferEnabled = true;

        public bool IsPostviewTransferEnabled
        {
            set
            {
                if (_IsPostviewTransferEnabled != value)
                {
                    Preference.SetPostviewTransferEnabled(value);
                    _IsPostviewTransferEnabled = value;
                    OnPropertyChanged("IsPostviewTransferEnabled");
                }
            }
            get
            {
                return _IsPostviewTransferEnabled;
            }
        }

        private bool _IsIntervalShootingEnabled = false;

        public bool IsIntervalShootingEnabled
        {
            set
            {
                if (_IsIntervalShootingEnabled != value)
                {
                    Preference.SetIntervalShootingEnabled(value);
                    _IsIntervalShootingEnabled = value;

                    OnPropertyChanged("IsIntervalShootingEnabled");
                    OnPropertyChanged("IntervalTimeVisibility");

                    // exclusion
                    if (value)
                    {
                        if (manager.cameraStatus.IsAvailable("setSelfTimer") && manager.IntervalManager != null)
                        {
                            SetSelfTimerOff();
                        }
                    }
                }
            }
            get
            {
                return _IsIntervalShootingEnabled;
            }
        }

        private async void SetSelfTimerOff()
        {
            try
            {
                await manager.CameraApi.SetSelfTimerAsync(SelfTimerParam.Off);
            }
            catch (RemoteApiException e)
            {
                Debug.WriteLine("Failed to set selftimer off: " + e.code);
            }
        }

        public Visibility IntervalTimeVisibility
        {
            get { return IsIntervalShootingEnabled ? Visibility.Visible : Visibility.Collapsed; }
        }

        private int _IntervalTime = 10;

        public int IntervalTime
        {
            set
            {
                if (_IntervalTime != value)
                {
                    Preference.SetIntervalTime(value);
                    _IntervalTime = value;
                    // Debug.WriteLine("IntervalTime changed: " + value);
                    OnPropertyChanged("IntervalTime");
                }
            }
            get
            {
                return _IntervalTime;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            // Debug.WriteLine("OnProperty changed: " + name);
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
                        Debug.WriteLine("Caught COMException: ApplicationSettings");
                    }
                });
            }
        }

        private bool _IsShootButtonDisplayed = true;
        public bool IsShootButtonDisplayed
        {
            set
            {
                if (_IsShootButtonDisplayed != value)
                {
                    Preference.SetShootButtonDisplayed(value);
                    _IsShootButtonDisplayed = value;
                    OnPropertyChanged("ShootButtonVisibility");
                    Debug.WriteLine("ShootbuttonVisibility updated: " + value.ToString());
                }
            }
            get
            {
                return _IsShootButtonDisplayed;
            }
        }

        private bool _IsHistogramDisplayed = true;
        public bool IsHistogramDisplayed
        {
            set
            {
                if (_IsHistogramDisplayed != value)
                {
                    Preference.SetHistogramDisplayed(value);
                    _IsHistogramDisplayed = value;
                    OnPropertyChanged("HistogramVisibility");
                }
            }
            get { return _IsHistogramDisplayed; }
        }

        private bool _GeotagEnabled = false;
        public bool GeotagEnabled
        {
            set
            {
                if (_GeotagEnabled != value)
                {
                    Preference.SetGeotagEnabled(value);
                    _GeotagEnabled = value;
                    OnPropertyChanged("GeotagEnabled");
                    OnPropertyChanged("GeopositionStatusVisibility");
                }
            }
            get { return _GeotagEnabled; }
        }

        private string _GridType = FramingGridTypes.Off;
        public string GridType
        {
            set
            {
                if (_GridType != value)
                {
                    Debug.WriteLine("GridType updated: " + value);
                    Preference.SetFramingGridsType(value);
                    _GridType = value;
                    OnPropertyChanged("GridType");
                }
            }
            get { return _GridType; }
        }

        public int GridTypeIndex
        {
            set
            {
                GridType = GridTypeSettingNames.ElementAtOrDefault(value).Key;
            }
            get
            {
                int i = 0;
                foreach (string type in GridTypeSettingNames.Keys.ToArray<string>())
                {
                    if (GridType.Equals(type))
                    {
                        return i;
                    }
                    i++;
                }
                return 0;
            }
        }

        public string[] GridTypeCandidates
        {
            get { return GridTypeSettingNames.Values.ToArray<string>(); }
        }

        public Visibility ShootButtonVisibility
        {
            get
            {
                if (_IsShootButtonDisplayed && !ShootButtonTemporaryCollapsed)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        private bool _ShootButtonTemporaryCollapsed = false;
        public bool ShootButtonTemporaryCollapsed
        {
            get { return _ShootButtonTemporaryCollapsed; }
            set
            {
                if (value != _ShootButtonTemporaryCollapsed)
                {
                    _ShootButtonTemporaryCollapsed = value;
                    OnPropertyChanged("ShootButtonVisibility");
                }
            }
        }

        public Visibility HistogramVisibility
        {
            get
            {
                if (_IsHistogramDisplayed)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        public Visibility GeopositionStatusVisibility
        {
            get
            {
                if (GeotagEnabled) { return Visibility.Visible; }
                else { return Visibility.Collapsed; }
            }
        }

    }

    public class FramingGridTypes
    {
        public const string Off = "00_grid_off";
        public const string RuleOfThirds = "01_grid_rule_third";
        public const string GoldenRatio = "02_grid_golden_ratio";
        public const string Crosshairs = "03_grid_crosshairs";
        public const string Square = "04_grid_square";
        public const string Fibonacci = "05_grid_fibonacci";
    }
}
