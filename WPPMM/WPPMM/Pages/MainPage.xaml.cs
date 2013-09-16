using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using WPPMM.Json;
using Microsoft.Phone.Net.NetworkInformation;
using Microsoft.Phone.Tasks;
using WPPMM.Ssdp;
using WPPMM.CameraManager;
using Microsoft.Phone.Shell;


namespace WPPMM
{
    public partial class MainPage : PhoneApplicationPage
    {

        private CameraManager.CameraManager cameraManager;

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
          

            if (DeviceNetworkInformation.IsWiFiEnabled)
            {
                NetworkStatus.Text = "";
                SearchButton.IsEnabled = true;
            }
            else
            {
                NetworkStatus.Text = "Currently, Wi-Fi is turned off.\nOpen Wi-Fi setting and connect to your devide.";
            }

            // display initialize
            cameraManager.RegisterUpdateListener(WifiUpdateListener);
            ProgressBar.Visibility = System.Windows.Visibility.Collapsed;

        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
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

        public void WifiUpdateListener()
        {
            String ddLocation = CameraManager.CameraManager.GetDDlocation();
            if (ddLocation != null)
            {
                NetworkStatus.Text = "DD location: " + CameraManager.CameraManager.GetDDlocation();
                StartRemoteButton.IsEnabled = true;
            }
            
        }




        // update disp
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

        }


    }
}