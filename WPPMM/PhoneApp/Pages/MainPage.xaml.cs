using Microsoft.Phone.Controls;
using Microsoft.Phone.Net.NetworkInformation;
using Microsoft.Phone.Reactive;
using Microsoft.Phone.Tasks;
using Microsoft.Xna.Framework.Media;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using WPPMM.CameraManager;
using WPPMM.DataModel;
using WPPMM.RemoteApi;
using WPPMM.Resources;
using WPPMM.Utils;

namespace WPPMM
{
    public partial class MainPage : PhoneApplicationPage
    {
        private const int PIVOTINDEX_MAIN = 0;
        private const int PIVOTINDEX_LIVEVIEW = 1;

        private CameraManager.CameraManager cameraManager = CameraManager.CameraManager.GetInstance();

        private bool isRequestingLiveview = false;

        private bool OnZooming;

        private const double APPBAR_OPACITY = 0.0;

        private readonly PostViewData pvd = new PostViewData();
        private AppBarManager abm = new AppBarManager();
        private ControlPanelManager cpm;

        public MainPage()
        {
            InitializeComponent();

            MyPivot.SelectionChanged += MyPivot_SelectionChanged;

            abm.SetEvent(IconMenu.About, (sender, e) => { NavigationService.Navigate(new Uri("/Pages/AboutPage.xaml", UriKind.Relative)); });
            abm.SetEvent(IconMenu.WiFi, (sender, e) => { var task = new ConnectionSettingsTask { ConnectionSettingsType = ConnectionSettingsType.WiFi }; task.Show(); });
            abm.SetEvent(IconMenu.ControlPanel, (sender, e) => { ApplicationBar.IsVisible = false; cpm.Show(); });
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Debug.WriteLine(e.Uri);
            progress.IsVisible = false;
            cameraManager.Refresh();
            UpdateNetworkStatus();
            LiveViewInit();
            if (GetSSIDName().StartsWith("DIRECT-"))
            {
                StartConnectionSequence(NavigationMode.New == e.NavigationMode || MyPivot.SelectedIndex == 1);
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            cameraManager.RequestCloseLiveView();
            //LiveViewInit();
        }

        private void StartConnectionSequence(bool connect)
        {
            progress.IsVisible = connect;
            CameraManager.CameraManager.GetInstance().RequestSearchDevices(() =>
            {
                progress.IsVisible = false;
                if (connect) GoToShootingPage();
            }, () =>
            {
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
            if (ssid != null && ssid.StartsWith("DIRECT-"))
            {
                NetworkStatus.Text = AppResources.Guide_CantFindDevice;
            }
            else
            {
                NetworkStatus.Text = AppResources.Guide_WiFiNotEnabled;
            }

            if (cameraManager.GetDeviceInfo() != null)
            {
                String modelName = cameraManager.GetDeviceInfo().FriendlyName;
                if (modelName != null)
                {
                    NetworkStatus.Text = "Connected device: " + modelName;
                    GuideMessage.Visibility = System.Windows.Visibility.Visible;
                }
            }

            // display initialize

            ProgressBar.Visibility = System.Windows.Visibility.Collapsed;
            cameraManager.UpdateEvent += WifiUpdateListener;
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
            MyPivot.SelectedIndex = 0;
        }

        internal void WifiUpdateListener(Status cameraStatus)
        {

            Debug.WriteLine("WifiUpdateLIstener called");

            if (cameraStatus.isAvailableConnecting)
            {
                String modelName = "";
                if (cameraManager.GetDeviceInfo().FriendlyName != null)
                {
                    modelName = cameraManager.GetDeviceInfo().FriendlyName;
                }
                NetworkStatus.Text = "Connected device: " + modelName;
                GuideMessage.Visibility = System.Windows.Visibility.Visible;
            }

            if (cameraStatus.isAvailableConnecting && cameraStatus.MethodTypes != null)
            {

            }
        }

        internal void LiveViewUpdateListener(Status cameraStatus)
        {
            if (cameraStatus.ZoomInfo != null)
            {
                // dumpZoomInfo(cameraStatus.ZoomInfo);
                double margin_left = cameraStatus.ZoomInfo.position * 156 / 100;
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
            isRequestingLiveview = true;

            cameraManager.RequestCloseLiveView();

            OnZooming = false;
        }

        private void takeImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (ApplicationSettings.GetInstance().IsIntervalShootingEnabled)
            {
                cameraManager.ToggleIntevalRec();
                return;
            }

            var status = cameraManager.cameraStatus;
            switch (status.CameraStatus)
            {
                case ApiParams.EventIdle:
                    switch (status.ShootModeInfo.current)
                    {
                        case ApiParams.ShootModeStill:
                            cameraManager.RequestActTakePicture();
                            break;
                        case ApiParams.ShootModeMovie:
                            cameraManager.StartMovieRec();
                            break;
                        case ApiParams.ShootModeAudio:
                            cameraManager.StartAudioRec();
                            break;
                    }
                    break;
                case ApiParams.EventMvRecording:
                    cameraManager.StopMovieRec();
                    break;
                case ApiParams.EventAuRecording:
                    cameraManager.StopAudioRec();
                    break;
            }
        }

        private void PostViewWindow_Loaded(object sender, RoutedEventArgs e)
        {
            cameraManager.PictureNotifier = OnPictureSaved;
            PostViewWindow.DataContext = pvd;
        }

        private void PostViewWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            PostViewWindow.DataContext = null;
            cameraManager.PictureNotifier = null;
        }

        private void OnPictureSaved(Picture pic)
        {
            pvd.PictureData = pic;
        }

        private void PostViewWindow_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (pvd.postview != null)
            {
                NavigationService.Navigate(new Uri("/Pages/ViewerPage.xaml", UriKind.Relative));
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
                    break;
                case 1:
                    LiveviewPageLoaded();
                    break;
            }
        }

        private async void LiveviewPageLoaded()
        {
            ApplicationBar = abm.Clear().CreateNew(APPBAR_OPACITY);
            //ApplicationBar = abm.Clear().Enable(IconMenu.ControlPanel).CreateNew(APPBAR_OPACITY);
            SetLayoutByOrientation(this.Orientation);

            cameraManager.UpdateEvent += LiveViewUpdateListener;
            cameraManager.ShowToast += ShowToast;
            ToastApparance.Completed += ToastApparance_Completed;
            if (cameraManager.IsClientReady())
            {
                cameraManager.OperateInitialProcess();
                cameraManager.RunEventObserver();
            }
            else if (!GetSSIDName().StartsWith("DIRECT-"))
            {
                Dispatcher.BeginInvoke(() => { GoToMainPage(); });
                return;
            }
            else
            {
                Debug.WriteLine("Await for async device discovery");
                cameraManager.cameraStatus.IsSearchingDevice = true;
                var found = await PrepareConnectionAsync();
                Dispatcher.BeginInvoke(() => { cameraManager.cameraStatus.IsSearchingDevice = false; });

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

            if (PreviousSelectedPivotIndex == PIVOTINDEX_LIVEVIEW)
            {
                var status = cameraManager.cameraStatus;
                if (cpm.ItemCount > 0)
                {
                    abm.Enable(IconMenu.ControlPanel);
                }

                Dispatcher.BeginInvoke(() => { if (cpm != null) cpm.Hide(); ApplicationBar = abm.CreateNew(APPBAR_OPACITY); });
            }
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
            cameraManager.StopEventObserver();
            cameraManager.SetLiveViewUpdateListener(null);
            cameraManager.UpdateEvent -= LiveViewUpdateListener;
            cameraManager.ShowToast -= ShowToast;
            ToastApparance.Completed -= ToastApparance_Completed;
            ApplicationBar = abm.Clear().Enable(IconMenu.About).Enable(IconMenu.WiFi).CreateNew(0.0);
            if (cpm != null) { cpm.Hide(); }
        }

        private void OnZoomInClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Stop Zoom In (if started)");
            if (OnZooming)
            {
                cameraManager.RequestActZoom(ApiParams.ZoomDirIn, ApiParams.ZoomActStop);
            }
        }

        private void OnZoomOutClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Stop zoom out (if started)");
            if (OnZooming)
            {
                cameraManager.RequestActZoom(ApiParams.ZoomDirOut, ApiParams.ZoomActStop);
            }
        }

        private void OnZoomInHold(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Debug.WriteLine("Zoom In: Start");
            cameraManager.RequestActZoom(ApiParams.ZoomDirIn, ApiParams.ZoomActStart);
            OnZooming = true;
        }

        private void OnZoomOutHold(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Debug.WriteLine("Zoom Out: Start");
            cameraManager.RequestActZoom(ApiParams.ZoomDirOut, ApiParams.ZoomActStart);
            OnZooming = true;
        }

        private void OnZoomInTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Debug.WriteLine("Zoom In: OneShot");
            cameraManager.RequestActZoom(ApiParams.ZoomDirIn, ApiParams.ZoomAct1Shot);
        }

        private void OnZoomOutTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Debug.WriteLine("Zoom In: OneShot");
            cameraManager.RequestActZoom(ApiParams.ZoomDirOut, ApiParams.ZoomAct1Shot);
        }

        private void ScreenImage_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("ScreenImage_Loaded");
            LiveviewPageUnloaded();
            ScreenImage.DataContext = cameraManager.LiveviewImage;
        }

        private void ScreenImage_Unloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("ScreenImage_UnLoaded");
            LiveviewPageUnloaded();
            ScreenImage.DataContext = null;
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            Debug.WriteLine("onbackkey");
            if (MyPivot.SelectedIndex == PIVOTINDEX_LIVEVIEW)
            {
                if (cpm.IsShowing())
                {
                    cpm.Hide();
                    ApplicationBar.IsVisible = true;
                }
                else
                    GoToMainPage();
                e.Cancel = true;
            }
            else
            {
                e.Cancel = false;
            }
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            ShootButton.DataContext = cameraManager.cameraStatus;
            ShootingProgress.DataContext = cameraManager.cameraStatus;
            ZoomElements.DataContext = cameraManager.cameraStatus;
            Toast.DataContext = cameraManager.cameraStatus;
            cpm = new ControlPanelManager(ControlPanel);
        }

        private void PhoneApplicationPage_Unloaded(object sender, RoutedEventArgs e)
        {
            ShootButton.DataContext = null;
            ShootingProgress.DataContext = null;
            ZoomElements.DataContext = null;
            cpm = null;
        }

        private void PhoneApplicationPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            Debug.WriteLine("OrientationChagned: " + e.Orientation);
            SetLayoutByOrientation(e.Orientation);
        }

        private void SetLayoutByOrientation(PageOrientation orientation)
        {
            switch (orientation)
            {
                case PageOrientation.LandscapeLeft:
                case PageOrientation.LandscapeRight:
                    AppTitle.Margin = new Thickness(60, 0, 0, 0);
                    ShootButton.Margin = new Thickness(0, 0, 70, 30);
                    ZoomElements.Margin = new Thickness(70, 0, 0, 30);
                    PostViewWindow.Margin = new Thickness(40, 20, 0, 0);
                    break;
                case PageOrientation.PortraitUp:
                    AppTitle.Margin = new Thickness(0, 0, 0, 0);
                    ShootButton.Margin = new Thickness(0, 0, 30, 80);
                    ZoomElements.Margin = new Thickness(30, 0, 0, 80);
                    PostViewWindow.Margin = new Thickness(20, 20, 0, 0);
                    break;
            }
        }

        public void ShowToast(String message)
        {
            ToastMessage.Text = message;
            ToastApparance.Begin();
        }

        void ToastApparance_Completed(object sender, EventArgs e)
        {
            Scheduler.Dispatcher.Schedule(CloseToast, TimeSpan.FromSeconds(3));
        }

        void CloseToast()
        {
            ToastDisApparance.Begin();
        }
    }
}
