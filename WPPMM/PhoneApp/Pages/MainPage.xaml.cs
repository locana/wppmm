using Microsoft.Phone.Controls;
using Microsoft.Phone.Net.NetworkInformation;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Microsoft.Xna.Framework.Media;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using WPPMM.CameraManager;
using WPPMM.DataModel;
using WPPMM.RemoteApi;
using WPPMM.Resources;


namespace WPPMM
{
    public partial class MainPage : PhoneApplicationPage
    {
        private const int PIVOTINDEX_MAIN = 0;
        private const int PIVOTINDEX_LIVEVIEW = 1;

        private static CameraManager.CameraManager cameraManager;

        private bool isRequestingLiveview = false;
        private BitmapImage screenBitmapImage;
        private MemoryStream screenMemoryStream;

        private byte[] screenData;
        private Stopwatch watch;

        private double screenWidth;
        private double screenHeight;

        private bool InProgress;
        private bool OnZooming;

        // コンストラクター
        public MainPage()
        {
            InitializeComponent();

            BuildLocalizedApplicationBar();

            cameraManager = CameraManager.CameraManager.GetInstance();

            MyPivot.SelectionChanged += MyPivot_SelectionChanged;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Debug.WriteLine(e.Uri);
            progress.IsVisible = false;
            cameraManager.Refresh();
            UpdateNetworkStatus();
            IsReadyToControl = false;
            LiveviewPageUnloaded();
            LiveViewInit();
            if (GetSSIDName().StartsWith("DIRECT-"))
            {
                StartConnectionSequence(NavigationMode.New == e.NavigationMode || MyPivot.SelectedIndex == 1);
            }
        }

        private bool IsReadyToControl = false;

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            LiveviewPageUnloaded();
            LiveViewInit();
        }

        private void StartConnectionSequence(bool connect)
        {
            progress.IsVisible = connect;
            CameraManager.CameraManager.GetInstance().RequestSearchDevices(() =>
            {
                progress.IsVisible = false;
                IsReadyToControl = true;
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

        private void HandleActTakePictureResult(string[] urls)
        {
            Debug.WriteLine("HandleActTakePictureResult");
            foreach (var url in urls)
            {
                Debug.WriteLine("URL: " + url);
            }
        }

        private void UpdateNetworkStatus()
        {
            var ssid = GetSSIDName();
            Debug.WriteLine("SSID: " + ssid);
            if (DeviceNetworkInformation.IsWiFiEnabled)
            {
                NetworkStatus.Text = AppResources.Guide_WiFiNotEnabled;
                StartRemoteButton.IsEnabled = true;
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
                }
            }

            // display initialize

            ProgressBar.Visibility = System.Windows.Visibility.Collapsed;
            cameraManager.UpdateEvent += WifiUpdateListener;
        }

        private void OnWiFiSettingButtonClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            ConnectionSettingsTask connectionSettingsTask = new ConnectionSettingsTask();
            connectionSettingsTask.ConnectionSettingsType = ConnectionSettingsType.WiFi;
            connectionSettingsTask.Show();
        }

        private void Button_Click_1(object sender, System.Windows.RoutedEventArgs e)
        {
            if (IsReadyToControl)
            {
                GoToShootingPage();
            }
            else
            {
                StartConnectionSequence(true);
                ProgressBar.IsIndeterminate = true;
            }
        }

        private void GoToShootingPage()
        {
            if (MyPivot.SelectedIndex == 1)
            {
                LiveviewPageLoaded();
            }
            else
            {
                // NavigationService.Navigate(new Uri("/Pages/LiveViewScreen.xaml", UriKind.Relative));
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
                String modelName = cameraManager.GetDeviceInfo().FriendlyName;
                NetworkStatus.Text = "Connected device: " + modelName;
            }

            if (cameraStatus.isAvailableConnecting && cameraStatus.MethodTypes != null)
            {
                StartRemoteButton.IsEnabled = true;
            }

        }

        internal void LiveViewUpdateListener(Status cameraStatus)
        {
            if (isRequestingLiveview &&
                cameraStatus.isConnected &&
                !cameraStatus.isAvailableShooting)
            {
                // starting liveview
                try
                {
                    cameraManager.ConnectLiveView();
                }
                catch (InvalidOperationException)
                {
                    Debug.WriteLine("Failed starting liveview because of duplicate liveview connection");
                    GoToMainPage();
                    return;
                }
            }

            Debug.WriteLine("LiveViewUpdateListener is called:");
            StringBuilder sb = new StringBuilder("method: ");
            foreach (String s in cameraStatus.MethodTypes)
            {
                sb.Append(" ");
                sb.Append(s);
            }
            Debug.WriteLine(sb.ToString());

            if (cameraStatus.isTakingPicture)
            {
                SetInProgress(true);
            }
            else if (InProgress && !cameraStatus.isTakingPicture)
            {
                SetInProgress(false);
            }

            if (cameraStatus.isAvailableShooting)
            {
                ShootButton.IsEnabled = true;
            }


            // change visibility of items for zoom
            if (cameraStatus.MethodTypes.Contains("actZoom"))
            {
                SetZoomDisp(true);

                if (cameraStatus.ZoomInfo != null)
                {
                    // dumpZoomInfo(cameraStatus.ZoomInfo);
                    double margin_left = cameraStatus.ZoomInfo.position_in_current_box * 156 / 100;
                    ZoomCursor.Margin = new Thickness(15 + margin_left, 2, 0, 0);
                    Debug.WriteLine("zoom bar display update: " + margin_left);
                }
            }
            else
            {
                SetZoomDisp(false);
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

        private void OSS_Menu_Click(object sender, System.EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/AboutPage.xaml", UriKind.Relative));
        }

        private void BuildLocalizedApplicationBar()
        {
            ApplicationBar = new ApplicationBar();
            ApplicationBar.Mode = ApplicationBarMode.Minimized;
            ApplicationBar.Opacity = 0.5;

            var OssMenuItem = new ApplicationBarMenuItem(AppResources.About);
            OssMenuItem.Click += OSS_Menu_Click;
            ApplicationBar.MenuItems.Add(OssMenuItem);
        }

        private void LiveViewInit()
        {
            isRequestingLiveview = true;

            screenBitmapImage = new BitmapImage();
            screenBitmapImage.CreateOptions = BitmapCreateOptions.None;


            screenData = new byte[1];

            watch = new Stopwatch();
            watch.Start();

            ShootButton.IsEnabled = false;
            InProgress = true;

            screenWidth = ScreenImage.ActualWidth;
            screenHeight = LayoutRoot.ActualHeight;

            cameraManager.RequestCloseLiveView();

            OnZooming = false;
        }

        public void EEScreenUpdateListener(byte[] data)
        {

            // Debug.WriteLine("[" + watch.ElapsedMilliseconds + "ms" + "][LiveViewScreen] from last calling. ");

            int size = data.Length;
            ScreenImage.Source = null;

            screenMemoryStream = new MemoryStream(data, 0, size);
            screenBitmapImage.SetSource(screenMemoryStream);
            ScreenImage.Source = screenBitmapImage;
            screenMemoryStream.Close();

        }

        private void takeImageButton_Click(object sender, RoutedEventArgs e)
        {
            ShootButton.IsEnabled = false;
            cameraManager.RequestActTakePicture();
        }

        private void SetInProgress(bool progress)
        {
            InProgress = progress;

            if (InProgress)
            {
                ShootingProgressBar.Visibility = Visibility.Visible;
                ProgressScreen.Visibility = Visibility.Visible;
            }
            else
            {
                ShootingProgressBar.Visibility = Visibility.Collapsed;
                ProgressScreen.Visibility = Visibility.Collapsed;
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
                var task = new PhotoChooserTask();
                task.Show();
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

        private void LiveviewPageLoaded()
        {
            cameraManager.UpdateEvent += LiveViewUpdateListener;
            cameraManager.StartLiveView();
            cameraManager.SetLiveViewUpdateListener(EEScreenUpdateListener);
            cameraManager.RunEventObserver();
        }

        private void LiveviewPageUnloaded()
        {
            cameraManager.StopEventObserver();
            cameraManager.SetLiveViewUpdateListener(null);
            cameraManager.UpdateEvent -= LiveViewUpdateListener;
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

        private void SetZoomDisp(bool disp)
        {
            if (disp)
            {
                if (ZoomElements.Visibility == System.Windows.Visibility.Collapsed)
                {
                    ZoomElements.Visibility = System.Windows.Visibility.Visible;
                }
            }
            else
            {
                if (ZoomElements.Visibility == System.Windows.Visibility.Visible)
                {
                    ZoomElements.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
        }
    }
}
