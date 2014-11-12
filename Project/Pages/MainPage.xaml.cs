using Kazyx.RemoteApi;
using Kazyx.RemoteApi.Camera;
using Kazyx.WPPMM.CameraManager;
using Kazyx.WPPMM.Controls;
using Kazyx.WPPMM.DataModel;
using Kazyx.WPPMM.Resources;
using Kazyx.WPPMM.Utils;
using Microsoft.Devices;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Net.NetworkInformation;
using Microsoft.Phone.Reactive;
using Microsoft.Phone.Tasks;
using NtNfcLib;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Windows.Networking.Proximity;

namespace Kazyx.WPPMM.Pages
{
    public partial class MainPage : PhoneApplicationPage
    {
        private const int PIVOTINDEX_MAIN = 0;
        private const int PIVOTINDEX_LIVEVIEW = 1;

        private readonly CameraManager.CameraManager cameraManager = CameraManager.CameraManager.GetInstance();

        private bool OnZooming;

        private const double APPBAR_OPACITY_ENTRANCE = 0.7;
        private const double APPBAR_OPACITY_LIVEVIEW = 0.0;
        private double AppBarOpacity = 0.7;

        private ShootingViewData svd;
        private readonly PostViewData pvd = new PostViewData();
        private AppBarManager abm = new AppBarManager();
        private ControlPanelManager cpm;

        private ProximityDevice ProximitiyDevice;
        private long SubscriptionIdNdef;

        private const string AP_NAME_PREFIX = "DIRECT-";

        private const bool FilterBySsid = true;

        private static readonly BitmapImage GeoInfoStatusImage_OK = new BitmapImage(new Uri("/Assets/Screen/GeoInfoStatus_OK.png", UriKind.Relative));
        private static readonly BitmapImage GeoInfoStatusImage_NG = new BitmapImage(new Uri("/Assets/Screen/GeoInfoStatus_NG.png", UriKind.Relative));
        private static readonly BitmapImage GeoInfoStatusImage_Updating = new BitmapImage(new Uri("/Assets/Screen/GeoInfoStatus_Updating.png", UriKind.Relative));

        private const string ViewerPageUri = "/Pages/RemoteViewerPage.xaml";

        public MainPage()
        {
            InitializeComponent();

            MyPivot.SelectionChanged += MyPivot_SelectionChanged;

            this.InitAppSettingPanel();

            abm.SetEvent(Menu.About, (sender, e) =>
            {
                NavigationService.Navigate(new Uri("/Pages/AboutPage.xaml", UriKind.Relative));
            });
#if DEBUG
            abm.SetEvent(Menu.Log, (sender, e) =>
            {
                NavigationService.Navigate(new Uri("/Pages/LogViewerPage.xaml", UriKind.Relative));
            });
            abm.SetEvent(Menu.Contents, (sender, e) =>
            {
                NavigationService.Navigate(new Uri(ViewerPageUri, UriKind.Relative));
            });
#endif
            abm.SetEvent(IconMenu.WiFi, (sender, e) => { var task = new ConnectionSettingsTask { ConnectionSettingsType = ConnectionSettingsType.WiFi }; task.Show(); });
            abm.SetEvent(IconMenu.ControlPanel, (sender, e) =>
            {
                if (abm != null)
                {
                    ApplicationBar = abm.Disable(IconMenu.TouchAfCancel).CreateNew(AppBarOpacity);
                }
                ApplicationBar.IsVisible = false;
                if (cameraManager != null)
                {
                    cameraManager.CancelTouchAF();
                    cameraManager.CancelHalfPressShutter();
                }
                if (Sliders.Visibility == System.Windows.Visibility.Visible)
                {
                    CloseSliderPanel();
                }
                cpm.Show();
            });
            abm.SetEvent(IconMenu.ApplicationSetting, (sender, e) => { this.OpenAppSettingPanel(); });
            abm.SetEvent(IconMenu.Ok, (sender, e) => { this.CloseAppSettingPanel(); });
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
                NavigationService.Navigate(new Uri(ViewerPageUri, UriKind.Relative));
            });
            abm.SetEvent(IconMenu.Hidden, (sender, e) => { NavigationService.Navigate(new Uri("/Pages/HiddenPage.xaml", UriKind.Relative)); });

            cpm = new ControlPanelManager(ControlPanel);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            DebugUtil.Log(e.Uri.OriginalString);
            progress.IsVisible = false;
            InitializeApplication();

            cameraManager.OnDisconnected += cameraManager_OnDisconnected;
            cameraManager.WifiInfoUpdated += WifiInfoUpdated;
            cameraManager.Downloader.QueueStatusUpdated += OnFetchingImages;
            cameraManager.OnRemoteClientError += cameraManager_OnRemoteClientError;
            cameraManager.PictureFetchFailed += cameraManager_PictureFetchFailed;
            cameraManager.PictureFetchStatusUpdated += cameraManager_PictureFetchStatusUpdated;
            cameraManager.PictureFetchSucceed += cameraManager_PictureFetchSucceed;
            cameraManager.OnTakePictureSucceed += cameraManager_OnTakePictureSucceed;
            cameraManager.MethodTypesUpdateNotifer += SupportedApiUpdated;

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

            if (!FilterBySsid || GetSSIDName().StartsWith(AP_NAME_PREFIX))
            {
                if (MyPivot.SelectedIndex != PIVOTINDEX_LIVEVIEW)
                {
                    StartConnectionSequence(true);
                }
            }

            ActivateGeoTagSetting(true);
            DisplayGridColorSetting(ApplicationSettings.GetInstance().GridType != FramingGridTypes.Off);
            DisplayFibonacciOriginSetting(ApplicationSettings.GetInstance().GridType == FramingGridTypes.Fibonacci);
        }

        void cameraManager_OnTakePictureSucceed()
        {
            if (!ApplicationSettings.GetInstance().IsPostviewTransferEnabled ||
                cameraManager.IntervalManager.IsRunning)
            {
                ShowToast(AppResources.Message_ImageCapture_Succeed);
            }
        }

        private void cameraManager_PictureFetchSucceed(Windows.Devices.Geolocation.Geoposition pos)
        {
            if (ApplicationSettings.GetInstance().GeotagEnabled && pos != null)
            {
                ShowToast(AppResources.Message_ImageDL_Succeed_withGeotag);
            }
            else if (ApplicationSettings.GetInstance().GeotagEnabled)
            {
                MessageBox.Show(AppResources.ErrorMessage_FailedToGetGeoposition);
            }
            else
            {
                ShowToast(AppResources.Message_ImageDL_Succeed);
            }
        }

        void cameraManager_PictureFetchStatusUpdated(int amount)
        {
            if (amount == 0)
            {
                AppStatus.GetInstance().IsDownloadingImages = false;
            }
            else
            {
                AppStatus.GetInstance().IsDownloadingImages = true;
            }
        }

        void cameraManager_PictureFetchFailed(ImageDLError err)
        {
            var error = "";
            var isOriginal = false;
            if (cameraManager.Status.PostviewSizeInfo != null
                && cameraManager.Status.PostviewSizeInfo.Current == "Original")
            {
                isOriginal = true;
            }

            switch (err)
            {
                case ImageDLError.Gone:
                    error = AppResources.ErrorMessage_ImageDL_Gone;
                    break;
                case ImageDLError.Network:
                    error = AppResources.ErrorMessage_ImageDL_Network;
                    break;
                case ImageDLError.Saving:
                case ImageDLError.DeviceInternal:
                    if (isOriginal)
                    {
                        error = AppResources.ErrorMessage_ImageDL_SavingOriginal;
                    }
                    else
                    {
                        error = AppResources.ErrorMessage_ImageDL_Saving;
                    }
                    break;
                case ImageDLError.GeotagAlreadyExists:
                    error = AppResources.ErrorMessage_ImageDL_DuplicatedGeotag;
                    break;
                case ImageDLError.GeotagAddition:
                    error = AppResources.ErrorMessage_ImageDL_Geotagging;
                    break;
                case ImageDLError.Unknown:
                case ImageDLError.Argument:
                default:
                    if (isOriginal)
                    {
                        error = AppResources.ErrorMessage_ImageDL_OtherOriginal;
                    }
                    else
                    {
                        error = AppResources.ErrorMessage_ImageDL_Other;
                    }
                    break;
            }
            MessageBox.Show(error, AppResources.MessageCaption_error, MessageBoxButton.OK);
            DebugUtil.Log(error);
        }

        void cameraManager_OnRemoteClientError(StatusCode code)
        {
            var err = "";

            if (cameraManager.IntervalManager.IsRunning)
            {
                err = AppResources.ErrorMessage_Interval + System.Environment.NewLine + System.Environment.NewLine + "Error code: " + code;
            }
            else
            {
                switch (code)
                {
                    case StatusCode.Any:
                        err = AppResources.ErrorMessage_fatal;
                        break;
                    case StatusCode.Timeout:
                        err = AppResources.ErrorMessage_timeout;
                        break;
                    case StatusCode.ShootingFailure:
                        err = AppResources.ErrorMessage_shootingFailure;
                        break;
                    case StatusCode.CameraNotReady:
                        err = AppResources.ErrorMessage_cameraNotReady;
                        break;
                    case StatusCode.Forbidden:
                        err = AppResources.BuiltInSRNotSupported;
                        break;
                    default:
                        err = AppResources.ErrorMessage_fatal;
                        break;
                }
            }
            err = err + System.Environment.NewLine + System.Environment.NewLine + "Error code: " + code;
            MessageBox.Show(err, AppResources.MessageCaption_error, MessageBoxButton.OK);
        }

        internal void cameraManager_OnDisconnected()
        {
            DebugUtil.Log("## Disconnected");
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
            InitializeProximityDevice();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            cameraManager.OnDisconnected -= cameraManager_OnDisconnected;
            cameraManager.WifiInfoUpdated -= WifiInfoUpdated;
            cameraManager.Downloader.QueueStatusUpdated -= OnFetchingImages;
            cameraManager.OnRemoteClientError -= cameraManager_OnRemoteClientError;
            cameraManager.PictureFetchFailed -= cameraManager_PictureFetchFailed;
            cameraManager.PictureFetchStatusUpdated -= cameraManager_PictureFetchStatusUpdated;
            cameraManager.PictureFetchSucceed -= cameraManager_PictureFetchSucceed;
            cameraManager.OnTakePictureSucceed -= cameraManager_OnTakePictureSucceed;
            cameraManager.MethodTypesUpdateNotifer -= SupportedApiUpdated;

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
        }

        private void StartConnectionSequence(bool connect)
        {
            progress.Text = AppResources.ProgressMessageDetectingDevice;
            progress.IsVisible = true;
            CameraManager.CameraManager.GetInstance().RequestSearchDevices(() =>
            {
                DebugUtil.Log("DeviceFound -> GoToShootingPage if required.");
                progress.IsVisible = false;
                if (connect) GoToShootingPage();
            }, () =>
            {
                DebugUtil.Log("Discovery timeout.");
                progress.IsVisible = false;
            });
        }

        private void HandleError(int code)
        {
            DebugUtil.Log("Error: " + code);
        }

        private void UpdateNetworkStatus()
        {
            var ssid = GetSSIDName();
            DebugUtil.Log("SSID: " + ssid);
            if (ssid != null && ssid.StartsWith(AP_NAME_PREFIX))
            {
                NetworkStatus.Text = AppResources.Guide_CantFindDevice;
            }
            else
            {
                NetworkStatus.Text = AppResources.Guide_WiFiNotEnabled;
            }

            if (cameraManager.CurrentDeviceInfo != null)
            {
                var modelName = cameraManager.CurrentDeviceInfo.FriendlyName;
                if (modelName != null)
                {
                    NetworkStatus.Text = AppResources.ConnectedDevice.Replace("_ssid_", modelName);
                    GuideMessage.Visibility = System.Windows.Visibility.Visible;
                }
            }
            else
            {
                NetworkStatus.Text = AppResources.Guide_CantFindDevice;
                GuideMessage.Visibility = System.Windows.Visibility.Collapsed;
            }

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

        internal void WifiInfoUpdated(CameraStatus cameraStatus)
        {
            UpdateNetworkStatus();
        }

        private string GetSSIDName()
        {
            foreach (var network in new NetworkInterfaceList())
            {
                if ((network.InterfaceType == NetworkInterfaceType.Wireless80211) &&
                    (network.InterfaceState == ConnectState.Connected))
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

        private async void CameraButtons_ShutterKeyPressed(object sender, EventArgs e)
        {
            if (cameraManager == null || cpm == null || cpm.IsShowing() || IsAppSettingPanelShowing()) { return; }
            if (StartContShootingAvailable())
            {
                await cameraManager.CameraApi.StartContShootingAsync();
                return;
            }
            RecStartStop();
        }

        private async void CameraButtons_ShutterKeyReleased(object sender, EventArgs e)
        {
            if (cameraManager == null || cpm == null || cpm.IsShowing() || IsAppSettingPanelShowing()) { return; }
            if (StopContShootingAvailable())
            { await cameraManager.CameraApi.StopContShootingAsync(); }
            cameraManager.CancelHalfPressShutter();
        }

        void CameraButtons_ShutterKeyHalfPressed(object sender, EventArgs e)
        {
            if (cameraManager == null || cpm == null || cpm.IsShowing() || IsAppSettingPanelShowing()) { return; }

            cameraManager.RequestHalfPressShutter();
        }

        private void takeImageButton_Click(object sender, RoutedEventArgs e)
        {
            RecStartStop();
        }

        private bool StartContShootingAvailable()
        {
            if (!cameraManager.IntervalManager.IsRunning &&
                cameraManager.Status != null &&
                cameraManager.Status.Status == EventParam.Idle &&
                cameraManager.Status.ShootMode.Current == ShootModeParam.Still &&
                cameraManager.Status.ContShootingMode != null &&
                (
                    cameraManager.Status.ContShootingMode.Current == ContinuousShootMode.Cont ||
                    cameraManager.Status.ContShootingMode.Current == ContinuousShootMode.SpeedPriority)
                )
            { return true; }
            return false;
        }

        private bool StopContShootingAvailable()
        {
            if (!cameraManager.IntervalManager.IsRunning &&
                cameraManager.Status != null &&
                cameraManager.Status.Status == EventParam.StCapturing &&
                cameraManager.Status.ShootMode.Current == ShootModeParam.Still &&
                cameraManager.Status.ContShootingMode != null &&
                (
                    cameraManager.Status.ContShootingMode.Current == ContinuousShootMode.Cont ||
                    cameraManager.Status.ContShootingMode.Current == ContinuousShootMode.SpeedPriority)
                )
            { return true; }
            return false;
        }

        private async void ShootButton_ManipulationStarted(object sender, System.Windows.Input.ManipulationStartedEventArgs e)
        {
            if (StartContShootingAvailable()) { await cameraManager.CameraApi.StartContShootingAsync(); }
        }

        private async void RecStartStop()
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

            var status = cameraManager.Status;
            switch (status.Status)
            {
                case EventParam.Idle:
                    switch (status.ShootMode.Current)
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
                case EventParam.StCapturing:
                    if (cameraManager.Status.ContShootingMode.Current == ContinuousShootMode.Cont ||
                cameraManager.Status.ContShootingMode.Current == ContinuousShootMode.SpeedPriority)
                    {
                        await cameraManager.CameraApi.StopContShootingAsync();
                    }
                    break;
            }
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
            cameraManager.ShowToast += ShowToast;
            cameraManager.OnFocusFrameRetrived += cameraManager_OnFocusFrameRetrived;
            ToastApparance.Completed += ToastApparance_Completed;
            FraimingGrids.ManipulationCompleted += FraimingGrids_ManipulationCompleted;

            CameraButtons.ShutterKeyPressed += CameraButtons_ShutterKeyPressed;
            CameraButtons.ShutterKeyHalfPressed += CameraButtons_ShutterKeyHalfPressed;
            CameraButtons.ShutterKeyReleased += CameraButtons_ShutterKeyReleased;

            cameraManager.OnAfStatusChanged += cameraManager_OnAfStatusChanged;
            cameraManager.OnExposureModeChanged += cameraManager_OnExposureModeChanged;

            if (svd != null)
            {
                svd.SlidersVisibilityChanged += SlidersVisibilityChanged;
            }

            await Task.Delay(500);

            if (cameraManager.IsClientReady())
            {
                progress.Text = AppResources.ProgressMessageConnecting;
                progress.IsVisible = true;
                cameraManager.RunEventObserver();
                await cameraManager.OperateInitialProcess();
                Dispatcher.BeginInvoke(() => { progress.IsVisible = false; });
            }
            else if (FilterBySsid && !GetSSIDName().StartsWith(AP_NAME_PREFIX))
            {
                Dispatcher.BeginInvoke(() => { GoToMainPage(); });
                return;
            }
            else
            {
                DebugUtil.Log("Await for async device discovery");
                AppStatus.GetInstance().IsSearchingDevice = true;
                Dispatcher.BeginInvoke(() =>
                {
                    progress.Text = AppResources.ProgressMessageConnecting;
                    progress.IsVisible = true;
                });
                var found = await PrepareConnectionAsync();
                Dispatcher.BeginInvoke(() => { AppStatus.GetInstance().IsSearchingDevice = false; });

                DebugUtil.Log("Async device discovery result: " + found);
                if (found)
                {
                    cameraManager.RunEventObserver();
                    await cameraManager.OperateInitialProcess();
                    Dispatcher.BeginInvoke(() => { progress.IsVisible = false; });
                }
                else
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        progress.IsVisible = false;
                        GoToMainPage();
                    });
                    return;
                }
            }

            AppBarOpacity = APPBAR_OPACITY_LIVEVIEW;
            abm.Clear();
            if (cpm != null && cpm.ItemCount > 0)
            {
                abm.Enable(IconMenu.ControlPanel);
            }
            abm.Enable(IconMenu.ApplicationSetting).Enable(IconMenu.CameraRoll);

            Dispatcher.BeginInvoke(() => { if (cpm != null) cpm.Hide(); ApplicationBar = abm.CreateNew(AppBarOpacity); });

            InitializeHitogram();

            cameraManager.OnHistogramUpdated += cameraManager_OnHistogramUpdated;
            GeopositionManager.GetInstance().GeopositionUpdated += GeopositionStatusUpdated;
            GeopositionManager.GetInstance().Enable = ApplicationSettings.GetInstance().GeotagEnabled;
            if (ApplicationSettings.GetInstance().GeotagEnabled)
            {
                await GeopositionManager.GetInstance().AcquireGeoPosition();
            }
        }

        private void SupportedApiUpdated()
        {
            var available = cameraManager.Status.IsSupported("setLiveviewFrameInfo");
            DebugUtil.Log("Focus frame setting visibility: " + available);
            if (FocusFrameSetting == null) { return; }
            if (available)
            {
                FocusFrameSetting.SettingVisibility = System.Windows.Visibility.Visible;
            }
            else
            {
                FocusFrameSetting.SettingVisibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void cameraManager_OnFocusFrameRetrived(ImageStream.FocusFramePacket p)
        {
            if (ApplicationSettings.GetInstance().RequestFocusFrameInfo)
            {
                FocusFrames.SetFocusFrames(p.FocusFrames);
            }
        }

        void cameraManager_OnExposureModeChanged(string obj)
        {
            if (Sliders.Visibility == System.Windows.Visibility.Visible)
            {
                this.CloseSliderPanel();
            }
        }

        private void OnFetchingImages(int count)
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (count != 0)
                {
                    progress.Text = AppResources.ProgressMessageFetching;
                    progress.IsVisible = true;
                }
                else
                {
                    progress.IsVisible = false;
                }
            });
        }

        internal void GeopositionStatusUpdated(GeopositionEventArgs args)
        {
            DebugUtil.Log("Geoposition status updated: " + args.Status);
            Dispatcher.BeginInvoke(() =>
            {
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
                    case GeopositiomManagerStatus.Unauthorized:
                        GeopositionStatusImage.Source = GeoInfoStatusImage_NG;
                        if (geoSetting != null)
                        {
                            geoSetting.CurrentSetting = false;
                        }
                        ActivateGeoTagSetting(false);
                        // MessageBox.Show(AppResources.ErrorMessage_LocationAccessUnauthorized);
                        break;
                }
            });
        }

        private void SlidersVisibilityChanged(System.Windows.Visibility visibility)
        {
            DebugUtil.Log("Slider visibility changed: " + visibility);
            if (visibility == System.Windows.Visibility.Collapsed)
            {
                CloseSliderPanel();
            }
        }

        private void InitializeHitogram()
        {
            Histogram.Init(WPPMM.Controls.Histogram.ColorType.White, 1500);
        }

        private void cameraManager_OnHistogramUpdated(int[] r, int[] g, int[] b)
        {
            Histogram.SetHistogramValue(r, g, b);
        }

        void cameraManager_OnAfStatusChanged(CameraStatus status)
        {
            if (status.FocusStatus == null)
            {
                return;
            }

            if (status.AfType == CameraStatus.AutoFocusType.Touch &&
                ((status.TouchFocusStatus == null && status.FocusStatus != FocusState.Released) || // QX10/100 may not return TouchFocusStatus record
                (status.TouchFocusStatus != null && status.TouchFocusStatus.Focused))) // SmartRemote gives TouchFocusStatus
            {
                // focus locked.
                if (!abm.IsEnabled(IconMenu.TouchAfCancel))
                {
                    if (cpm != null && !cpm.IsShowing())
                    {
                        ApplicationBar = abm.Enable(IconMenu.TouchAfCancel).CreateNew(AppBarOpacity);
                    }
                    else
                    {
                        abm.Enable(IconMenu.TouchAfCancel);
                    }
                }
            }
            else
            {
                // touch AF cancelled.
                if (abm.IsEnabled(IconMenu.TouchAfCancel))
                {
                    if (cpm != null && !cpm.IsShowing())
                    {
                        ApplicationBar = abm.Disable(IconMenu.TouchAfCancel).CreateNew(AppBarOpacity);
                    }
                    else
                    {
                        abm.Disable(IconMenu.TouchAfCancel);
                    }
                }
            }
        }

        void FraimingGrids_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
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
            var grids = sender as FramingGrids;
            var touchX = e.ManipulationOrigin.X;
            var touchY = e.ManipulationOrigin.Y;

            var posX = touchX * 100.0 / grids.ActualWidth;
            var posY = touchY * 100.0 / grids.ActualHeight;

            Dispatcher.BeginInvoke(() =>
            {
                TouchAFPointer.Margin = new Thickness(touchX - TouchAFPointer.Width / 2, touchY - TouchAFPointer.Height / 2, 0, 0);
            });

            // DebugUtil.Log("tx: " + touchX + " ty: " + touchY);
            DebugUtil.Log("touch position X: " + posX + " Y: " + posY);

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

            cameraManager.ShowToast -= ShowToast;
            cameraManager.OnFocusFrameRetrived -= cameraManager_OnFocusFrameRetrived;
            ToastApparance.Completed -= ToastApparance_Completed;
            CameraButtons.ShutterKeyPressed -= CameraButtons_ShutterKeyPressed;
            CameraButtons.ShutterKeyHalfPressed -= CameraButtons_ShutterKeyHalfPressed;
            CameraButtons.ShutterKeyReleased -= CameraButtons_ShutterKeyReleased;

            cameraManager.OnAfStatusChanged -= cameraManager_OnAfStatusChanged;
            cameraManager.OnExposureModeChanged -= cameraManager_OnExposureModeChanged;
            FraimingGrids.ManipulationCompleted -= FraimingGrids_ManipulationCompleted;
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
            AppBarOpacity = APPBAR_OPACITY_ENTRANCE;
#if DEBUG
            ApplicationBar = abm.Clear().Enable(Menu.Log).Enable(Menu.Contents).Enable(IconMenu.WiFi).Enable(IconMenu.Hidden).CreateNew(AppBarOpacity);
#else
            ApplicationBar = abm.Clear().Enable(IconMenu.WiFi).Enable(IconMenu.Hidden).CreateNew(AppBarOpacity);
#endif
        }

        private void OnZoomInClick(object sender, RoutedEventArgs e)
        {
            DebugUtil.Log("Stop Zoom In (if started)");
            if (OnZooming)
            {
                cameraManager.RequestActZoom(ZoomParam.DirectionIn, ZoomParam.ActionStop);
            }
        }

        private void OnZoomOutClick(object sender, RoutedEventArgs e)
        {
            DebugUtil.Log("Stop zoom out (if started)");
            if (OnZooming)
            {
                cameraManager.RequestActZoom(ZoomParam.DirectionOut, ZoomParam.ActionStop);
            }
        }

        private void OnZoomInHold(object sender, System.Windows.Input.GestureEventArgs e)
        {
            DebugUtil.Log("Zoom In: Start");
            cameraManager.RequestActZoom(ZoomParam.DirectionIn, ZoomParam.ActionStart);
            OnZooming = true;
        }

        private void OnZoomOutHold(object sender, System.Windows.Input.GestureEventArgs e)
        {
            DebugUtil.Log("Zoom Out: Start");
            cameraManager.RequestActZoom(ZoomParam.DirectionOut, ZoomParam.ActionStart);
            OnZooming = true;
        }

        private void OnZoomInTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            DebugUtil.Log("Zoom In: OneShot");
            cameraManager.RequestActZoom(ZoomParam.DirectionIn, ZoomParam.Action1Shot);
        }

        private void OnZoomOutTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            DebugUtil.Log("Zoom In: OneShot");
            cameraManager.RequestActZoom(ZoomParam.DirectionOut, ZoomParam.Action1Shot);
        }

        private void ScreenImage_Loaded(object sender, RoutedEventArgs e)
        {
            DebugUtil.Log("ScreenImage_Loaded");
            ScreenImage.DataContext = cameraManager.LiveviewImage;
        }

        private void ScreenImage_Unloaded(object sender, RoutedEventArgs e)
        {
            DebugUtil.Log("ScreenImage_UnLoaded");
            ScreenImage.DataContext = null;
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            DebugUtil.Log("onbackkey");
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
                    CloseControlPanel();
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

        private void CloseControlPanel()
        {
            if (cpm != null)
            {
                cpm.Hide();
            }
            if (ApplicationBar != null)
            {
                ApplicationBar.IsVisible = true;
            }
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            svd = new ShootingViewData(AppStatus.GetInstance(), cameraManager.Status);
            ShootingPivot.DataContext = svd;
            IntervalStatusPanel.DataContext = cameraManager.IntervalManager;
            ScreenImageWrapper.DataContext = cameraManager.Status;
            AudioScreenImage.DataContext = cameraManager.Status;
            ShootButtonWrapper.DataContext = ApplicationSettings.GetInstance();
            ShootButton.DataContext = svd;
            TouchAFPointer.DataContext = svd;
            Histogram.DataContext = ApplicationSettings.GetInstance();
            GeopositionStatusImage.DataContext = ApplicationSettings.GetInstance();
            this.FraimingGrids.DataContext = ApplicationSettings.GetInstance();

            cpm.ReplacePanel(ControlPanel);
        }

        private void PhoneApplicationPage_Unloaded(object sender, RoutedEventArgs e)
        {
            /*
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
             * */
        }

        private void PhoneApplicationPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            DebugUtil.Log("OrientationChagned: " + e.Orientation);
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
                    AppSettingPanel.Margin = new Thickness(0, -36, 0, 24);
                    BottomElements.Margin = new Thickness(0);
                    ZoomElements.Margin = new Thickness(50, 0, 0, 0);
                    ShootButtonWrapper.Margin = new Thickness(0, 0, 80, 0);
                    OpenSlider.Margin = new Thickness(60, 0, 0, 0);
                    Sliders.Margin = new Thickness(70, 0, 70, 0);
                    EntrancePivot.Margin = new Thickness(70, 0, 70, 0);
                    NFCMessage.Margin = new Thickness(30, 20, 30, 30);
                    Grid.SetRow(Histogram, 1);
                    Grid.SetColumn(Histogram, 0);
                    Grid.SetRow(IntervalStatusPanel, 2);
                    Grid.SetColumn(IntervalStatusPanel, 0);
                    SupportItems.Margin = new Thickness(70, 0, 70, 0);
                    break;
                case PageOrientation.LandscapeRight:
                    AppTitle.Margin = new Thickness(60, 0, 0, 0);
                    UpperLeftElements.Margin = new Thickness(42, 46, 0, 0);
                    StatusDisplayelements.Margin = new Thickness(40, 6, 60, 0);
                    AppSettings.Margin = new Thickness(36, 64, 16, 64);
                    AppSettingPanel.Margin = new Thickness(0, -36, 0, 24);
                    BottomElements.Margin = new Thickness(0);
                    ZoomElements.Margin = new Thickness(90, 0, 0, 0);
                    ShootButtonWrapper.Margin = new Thickness(0, 0, 80, 0);
                    OpenSlider.Margin = new Thickness(90, 0, 0, 0);
                    Sliders.Margin = new Thickness(70, 0, 70, 0);
                    EntrancePivot.Margin = new Thickness(70, 0, 70, 0);
                    NFCMessage.Margin = new Thickness(30, 20, 30, 30);
                    Grid.SetRow(Histogram, 1);
                    Grid.SetColumn(Histogram, 0);
                    Grid.SetRow(IntervalStatusPanel, 2);
                    Grid.SetColumn(IntervalStatusPanel, 0);
                    SupportItems.Margin = new Thickness(70, 0, 70, 0);
                    break;
                case PageOrientation.PortraitUp:
                    AppTitle.Margin = new Thickness(0);
                    UpperLeftElements.Margin = new Thickness(10, 46, 0, 0);
                    StatusDisplayelements.Margin = new Thickness(10, 6, 0, 0);
                    AppSettings.Margin = new Thickness(-12, 64, 0, 74);
                    AppSettingPanel.Margin = new Thickness(0, -36, 0, 90);
                    BottomElements.Margin = new Thickness(0, 0, 0, 70);
                    ZoomElements.Margin = new Thickness(20, 0, 0, 0);
                    ShootButtonWrapper.Margin = new Thickness(0, 0, 30, 0);
                    OpenSlider.Margin = new Thickness(5, 0, 0, 0);
                    Sliders.Margin = new Thickness(5, 0, 0, 0);
                    EntrancePivot.Margin = new Thickness(0);
                    NFCMessage.Margin = new Thickness(30, 50, 30, 30);
                    Grid.SetRow(Histogram, 1);
                    Grid.SetColumn(Histogram, 0);
                    Grid.SetRow(IntervalStatusPanel, 1);
                    Grid.SetColumn(IntervalStatusPanel, 1);
                    SupportItems.Margin = new Thickness(0, 0, 0, 70);
                    break;
            }
        }

        public void ShowToast(string message)
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
            }, TimeSpan.FromSeconds(3));
        }

        void SetPivotIsLocked(bool l)
        {
            Dispatcher.BeginInvoke(() => { MyPivot.IsLocked = l; });
        }

        private void InitializeProximityDevice()
        {
            try
            {
                ProximitiyDevice = ProximityDevice.GetDefault();
            }
            catch (System.IO.FileNotFoundException)
            {
                ProximitiyDevice = null;
                DebugUtil.Log("Caught ununderstandable exception. ");
                return;
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                ProximitiyDevice = null;
                DebugUtil.Log("Caught ununderstandable exception. ");
                return;
            }

            if (ProximitiyDevice == null)
            {
                DebugUtil.Log("It seems this is not NFC available device");
                return;
            }

            try
            {
                SubscriptionIdNdef = ProximitiyDevice.SubscribeForMessage("NDEF", NFCMessageReceivedHandler);
            }
            catch (Exception e)
            {
                ProximitiyDevice = null;
                DebugUtil.Log("Caught ununderstandable exception. " + e.Message + e.StackTrace);
                return;
            }

            NFCMessage.Visibility = System.Windows.Visibility.Visible;
        }

        private void NFCMessageReceivedHandler(ProximityDevice sender, ProximityMessage message)
        {
            var parser = new NdefParser(message);
            var ndefRecords = new List<NdefRecord>();

            var err = AppResources.ErrorMessage_fatal;
            var caption = AppResources.MessageCaption_error;

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
            if (ProximitiyDevice != null)
            {
                NFCMessage.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private AppSettingData<bool> geoSetting;
        private AppSettingData<int> gridColorSetting;
        private AppSettingData<int> fibonacciOriginSetting;
        private AppSettingData<bool> FocusFrameSetting;

        private void InitAppSettingPanel()
        {
            var image_settings = new SettingSection(AppResources.SettingSection_Image);

            AppSettings.Children.Add(image_settings);

            image_settings.Add(new CheckBoxSetting(
                new AppSettingData<bool>(AppResources.PostviewTransferSetting, AppResources.Guide_ReceiveCapturedImage,
                () => { return ApplicationSettings.GetInstance().IsPostviewTransferEnabled; },
                enabled => { ApplicationSettings.GetInstance().IsPostviewTransferEnabled = enabled; })));

            geoSetting = new AppSettingData<bool>(AppResources.AddGeotag, AppResources.AddGeotag_guide,
                () => { return ApplicationSettings.GetInstance().GeotagEnabled; },
                enabled => { ApplicationSettings.GetInstance().GeotagEnabled = enabled; GeopositionManager.GetInstance().Enable = enabled; });
            image_settings.Add(new CheckBoxSetting(geoSetting));

            var display_settings = new SettingSection(AppResources.SettingSection_Display);

            AppSettings.Children.Add(display_settings);

            display_settings.Add(new CheckBoxSetting(
                new AppSettingData<bool>(AppResources.DisplayTakeImageButtonSetting, AppResources.Guide_DisplayTakeImageButtonSetting,
                () => { return ApplicationSettings.GetInstance().IsShootButtonDisplayed; },
                enabled => { ApplicationSettings.GetInstance().IsShootButtonDisplayed = enabled; })));

            display_settings.Add(new CheckBoxSetting(
                new AppSettingData<bool>(AppResources.DisplayHistogram, AppResources.Guide_Histogram,
                () => { return ApplicationSettings.GetInstance().IsHistogramDisplayed; },
                enabled => { ApplicationSettings.GetInstance().IsHistogramDisplayed = enabled; })));

            FocusFrameSetting = new AppSettingData<bool>(AppResources.FocusFrameDisplay, AppResources.Guide_FocusFrameDisplay,
                () => { return ApplicationSettings.GetInstance().RequestFocusFrameInfo; },
                enabled =>
                {
                    ApplicationSettings.GetInstance().RequestFocusFrameInfo = enabled;
                    cameraManager.FocusFrameSettingChanged(enabled);
                    if (!enabled) { FocusFrames.ClearFrames(); }
                });
            display_settings.Add(new CheckBoxSetting(FocusFrameSetting));

            display_settings.Add(new ListPickerSetting(
                new AppSettingData<int>(AppResources.FramingGrids, AppResources.Guide_FramingGrids,
                    () => { return ApplicationSettings.GetInstance().GridTypeIndex; },
                    setting =>
                    {
                        ApplicationSettings.GetInstance().GridTypeIndex = setting;
                        DisplayGridColorSetting(ApplicationSettings.GetInstance().GridTypeSettings[setting] != FramingGridTypes.Off);
                        DisplayFibonacciOriginSetting(ApplicationSettings.GetInstance().GridTypeSettings[setting] == FramingGridTypes.Fibonacci);
                    },
                    SettingsValueConverter.FromFramingGrid(ApplicationSettings.GetInstance().GridTypeSettings.ToArray())
                    )));

            gridColorSetting = new AppSettingData<int>(AppResources.FramingGridColor, null,
                    () => { return ApplicationSettings.GetInstance().GridColorIndex; },
                    setting => { ApplicationSettings.GetInstance().GridColorIndex = setting; },
                    SettingsValueConverter.FromFramingGridColor(ApplicationSettings.GetInstance().GridColorSettings.ToArray()));
            display_settings.Add(new ListPickerSetting(gridColorSetting));

            fibonacciOriginSetting = new AppSettingData<int>(AppResources.FibonacciSpiralOrigin, null,
                () => { return ApplicationSettings.GetInstance().FibonacciOriginIndex; },
                setting => { ApplicationSettings.GetInstance().FibonacciOriginIndex = setting; },
                SettingsValueConverter.FromFibonacciLineOrigin(ApplicationSettings.GetInstance().FibonacciLineOriginSettings.ToArray()));
            display_settings.Add(new ListPickerSetting(fibonacciOriginSetting));

            HideSettingAnimation.Completed += HideSettingAnimation_Completed;
        }

        private void ActivateGeoTagSetting(bool activate)
        {
            if (geoSetting != null)
            {
                geoSetting.Guide = activate ? AppResources.AddGeotag_guide : AppResources.ErrorMessage_LocationAccessUnauthorized;
                geoSetting.IsActive = activate;
            }
        }

        private void DisplayGridColorSetting(bool displayed)
        {
            if (gridColorSetting != null)
            {
                gridColorSetting.IsActive = displayed;
            }
        }

        private void DisplayFibonacciOriginSetting(bool displayed)
        {
            if (fibonacciOriginSetting != null)
            {
                fibonacciOriginSetting.IsActive = displayed;
            }
        }

        private void OpenAppSettingPanel()
        {
            if (cameraManager != null)
            {
                cameraManager.CancelTouchAF();
                cameraManager.CancelHalfPressShutter();
            }
            AppSettingPanel.Visibility = System.Windows.Visibility.Visible;
            ApplicationBar = abm.Clear().Enable(IconMenu.Ok).CreateNew(AppBarOpacity);
            ShowSettingAnimation.Begin();
        }

        private void CloseAppSettingPanel()
        {
            HideSettingAnimation.Begin();
            ApplicationBar = abm.Clear().Enable(IconMenu.ControlPanel).Enable(IconMenu.ApplicationSetting).Enable(IconMenu.CameraRoll).CreateNew(AppBarOpacity);
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
            if (cameraManager == null || cameraManager.Status == null || cameraManager.Status.FNumber == null)
            {
                return;
            }

            var v = (sender as Slider).Value;
            var value = (int)Math.Round(v);
            FNumberSlider.Value = value;

            if (value < cameraManager.Status.FNumber.Candidates.Count)
            {
                cameraManager.SetFNumber(cameraManager.Status.FNumber.Candidates[value]);
            }
        }

        private void ShutterSpeedSlider_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            if (cameraManager == null || cameraManager.Status == null || cameraManager.Status.ShutterSpeed == null)
            {
                return;
            }

            var v = (sender as Slider).Value;
            var value = (int)Math.Round(v);
            ShutterSpeedSlider.Value = value;

            if (value < cameraManager.Status.ShutterSpeed.Candidates.Count)
            {
                cameraManager.SetShutterSpeed(cameraManager.Status.ShutterSpeed.Candidates[value]);
            }
        }

        private void EvSlider_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            if (cameraManager == null || cameraManager.Status == null || cameraManager.Status.EvInfo == null)
            {
                return;
            }

            var v = (sender as Slider).Value;
            var value = (int)Math.Round(v);
            EvSlider.Value = value;

            if (value >= cameraManager.Status.EvInfo.Candidate.MinIndex && value <= cameraManager.Status.EvInfo.Candidate.MaxIndex)
            {
                cameraManager.SetExposureCompensation(value);
            }
        }

        private void IsoSlider_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            if (cameraManager == null || cameraManager.Status == null || cameraManager.Status.ISOSpeedRate == null)
            {
                return;
            }

            var v = (sender as Slider).Value;
            var value = (int)Math.Round(v);
            IsoSlider.Value = value;

            if (value < cameraManager.Status.ISOSpeedRate.Candidates.Count)
            {
                cameraManager.SetIsoSpeedRate(cameraManager.Status.ISOSpeedRate.Candidates[value]);
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
                DebugUtil.Log("Failed to set program shift: " + ex.code);
            }
        }

        private void OpenSliderPanel()
        {
            if (cpm.IsShowing())
            {
                CloseControlPanel();
            }
            Sliders.Visibility = Visibility.Visible;
            // make shoot button and zoom bar/buttons invisible.
            ApplicationSettings.GetInstance().ShootButtonTemporaryCollapsed = true;
            if (svd != null) { svd.ZoomElementsTemporaryCollapsed = true; }
            StartOpenSliderAnimation(0, 180);
        }

        private void CloseSliderPanel()
        {
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

        private void ScreenImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var rh = (sender as Image).RenderSize.Height;
            var rw = (sender as Image).RenderSize.Width;
            // DebugUtil.Log("render size: " + rw + " x " + rh);
            this.FraimingGrids.Height = rh;
            this.FraimingGrids.Width = rw;
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/AboutPage.xaml", UriKind.Relative));
        }
    }
}
