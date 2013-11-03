using Microsoft.Phone.Controls;
using Microsoft.Phone.Net.NetworkInformation;
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

        private const double APPBAR_OPACITY = 1.0;

        private AppBarManager abm = new AppBarManager();

        public MainPage()
        {
            InitializeComponent();

            MyPivot.SelectionChanged += MyPivot_SelectionChanged;

            abm.SetEvent(Menu.ImageSize, PostViewMenuItem_Click);
            abm.SetEvent(IconMenu.About, (sender, e) => { NavigationService.Navigate(new Uri("/Pages/AboutPage.xaml", UriKind.Relative)); });
            abm.SetEvent(IconMenu.WiFi, (sender, e) => { var task = new ConnectionSettingsTask { ConnectionSettingsType = ConnectionSettingsType.WiFi }; task.Show(); });
            abm.SetEvent(IconMenu.SwitchShootMode, SwitchShootMode_Clicked);
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
            if (DeviceNetworkInformation.IsWiFiEnabled)
            {
                NetworkStatus.Text = AppResources.Guide_WiFiNotEnabled;
            }
            else if (ssid.StartsWith("DIRECT-"))
            {
                NetworkStatus.Text = AppResources.Guide_CantFindDevice;
            }
            else
            {
                NetworkStatus.Text = AppResources.Guide_WiFiNotConnected;
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

        private void HideOptionSelector()
        {
            if (OptionSelector.Visibility == System.Windows.Visibility.Visible)
            {
                OptionSelector.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        internal void WifiUpdateListener(Status cameraStatus)
        {

            Debug.WriteLine("WifiUpdateLIstener called");

            if (cameraStatus.isAvailableConnecting)
            {
                String modelName = cameraManager.GetDeviceInfo().FriendlyName;
                NetworkStatus.Text = "Connected device: " + modelName;
                GuideMessage.Visibility = System.Windows.Visibility.Visible;
            }

            if (cameraStatus.isAvailableConnecting && cameraStatus.MethodTypes != null)
            {

            }
        }

        internal void LiveViewUpdateListener(Status cameraStatus)
        {
            if (isRequestingLiveview &&
                cameraStatus.isConnected &&
                !cameraStatus.IsAvailableShooting)
            {
                // starting liveview
                bool started = cameraManager.ConnectLiveView();
                if (!started)
                {
                    GoToMainPage();
                    return;
                }
            }

            if (cameraStatus.ZoomInfo != null)
            {
                // dumpZoomInfo(cameraStatus.ZoomInfo);
                double margin_left = cameraStatus.ZoomInfo.position_in_current_box * 156 / 100;
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

        private void PostViewMenuItem_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("PostViewMenuItem clicked");

            if (cameraManager.cameraStatus.AvailablePostViewSize.Count != 0)
            {
                OptionSelector.ItemsSource = cameraManager.cameraStatus.AvailablePostViewSize;
                OptionSelector.Visibility = System.Windows.Visibility.Visible;
                OptionSelector.SelectionChanged += OptionSelector_SelectionChanged;
            }
        }

        private void OptionSelector_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            String size = ((LongListSelector)sender).SelectedItem.ToString();
            Debug.WriteLine("selected option: " + size);
            cameraManager.SetPostViewImageSize(size);
            OptionSelector.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void LiveViewInit()
        {
            isRequestingLiveview = true;

            cameraManager.RequestCloseLiveView();

            OnZooming = false;
        }

        private void takeImageButton_Click(object sender, RoutedEventArgs e)
        {
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
                    }
                    break;
                case ApiParams.EventMvRecording:
                    cameraManager.StopMovieRec();
                    break;
            }
        }

        private void SwitchShootMode_Clicked(object sender, EventArgs e)
        {
            switch (cameraManager.cameraStatus.ShootModeInfo.current)
            {
                case ApiParams.ShootModeStill:
                    cameraManager.SetShootMode(ApiParams.ShootModeMovie);
                    break;
                case ApiParams.ShootModeMovie:
                    cameraManager.SetShootMode(ApiParams.ShootModeStill);
                    break;
            }
        }

        private readonly PostViewData pvd = new PostViewData();

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
            ApplicationBar = abm.Disable(IconMenu.WiFi).Disable(IconMenu.About).CreateNew(APPBAR_OPACITY);
            cameraManager.UpdateEvent += LiveViewUpdateListener;
            if (cameraManager.IsClientReady())
            {
                cameraManager.StartLiveView();
                cameraManager.RunEventObserver();
            }
            else
            {
                Debug.WriteLine("Await for async device discovery");
                var found = await PrepareConnectionAsync();
                Debug.WriteLine("Async device discovery result: " + found);
                if (found)
                {
                    cameraManager.StartLiveView();
                    cameraManager.RunEventObserver();
                }
            }

            if (PreviousSelectedPivotIndex == PIVOTINDEX_LIVEVIEW)
            {
                if (cameraManager.cameraStatus.MethodTypes.Contains("setPostviewImageSize"))
                    abm.Enable(Menu.ImageSize);
                if (cameraManager.cameraStatus.MethodTypes.Contains("setShootMode"))
                    abm.Enable(IconMenu.SwitchShootMode);
                if (cameraManager.cameraStatus.MethodTypes.Contains("setSelfTimer"))
                    abm.Enable(IconMenu.SelfTimer);

                Dispatcher.BeginInvoke(() => { ApplicationBar = abm.CreateNew(APPBAR_OPACITY); });
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
            ApplicationBar = abm.Enable(IconMenu.About).Enable(IconMenu.WiFi)//
                .Disable(Menu.ImageSize).Disable(IconMenu.SwitchShootMode).Disable(IconMenu.SelfTimer)//
                .CreateNew(0.0);
            HideOptionSelector();
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
        }

        private void PhoneApplicationPage_Unloaded(object sender, RoutedEventArgs e)
        {
            ShootButton.DataContext = null;
            ShootingProgress.DataContext = null;
            ZoomElements.DataContext = null;
        }

        private void PhoneApplicationPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            Debug.WriteLine("OrientationChagned: " + e.Orientation);
            switch (e.Orientation)
            {
                case PageOrientation.LandscapeLeft:
                    AppTitle.Margin = new Thickness(30, 0, 0, 0);
                    break;
                case PageOrientation.LandscapeRight:
                    AppTitle.Margin = new Thickness(60, 0, 0, 0);
                    break;
                case PageOrientation.PortraitUp:
                    AppTitle.Margin = new Thickness(0, 0, 0, 0);
                    break;
            }
        }
    }
}
