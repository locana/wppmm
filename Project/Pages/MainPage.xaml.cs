using Kazyx.RemoteApi;
using Kazyx.WPMMM.CameraManager;
using Kazyx.WPMMM.Controls;
using Kazyx.WPMMM.Resources;
using Kazyx.WPPMM.CameraManager;
using Kazyx.WPPMM.Controls;
using Kazyx.WPPMM.DataModel;
using Kazyx.WPPMM.Utils;
using Microsoft.Devices;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Net.NetworkInformation;
using Microsoft.Phone.Reactive;
using Microsoft.Phone.Tasks;
using Microsoft.Xna.Framework.Media;
using NtNfcLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Windows.Devices.Geolocation;
using Windows.Networking.Proximity;

namespace Kazyx.WPPMM.Pages
{
    public partial class MainPage : PhoneApplicationPage
    {
        private const int PIVOTINDEX_MAIN = 0;
        private const int PIVOTINDEX_LIVEVIEW = 1;

        private readonly CameraManager.CameraManager cameraManager = CameraManager.CameraManager.GetInstance();

        private bool OnZooming;

        private const double APPBAR_OPACITY = 0.0;

        private ShootingViewData svd;
        private readonly PostViewData pvd = new PostViewData();
        private AppBarManager abm = new AppBarManager();
        private ControlPanelManager cpm;

        private ProximityDevice _proximitiyDevice;
        private long _subscriptionIdNdef;

        private const string AP_NAME_PREFIX = "DIRECT-";

        private const bool FilterBySsid = true;

        private static readonly BitmapImage GeoInfoStatusImage_OK = new BitmapImage(new Uri("/Assets/Screen/GeoInfoStatus_OK.png", UriKind.Relative));
        private static readonly BitmapImage GeoInfoStatusImage_NG = new BitmapImage(new Uri("/Assets/Screen/GeoInfoStatus_NG.png", UriKind.Relative));
        private static readonly BitmapImage GeoInfoStatusImage_Updating = new BitmapImage(new Uri("/Assets/Screen/GeoInfoStatus_Updating.png", UriKind.Relative));

        public MainPage()
        {
            InitializeComponent();

            MyPivot.SelectionChanged += MyPivot_SelectionChanged;

            this.InitAppSettingPanel();

            abm.SetEvent(Menu.About, (sender, e) => { 
                NavigationService.Navigate(new Uri("/Pages/AboutPage.xaml", UriKind.Relative)); 
            });
            abm.SetEvent(IconMenu.WiFi, (sender, e) => { var task = new ConnectionSettingsTask { ConnectionSettingsType = ConnectionSettingsType.WiFi }; task.Show(); });
            abm.SetEvent(IconMenu.ControlPanel, (sender, e) =>
            {
                if (abm != null)
                {
                    ApplicationBar = abm.Disable(IconMenu.TouchAfCancel).CreateNew(APPBAR_OPACITY);
                }
                ApplicationBar.IsVisible = false;
                if (cameraManager != null)
                {
                    cameraManager.CancelTouchAF();
                    cameraManager.CancelHalfPressShutter();
                }
                cpm.Show();
            });
            abm.SetEvent(IconMenu.ApplicationSetting, (sender, e) => { this.OpenAppSettingPanel(); });
            abm.SetEvent(IconMenu.CloseApplicationSetting, (sender, e) => { this.CloseAppSettingPanel(); });
            abm.SetEvent(IconMenu.TouchAfCancel, (sender, e) =>
            {
                if (cameraManager != null) { cameraManager.CancelTouchAF(); }
            });
            abm.SetEvent(IconMenu.CameraRoll, (sender, e) =>
            {
                if (cameraManager != null)
                {
                    cameraManager.CancelTouchAF();
                    cameraManager.CancelHalfPressShutter();
                }
                NavigationService.Navigate(new Uri("/Pages/ViewerPage.xaml", UriKind.Relative));
            });
            abm.SetEvent(IconMenu.Hidden, (sender, e) => { NavigationService.Navigate(new Uri("/Pages/HiddenPage.xaml", UriKind.Relative)); });

            cpm = new ControlPanelManager(ControlPanel);

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Debug.WriteLine(e.Uri);
            progress.IsVisible = false;
            InitializeApplication();
            if (FilterBySsid && GetSSIDName().StartsWith(AP_NAME_PREFIX))
            {
                if (MyPivot.SelectedIndex != PIVOTINDEX_LIVEVIEW)
                {
                    StartConnectionSequence(NavigationMode.New == e.NavigationMode);
                }
            }

            cameraManager.OnDisconnected += cameraManager_OnDisconnected;
            cameraManager.UpdateEvent += WifiUpdateListener;

            switch (MyPivot.SelectedIndex)
            {
                case PIVOTINDEX_MAIN:
                    LiveviewPageUnloaded();
                    EntrancePageLoaded();
                    break;
                case PIVOTINDEX_LIVEVIEW:
                    EntrancePageUnloaded();
                    LiveviewPageLoaded();
                    break;
            }
        }

        internal void cameraManager_OnDisconnected()
        {
            MessageBox.Show(AppResources.ErrorMessage_Dsconnected, AppResources.MessageCaption_error, MessageBoxButton.OK);
            MyPivot.IsLocked = false;
            if (cpm != null && cpm.IsShowing())
            {
                cpm.Hide();
            }
            this.GoToMainPage();
            this.InitializeApplication();
        }

        internal void InitializeApplication()
        {
            cameraManager.Refresh();
            UpdateNetworkStatus();
            LiveViewInit();
            initNFC();
        }

        internal void HideControlPanel()
        {
            if (cpm != null && cpm.IsShowing())
            {
                cpm.Hide();
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            cameraManager.OnDisconnected -= cameraManager_OnDisconnected;
            cameraManager.UpdateEvent -= WifiUpdateListener;

            switch (MyPivot.SelectedIndex)
            {
                case PIVOTINDEX_LIVEVIEW:
                    LiveviewPageUnloaded();
                    break;
                case PIVOTINDEX_MAIN:
                    EntrancePageUnloaded();
                    break;
            }

            cameraManager.RequestCloseLiveView();
            cameraManager.Refresh();
        }

        private void StartConnectionSequence(bool connect)
        {
            progress.IsVisible = connect;
            CameraManager.CameraManager.GetInstance().RequestSearchDevices(() =>
            {
                Debug.WriteLine("DeviceFound -> GoToShootingPage if required.");
                progress.IsVisible = false;
                if (connect) GoToShootingPage();
            }, () =>
            {
                Debug.WriteLine("Discovery timeout.");
                progress.IsVisible = false;
            });
        }

        private void HandleError(int code)
        {
            Debug.WriteLine("Error: " + code);
        }

        private void UpdateNetworkStatus()
        {
            var ssid = GetSSIDName();
            Debug.WriteLine("SSID: " + ssid);
            if (ssid != null && ssid.StartsWith(AP_NAME_PREFIX))
            {
                NetworkStatus.Text = AppResources.Guide_CantFindDevice;
            }
            else
            {
                NetworkStatus.Text = AppResources.Guide_WiFiNotEnabled;
            }

            if (cameraManager.DeviceInfo != null)
            {
                String modelName = cameraManager.DeviceInfo.FriendlyName;
                if (modelName != null)
                {
                    NetworkStatus.Text = AppResources.ConnectedDevice.Replace("_ssid_", modelName);
                    GuideMessage.Visibility = System.Windows.Visibility.Visible;
                }
            }
            // display initialize

            ProgressBar.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void GoToShootingPage()
        {
            if (MyPivot.SelectedIndex == 1)
            {
                LiveviewPageLoaded();
            }
            else
            {
                MyPivot.SelectedIndex = 1;
            }
        }

        private void GoToMainPage()
        {
            MyPivot.IsLocked = false;
            MyPivot.SelectedIndex = 0;
        }

        internal void WifiUpdateListener(CameraStatus cameraStatus)
        {
            Debug.WriteLine("WifiUpdateLIstener called");

            if (cameraStatus.isAvailableConnecting)
            {
                String modelName = "";
                if (cameraManager.DeviceInfo != null && cameraManager.DeviceInfo.FriendlyName != null)
                {
                    modelName = cameraManager.DeviceInfo.FriendlyName;
                }
                NetworkStatus.Text = AppResources.ConnectedDevice.Replace("_ssid_", modelName);
                GuideMessage.Visibility = System.Windows.Visibility.Visible;
            }
        }

        internal void LiveViewUpdateListener(CameraStatus cameraStatus)
        {
            if (cameraStatus.ZoomInfo != null)
            {
                // dumpZoomInfo(cameraStatus.ZoomInfo);
                double margin_left = cameraStatus.ZoomInfo.Position * 156 / 100;
                ZoomCursor.Margin = new Thickness(15 + margin_left, 2, 0, 0);
                Debug.WriteLine("zoom bar display update: " + margin_left);
            }
        }

        private string GetSSIDName()
        {
            foreach (var network in new NetworkInterfaceList())
            {
                if (
                    (network.InterfaceType == NetworkInterfaceType.Wireless80211) &&
                    (network.InterfaceState == ConnectState.Connected)
                    )
                {
                    return network.InterfaceName;
                }
            }
            return "<Not connected>";
        }

        private void LiveViewInit()
        {
            cameraManager.RequestCloseLiveView();
            OnZooming = false;
        }

        void CameraButtons_ShutterKeyPressed(object sender, EventArgs e)
        {
            if (cameraManager == null || cpm == null || cpm.IsShowing() || IsAppSettingPanelShowing()) { return; }
            RecStartStop();
        }

        private void takeImageButton_Click(object sender, RoutedEventArgs e)
        {
            RecStartStop();
        }

        private void RecStartStop()
        {
            if (cameraManager.IntervalManager.IsRunning)
            {
                cameraManager.StopLocalIntervalRec();
                if (cpm != null)
                {
                    cpm.OnControlPanelPropertyChanged("CpIsAvailableSelfTimer");
                    cpm.OnControlPanelPropertyChanged("CpIsAvailableShootMode");
                    cpm.OnControlPanelPropertyChanged("CpIsAvailablePostviewSize");
                    cpm.OnControlPanelPropertyChanged("CpIsAvailableStillImageFunctions");
                }
                return;
            }

            var status = cameraManager.cameraStatus;
            switch (status.Status)
            {
                case EventParam.Idle:
                    switch (status.ShootModeInfo.Current)
                    {
                        case ShootModeParam.Still:
                            if (ApplicationSettings.GetInstance().IsIntervalShootingEnabled)
                            {
                                cameraManager.StartLocalIntervalRec();
                                if (cpm != null)
                                {
                                    cpm.OnControlPanelPropertyChanged("CpIsAvailableSelfTimer");
                                    cpm.OnControlPanelPropertyChanged("CpIsAvailableShootMode");
                                    cpm.OnControlPanelPropertyChanged("CpIsAvailablePostviewSize");
                                    cpm.OnControlPanelPropertyChanged("CpIsAvailableStillImageFunctions");
                                }
                            }
                            else
                            {
                                cameraManager.RequestActTakePicture();
                            }
                            break;
                        case ShootModeParam.Movie:
                            cameraManager.StartMovieRec();
                            break;
                        case ShootModeParam.Audio:
                            cameraManager.StartAudioRec();
                            break;
                        case ShootModeParam.Interval:
                            cameraManager.StartIntervalStillRec();
                            break;
                    }
                    break;
                case EventParam.MvRecording:
                    cameraManager.StopMovieRec();
                    break;
                case EventParam.AuRecording:
                    cameraManager.StopAudioRec();
                    break;
                case EventParam.ItvRecording:
                    cameraManager.StopIntervalStillRec();
                    break;
            }
        }

        private void OnPictureSaved(Picture pic)
        {
            ApplicationBar = abm.Enable(IconMenu.CameraRoll).CreateNew(0.0);
        }

        private int PreviousSelectedPivotIndex = -1;

        private void MyPivot_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var pivot = sender as Pivot;
            if (pivot == null)
            {
                return;
            }

            if (PreviousSelectedPivotIndex == pivot.SelectedIndex)
            {
                return;
            }
            PreviousSelectedPivotIndex = pivot.SelectedIndex;
            switch (PreviousSelectedPivotIndex)
            {
                case 0:
                    LiveviewPageUnloaded();
                    EntrancePageLoaded();
                    break;
                case 1:
                    EntrancePageUnloaded();
                    LiveviewPageLoaded();
                    break;
                default:
                    break;
            }
        }

        private void EntrancePageUnloaded()
        {
            EntrancePivot.Opacity = 0;
            ClearNFCInfo();
        }

        private async void LiveviewPageLoaded()
        {
            SetPivotIsLocked(true);

            AppStatus.GetInstance().IsInShootingDisplay = true;
            ShootingPivot.Opacity = 1;
            SetLayoutByOrientation(this.Orientation);

            cameraManager.UpdateEvent += LiveViewUpdateListener;
            cameraManager.ShowToast += ShowToast;
            ToastApparance.Completed += ToastApparance_Completed;
            ScreenImage.ManipulationCompleted += ScreenImage_ManipulationCompleted;

            CameraButtons.ShutterKeyPressed += CameraButtons_ShutterKeyPressed;
            CameraButtons.ShutterKeyHalfPressed += CameraButtons_ShutterKeyHalfPressed;
            CameraButtons.ShutterKeyReleased += CameraButtons_ShutterKeyReleased;

            cameraManager.PictureNotifier = OnPictureSaved;
            cameraManager.OnAfStatusChanged += cameraManager_OnAfStatusChanged;
            if (svd != null)
            {
                svd.SlidersVisibilityChanged += SlidersVisibilityChanged;
            }
            if (cameraManager.IsClientReady())
            {
                cameraManager.OperateInitialProcess();
                cameraManager.RunEventObserver();
            }
            else if (FilterBySsid && !GetSSIDName().StartsWith(AP_NAME_PREFIX))
            {
                Dispatcher.BeginInvoke(() => { GoToMainPage(); });
                return;
            }
            else
            {
                Debug.WriteLine("Await for async device discovery");
                AppStatus.GetInstance().IsSearchingDevice = true;
                progress.IsVisible = true;
                var found = await PrepareConnectionAsync();
                progress.IsVisible = false;
                Dispatcher.BeginInvoke(() => { AppStatus.GetInstance().IsSearchingDevice = false; });

                Debug.WriteLine("Async device discovery result: " + found);
                if (found)
                {
                    cameraManager.OperateInitialProcess();
                    cameraManager.RunEventObserver();
                }
                else
                {
                    Dispatcher.BeginInvoke(() => { GoToMainPage(); });
                    return;
                }
            }

            abm.Clear();
            if (cpm != null && cpm.ItemCount > 0)
            {
                abm.Enable(IconMenu.ControlPanel);
            }
            abm.Enable(IconMenu.ApplicationSetting);

            Dispatcher.BeginInvoke(() => { if (cpm != null) cpm.Hide(); ApplicationBar = abm.CreateNew(APPBAR_OPACITY); });

            InitializeHitogram();

            cameraManager.OnHistogramUpdated += cameraManager_OnHistogramUpdated;
            GeopositionManager.GetInstance().GeopositionUpdated += GeopositionStatusUpdated;
            GeopositionManager.GetInstance().Enable = true;
        }

        internal void GeopositionStatusUpdated(GeopositionEventArgs args)
        {
            Debug.WriteLine("Geoposition status updated: " + args.Status);
            switch (args.Status)
            {
                case GeopositiomManagerStatus.Acquiring:
                    GeopositionStatusImage.Source = GeoInfoStatusImage_Updating;
                    break;
                case GeopositiomManagerStatus.OK:
                    GeopositionStatusImage.Source = GeoInfoStatusImage_OK;
                    break;
                case GeopositiomManagerStatus.Failed:
                    GeopositionStatusImage.Source = GeoInfoStatusImage_NG;
                    break;
            }
        }

        private void SlidersVisibilityChanged(System.Windows.Visibility visibility)
        {
            if (visibility == System.Windows.Visibility.Collapsed)
            {
                CloseSliderPanel();
            }
        }

        private void InitializeHitogram()
        {
            Histogram.Init(WPMMM.Controls.Histogram.ColorType.White, 1500);
        }

        private void cameraManager_OnHistogramUpdated(int[] r, int[] g, int[] b)
        {
            Histogram.SetHistogramValue(r, g, b);
        }

        void cameraManager_OnAfStatusChanged(CameraStatus status)
        {
            if (status.AfType == CameraStatus.AutoFocusType.Touch && 
                ( ( status.TouchFocusStatus == null && status.FocusStatus != FocusState.Released) ||
                ( status.TouchFocusStatus != null && status.TouchFocusStatus.Focused )))
            {
                if (!abm.IsEnabled(IconMenu.TouchAfCancel))
                {
                    if (cpm != null && !cpm.IsShowing())
                    {
                        ApplicationBar = abm.Enable(IconMenu.TouchAfCancel).CreateNew(0.0);
                    }
                    else
                    {
                        abm.Enable(IconMenu.TouchAfCancel);
                    }
                }
            }
            else
            {
                if (abm.IsEnabled(IconMenu.TouchAfCancel))
                {
                    if (cpm != null && !cpm.IsShowing())
                    {
                        ApplicationBar = abm.Disable(IconMenu.TouchAfCancel).CreateNew(0.0);
                    }
                    else
                    {
                        abm.Disable(IconMenu.TouchAfCancel);
                    }
                }
            }
        }

        void CameraButtons_ShutterKeyReleased(object sender, EventArgs e)
        {
            if (cameraManager == null || cpm == null || cpm.IsShowing() || IsAppSettingPanelShowing()) { return; }

            cameraManager.CancelHalfPressShutter();
        }

        void CameraButtons_ShutterKeyHalfPressed(object sender, EventArgs e)
        {
            if (cameraManager == null || cpm == null || cpm.IsShowing() || IsAppSettingPanelShowing()) { return; }

            cameraManager.RequestHalfPressShutter();
        }

        void ScreenImage_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            if (cpm.IsShowing())
            {
                cpm.Hide();
                if (ApplicationBar != null)
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        ApplicationBar.IsVisible = true;
                    });
                }
                return;
            }

            if (Sliders.Visibility == System.Windows.Visibility.Visible)
            {
                CloseSliderPanel();
                return;
            }

            if (!cameraManager.IsTouchAfAvailable())
            {
                return;
            }
            Image image = sender as Image;
            var touchX = e.ManipulationOrigin.X;
            var touchY = e.ManipulationOrigin.Y;

            double posX = touchX * 100.0 / image.ActualWidth;
            double posY = touchY * 100.0 / image.ActualHeight;

            Dispatcher.BeginInvoke(() =>
            {
                TouchAFPointer.Margin = new Thickness(touchX - TouchAFPointer.Width / 2, touchY - TouchAFPointer.Height / 2, 0, 0);
            });

            // Debug.WriteLine("tx: " + touchX + " ty: " + touchY);
            Debug.WriteLine("touch position X: " + posX + " Y: " + posY);

            cameraManager.RequestTouchAF(posX, posY);
        }

        private Task<bool> PrepareConnectionAsync()
        {
            var tcs = new TaskCompletionSource<bool>();
            bool done = false;
            cameraManager.RequestSearchDevices(() =>
            {
                if (!done) { done = true; tcs.SetResult(true); }
            }, () =>
            {
                if (!done) { done = true; tcs.SetResult(false); }
            });
            return tcs.Task;
        }

        private void LiveviewPageUnloaded()
        {
            SetPivotIsLocked(false);

            AppStatus.GetInstance().IsInShootingDisplay = false;
            ShootingPivot.Opacity = 0;
            cameraManager.StopEventObserver();
            cameraManager.UpdateEvent -= LiveViewUpdateListener;
            cameraManager.ShowToast -= ShowToast;
            ToastApparance.Completed -= ToastApparance_Completed;
            CameraButtons.ShutterKeyPressed -= CameraButtons_ShutterKeyPressed;
            CameraButtons.ShutterKeyHalfPressed -= CameraButtons_ShutterKeyHalfPressed;
            CameraButtons.ShutterKeyReleased -= CameraButtons_ShutterKeyReleased;

            cameraManager.PictureNotifier = null;

            cameraManager.OnAfStatusChanged -= cameraManager_OnAfStatusChanged;

            ScreenImage.ManipulationCompleted -= ScreenImage_ManipulationCompleted;
            cameraManager.IntervalManager.Stop();

            if (svd != null)
            {
                svd.SlidersVisibilityChanged -= SlidersVisibilityChanged;
            }

            if (cpm != null) { cpm.Hide(); }

            if (IsAppSettingPanelShowing())
            {
                this.CloseAppSettingPanel();
            }

            cameraManager.OnHistogramUpdated -= cameraManager_OnHistogramUpdated;
            GeopositionManager.GetInstance().GeopositionUpdated -= GeopositionStatusUpdated;
        }

        private void EntrancePageLoaded()
        {
            EntrancePivot.Opacity = 1;
            ApplicationBar = abm.Clear().Enable(Menu.About).Enable(IconMenu.WiFi).Enable(IconMenu.Hidden).CreateNew(0.0);
        }

        private void OnZoomInClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Stop Zoom In (if started)");
            if (OnZooming)
            {
                cameraManager.RequestActZoom(ZoomParam.DirectionIn, ZoomParam.ActionStop);
            }
        }

        private void OnZoomOutClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Stop zoom out (if started)");
            if (OnZooming)
            {
                cameraManager.RequestActZoom(ZoomParam.DirectionOut, ZoomParam.ActionStop);
            }
        }

        private void OnZoomInHold(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Debug.WriteLine("Zoom In: Start");
            cameraManager.RequestActZoom(ZoomParam.DirectionIn, ZoomParam.ActionStart);
            OnZooming = true;
        }

        private void OnZoomOutHold(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Debug.WriteLine("Zoom Out: Start");
            cameraManager.RequestActZoom(ZoomParam.DirectionOut, ZoomParam.ActionStart);
            OnZooming = true;
        }

        private void OnZoomInTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Debug.WriteLine("Zoom In: OneShot");
            cameraManager.RequestActZoom(ZoomParam.DirectionIn, ZoomParam.Action1Shot);
        }

        private void OnZoomOutTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Debug.WriteLine("Zoom In: OneShot");
            cameraManager.RequestActZoom(ZoomParam.DirectionOut, ZoomParam.Action1Shot);
        }

        private void ScreenImage_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("ScreenImage_Loaded");
            ScreenImage.DataContext = cameraManager.LiveviewImage;
        }

        private void ScreenImage_Unloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("ScreenImage_UnLoaded");
            ScreenImage.DataContext = null;
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            Debug.WriteLine("onbackkey");
            if (MyPivot.SelectedIndex == PIVOTINDEX_LIVEVIEW)
            {
                e.Cancel = true;

                if (IsAppSettingPanelShowing())
                {
                    this.CloseAppSettingPanel();
                    return;
                }

                if (cpm != null && cpm.IsShowing())
                {
                    cpm.Hide();
                    if (ApplicationBar != null)
                    {
                        ApplicationBar.IsVisible = true;
                    }
                    return;
                }

                if (Sliders.Visibility == System.Windows.Visibility.Visible)
                {
                    CloseSliderPanel();
                    return;
                }

                GoToMainPage();

            }
            else
            {
                e.Cancel = false;
            }
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            svd = new ShootingViewData(AppStatus.GetInstance(), cameraManager.cameraStatus);
            ShootingPivot.DataContext = svd;
            IntervalStatusPanel.DataContext = cameraManager.IntervalManager;
            ScreenImageWrapper.DataContext = cameraManager.cameraStatus;
            AudioScreenImage.DataContext = cameraManager.cameraStatus;
            ShootButtonWrapper.DataContext = ApplicationSettings.GetInstance();
            ShootButton.DataContext = svd;
            TouchAFPointer.DataContext = svd;
            Histogram.DataContext = ApplicationSettings.GetInstance();
            GeopositionStatusImage.DataContext = ApplicationSettings.GetInstance();

            cpm.ReplacePanel(ControlPanel);
        }

        private void PhoneApplicationPage_Unloaded(object sender, RoutedEventArgs e)
        {
            ShootingPivot.DataContext = null;
            IntervalStatusPanel.DataContext = null;
            ScreenImageWrapper.DataContext = null;
            AudioScreenImage.DataContext = null;
            ShootButtonWrapper.DataContext = null;
            ShootButton.DataContext = null;
            TouchAFPointer.DataContext = null;
            Histogram.DataContext = null;
            GeopositionStatusImage.DataContext = null;
            svd = null;
        }

        private void PhoneApplicationPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            Debug.WriteLine("OrientationChagned: " + e.Orientation);
            if (cameraManager != null)
            {
                cameraManager.CancelTouchAF();
                cameraManager.CancelHalfPressShutter();
            }
            SetLayoutByOrientation(e.Orientation);
        }

        private void SetLayoutByOrientation(PageOrientation orientation)
        {
            switch (orientation)
            {
                case PageOrientation.LandscapeLeft:
                    AppTitle.Margin = new Thickness(60, 0, 0, 0);
                    UpperLeftElements.Margin = new Thickness(42, 46, 0, 0);
                    StatusDisplayelements.Margin = new Thickness(40, 6, 60, 0);
                    AppSettings.Margin = new Thickness(20, 64, 40, 64);
                    BottomElements.Margin = new Thickness(0, 0, 0, 0);
                    ZoomElements.Margin = new Thickness(50, 0, 0, 0);
                    ShootButtonWrapper.Margin = new Thickness(0, 0, 80, 0);
                    OpenSlider.Margin = new Thickness(50, 0, 0, 0);
                    Sliders.Margin = new Thickness(70, 0, 70, 0);
                    Grid.SetRow(Histogram, 1);
                    Grid.SetColumn(Histogram, 0);
                    Grid.SetRow(IntervalStatusPanel, 2);
                    Grid.SetColumn(IntervalStatusPanel, 0);
                    break;
                case PageOrientation.LandscapeRight:
                    AppTitle.Margin = new Thickness(60, 0, 0, 0);
                    UpperLeftElements.Margin = new Thickness(42, 46, 0, 0);
                    StatusDisplayelements.Margin = new Thickness(40, 6, 60, 0);
                    AppSettings.Margin = new Thickness(36, 64, 16, 64);
                    BottomElements.Margin = new Thickness(0, 0, 0, 0);
                    ZoomElements.Margin = new Thickness(70, 0, 0, 0);
                    ShootButtonWrapper.Margin = new Thickness(0, 0, 80, 0);
                    OpenSlider.Margin = new Thickness(70, 0, 0, 0);
                    Sliders.Margin = new Thickness(70, 0, 70, 0);
                    Grid.SetRow(Histogram, 1);
                    Grid.SetColumn(Histogram, 0);
                    Grid.SetRow(IntervalStatusPanel, 2);
                    Grid.SetColumn(IntervalStatusPanel, 0);
                    break;
                case PageOrientation.PortraitUp:
                    AppTitle.Margin = new Thickness(0, 0, 0, 0);
                    UpperLeftElements.Margin = new Thickness(10, 46, 0, 0);
                    StatusDisplayelements.Margin = new Thickness(10, 6, 0, 0);
                    AppSettings.Margin = new Thickness(-12, 64, 0, 64);
                    BottomElements.Margin = new Thickness(0, 0, 0, 70);
                    ZoomElements.Margin = new Thickness(20, 0, 0, 0);
                    ShootButtonWrapper.Margin = new Thickness(0, 0, 30, 0);
                    OpenSlider.Margin = new Thickness(5, 0, 0, 0);
                    Sliders.Margin = new Thickness(5, 0, 0, 0);
                    Grid.SetRow(Histogram, 1);
                    Grid.SetColumn(Histogram, 0);
                    Grid.SetRow(IntervalStatusPanel, 1);
                    Grid.SetColumn(IntervalStatusPanel, 1);
                    break;
            }
        }

        public void ShowToast(String message)
        {
            Dispatcher.BeginInvoke(() =>
            {
                ToastMessage.Text = message;
                ToastApparance.Begin();
            });
        }

        void ToastApparance_Completed(object sender, EventArgs e)
        {
            Scheduler.Dispatcher.Schedule(() =>
            {
                ToastDisApparance.Begin();
            }
                , TimeSpan.FromSeconds(3));
        }

        void SetPivotIsLocked(bool l)
        {
            if (l)
            {
                Dispatcher.BeginInvoke(() => { MyPivot.IsLocked = true; });
            }
            else
            {
                Dispatcher.BeginInvoke(() => { MyPivot.IsLocked = false; });
            }
        }

        private void initNFC()
        {
            // Initialize NFC
            _proximitiyDevice = ProximityDevice.GetDefault();

            if (_proximitiyDevice == null)
            {
                Debug.WriteLine("It seems this is not NFC available device");
                return;
            }

            _subscriptionIdNdef = _proximitiyDevice.SubscribeForMessage("NDEF", NFCMessageReceivedHandler);
            NFCMessage.Visibility = System.Windows.Visibility.Visible;
        }

        private void NFCMessageReceivedHandler(ProximityDevice sender, ProximityMessage message)
        {
            var parser = new NdefParser(message);
            List<NdefRecord> ndefRecords = new List<NdefRecord>();

            String err = AppResources.ErrorMessage_fatal;
            String caption = AppResources.MessageCaption_error;

            try
            {
                ndefRecords = parser.Parse();
            }
            catch (NoSonyNdefRecordException)
            {
                err = AppResources.ErrorMessage_CantFindSonyRecord;
                Dispatcher.BeginInvoke(() => { MessageBox.Show(err, caption, MessageBoxButton.OK); });
            }
            catch (NoNdefRecordException)
            {
                err = AppResources.ErrorMessage_ParseNFC;
                Dispatcher.BeginInvoke(() => { MessageBox.Show(err, caption, MessageBoxButton.OK); });
            }
            catch (NdefParseException)
            {
                err = AppResources.ErrorMessage_ParseNFC;
                Dispatcher.BeginInvoke(() => { MessageBox.Show(err, caption, MessageBoxButton.OK); });
            }
            catch (Exception)
            {
                err = AppResources.ErrorMessage_fatal;
                Dispatcher.BeginInvoke(() => { MessageBox.Show(err, caption, MessageBoxButton.OK); });
            }

            if (ndefRecords.Count > 0)
            {
                foreach (NdefRecord r in ndefRecords)
                {
                    if (r.SSID.Length > 0 && r.Password.Length > 0)
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            Clipboard.SetText(r.Password);
                            var sb = new StringBuilder();
                            sb.Append(AppResources.Message_NFC_succeed);
                            sb.Append(System.Environment.NewLine);
                            sb.Append(System.Environment.NewLine);
                            sb.Append("SSID: ");
                            sb.Append(r.SSID);
                            sb.Append(System.Environment.NewLine);
                            sb.Append("Password: ");
                            sb.Append(r.Password);
                            sb.Append(System.Environment.NewLine);
                            sb.Append(System.Environment.NewLine);
                            sb.Append(AppResources.NFC_iiwake);
                            MessageBox.Show(sb.ToString(), AppResources.MessageCaption_NFC_Succeed, MessageBoxButton.OK);
                        });
                        break;
                    }
                }
            }
        }

        private void ClearNFCInfo()
        {
            if (_proximitiyDevice != null)
            {
                NFCMessage.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void InitAppSettingPanel()
        {
            AppSettings.Children.Add(new CheckBoxSetting(AppResources.DisplayTakeImageButtonSetting, AppResources.Guide_DisplayTakeImageButtonSetting, CheckBoxSetting.SettingType.displayShootbutton));
            AppSettings.Children.Add(new CheckBoxSetting(AppResources.PostviewTransferSetting, AppResources.Guide_ReceiveCapturedImage, CheckBoxSetting.SettingType.postviewImageTransfer));
            AppSettings.Children.Add(new CheckBoxSetting(AppResources.DisplayHistogram, AppResources.Guide_Histogram, CheckBoxSetting.SettingType.displayHistogram));
            AppSettings.Children.Add(new CheckBoxSetting(AppResources.AddGeotag, AppResources.AddGeotag_guide, CheckBoxSetting.SettingType.geotagEnable));
            HideSettingAnimation.Completed += HideSettingAnimation_Completed;
        }

        private void OpenAppSettingPanel()
        {
            if (cameraManager != null)
            {
                cameraManager.CancelTouchAF();
                cameraManager.CancelHalfPressShutter();
            }
            AppSettingPanel.Visibility = System.Windows.Visibility.Visible;
            ApplicationBar = abm.Clear().Enable(IconMenu.CloseApplicationSetting).CreateNew(APPBAR_OPACITY);
            ShowSettingAnimation.Begin();
        }

        private void CloseAppSettingPanel()
        {

            HideSettingAnimation.Begin();
            ApplicationBar = abm.Clear().Enable(IconMenu.ControlPanel).Enable(IconMenu.ApplicationSetting).CreateNew(APPBAR_OPACITY);
        }

        void HideSettingAnimation_Completed(object sender, EventArgs e)
        {
            AppSettingPanel.Visibility = System.Windows.Visibility.Collapsed;

        }

        private bool IsAppSettingPanelShowing()
        {
            if (AppSettingPanel.Visibility == System.Windows.Visibility.Visible)
            {
                return true;
            }

            return false;
        }

        private void FNumberSlider_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            if (cameraManager == null || cameraManager.cameraStatus == null || cameraManager.cameraStatus.FNumber == null)
            {
                return;
            }

            var v = (sender as Slider).Value;
            var value = (int)Math.Round(v);
            FNumberSlider.Value = value;

            if (value < cameraManager.cameraStatus.FNumber.Candidates.Length)
            {
                cameraManager.SetFNumber(cameraManager.cameraStatus.FNumber.Candidates[value]);
            }
        }

        private void ShutterSpeedSlider_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            if (cameraManager == null || cameraManager.cameraStatus == null || cameraManager.cameraStatus.ShutterSpeed == null)
            {
                return;
            }

            var v = (sender as Slider).Value;
            var value = (int)Math.Round(v);
            ShutterSpeedSlider.Value = value;

            if (value < cameraManager.cameraStatus.ShutterSpeed.Candidates.Length)
            {
                cameraManager.SetShutterSpeed(cameraManager.cameraStatus.ShutterSpeed.Candidates[value]);
            }
        }

        private void EvSlider_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            if (cameraManager == null || cameraManager.cameraStatus == null || cameraManager.cameraStatus.EvInfo == null)
            {
                return;
            }

            var v = (sender as Slider).Value;
            var value = (int)Math.Round(v);
            EvSlider.Value = value;

            if (value >= cameraManager.cameraStatus.EvInfo.Candidate.MinIndex && value <= cameraManager.cameraStatus.EvInfo.Candidate.MaxIndex)
            {
                cameraManager.SetExposureCompensation(value);
            }
        }

        private void IsoSlider_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            if (cameraManager == null || cameraManager.cameraStatus == null || cameraManager.cameraStatus.ISOSpeedRate == null)
            {
                return;
            }

            var v = (sender as Slider).Value;
            var value = (int)Math.Round(v);
            IsoSlider.Value = value;

            if (value < cameraManager.cameraStatus.ISOSpeedRate.Candidates.Length)
            {
                cameraManager.SetIsoSpeedRate(cameraManager.cameraStatus.ISOSpeedRate.Candidates[value]);
            }
        }

        private async void ProgramShiftBar_OnRelease(object sender, OnReleaseArgs e)
        {
            if (cameraManager == null || cameraManager.CameraApi == null)
            {
                return;
            }

            try
            {
                await cameraManager.CameraApi.SetProgramShiftAsync(e.Value);
            }
            catch (RemoteApiException ex)
            {
                Debug.WriteLine("Failed to set program shift: " + ex.code);
            }
        }

        private void OpenSliderPanel()
        {
            Debug.WriteLine("OpenSlider");
            Sliders.Visibility = Visibility.Visible;
            // make shoot button and zoom bar/buttons invisible.
            ApplicationSettings.GetInstance().ShootButtonTemporaryCollapsed = true;
            if (svd != null) { svd.ZoomElementsTemporaryCollapsed = true; }
            StartOpenSliderAnimation(0, 180);
        }

        private void CloseSliderPanel()
        {
            Debug.WriteLine("CloseSlider");
            Sliders.Visibility = Visibility.Collapsed;
            ApplicationSettings.GetInstance().ShootButtonTemporaryCollapsed = false;
            if (svd != null) { svd.ZoomElementsTemporaryCollapsed = false; }
            StartOpenSliderAnimation(180, 0);
        }

        public void StartOpenSliderAnimation(double from, double to)
        {
            var duration = new Duration(TimeSpan.FromMilliseconds(200));
            var sb = new Storyboard();
            sb.Duration = duration;

            var da = new DoubleAnimation();
            da.Duration = duration;

            sb.Children.Add(da);

            var rt = new RotateTransform();

            Storyboard.SetTarget(da, rt);
            Storyboard.SetTargetProperty(da, new PropertyPath("Angle"));
            da.From = from;
            da.To = to;

            OpenSlider.RenderTransform = rt;
            OpenSlider.RenderTransformOrigin = new Point(0.5, 0.5);
            sb.Begin();
        }

        private void OpenSlider_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            if (Sliders.Visibility == Visibility.Collapsed)
            {
                OpenSliderPanel();
            }
            else
            {
                CloseSliderPanel();
            }
            e.Handled = true;
        }

        private void CameraParameters_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            if (!e.Handled)
            {
                if (Sliders.Visibility == Visibility.Collapsed)
                {
                    OpenSliderPanel();
                }
                else
                {
                    CloseSliderPanel();
                }
            }
        }


    }
}
