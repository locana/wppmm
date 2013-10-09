using Microsoft.Phone.Controls;
using System;
using System.Diagnostics;
using Microsoft.Phone.Net.NetworkInformation;
using Microsoft.Phone.Tasks;
using Microsoft.Phone.Shell;
using WPPMM.Resources;


namespace WPPMM
{
    public partial class MainPage : PhoneApplicationPage
    {

        private static CameraManager.CameraManager cameraManager;


        // コンストラクター
        public MainPage()
        {
            InitializeComponent();


            cameraManager = CameraManager.CameraManager.GetInstance();

            // get current network status
            UpdateNetworkStatus();


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
            Debug.WriteLine("SSID: " + this.GetSSIDName());
            if (DeviceNetworkInformation.IsWiFiEnabled)
            {
                NetworkStatus.Text = AppResources.Guide_WiFiNotEnabled;
                SearchButton.IsEnabled = true;
            }
            else if (this.GetSSIDName().StartsWith("DIRECT-"))
            {
                NetworkStatus.Text = AppResources.Guide_CantFindDevice;
            }
            else
            {
                NetworkStatus.Text = AppResources.Guide_WiFiNotConnected;
            }

            if (cameraManager != null)
            {
                if (CameraManager.CameraManager.GetDeviceInfo() != null)
                {
                    String modelName = CameraManager.CameraManager.GetDeviceInfo().FriendlyName;
                    if (modelName != null)
                    {
                        NetworkStatus.Text = "Connected device: " + modelName;
                        StartRemoteButton.IsEnabled = true;
                    }
                }
            }

            // display initialize

            ProgressBar.Visibility = System.Windows.Visibility.Collapsed;
            cameraManager.RegisterUpdateListener(WifiUpdateListener);
        }

        private void OnWiFiSettingButtonClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            ConnectionSettingsTask connectionSettingsTask = new ConnectionSettingsTask();
            connectionSettingsTask.ConnectionSettingsType = ConnectionSettingsType.WiFi;
            connectionSettingsTask.Show();
        }

        private void SearchButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            cameraManager.InitializeConnection();
            ProgressBar.IsIndeterminate = true;

        }

        private void Button_Click_1(object sender, System.Windows.RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/LiveViewScreen.xaml", UriKind.Relative));
        }

        internal void WifiUpdateListener(WPPMM.CameraManager.Status cameraStatus)
        {
                        
            if (cameraStatus.isAvailableConnecting)
            {
                String modelName = CameraManager.CameraManager.GetDeviceInfo().FriendlyName;
                NetworkStatus.Text = "Connected device: " + modelName;
            }

            if (cameraStatus.isAvailableConnecting && cameraStatus.MethodTypes != null)
            {
                StartRemoteButton.IsEnabled = true;
            }

        }




        // update disp
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (PhoneApplicationService.Current.StartupMode == StartupMode.Activate)
            {

            }

            UpdateNetworkStatus();
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


    }
}