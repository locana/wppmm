using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using WPPMM.Json;
using Microsoft.Phone.Net.NetworkInformation;
using Microsoft.Phone.Tasks;
using WPPMM.Ssdp;
using WPPMM.CameraManager;


namespace WPPMM
{
    public partial class MainPage : PhoneApplicationPage
    {

        private CameraManager.CameraManager cameraManager;

        // コンストラクター
        public MainPage()
        {
            InitializeComponent();

            // ApplicationBar をローカライズするためのサンプル コード
            //BuildLocalizedApplicationBar();

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
          
            /*
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("Network available:  ");
            sb.AppendLine(DeviceNetworkInformation.IsNetworkAvailable.ToString());
            sb.Append("Cellular enabled:  ");
            sb.AppendLine(DeviceNetworkInformation.IsCellularDataEnabled.ToString());
            sb.Append("Roaming enabled:  ");
            sb.AppendLine(DeviceNetworkInformation.IsCellularDataRoamingEnabled.ToString());
            sb.Append("Wi-Fi enabled:  ");
            sb.AppendLine(DeviceNetworkInformation.IsWiFiEnabled.ToString());
            NetworkStatus.Text = sb.ToString();
             */

            if (DeviceNetworkInformation.IsWiFiEnabled)
            {
                NetworkStatus.Text = "";
            }
            else
            {
                NetworkStatus.Text = "Currently, Wi-Fi is turned off.\nOpen Wi-Fi setting and connect to your devide.";
            }

            // display initialize
            cameraManager.SetWiFiStatusListener(changeMessageState);
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

        public void changeMessageState(String message)
        {
            NetworkStatus.Text = message;
        }


    }
}