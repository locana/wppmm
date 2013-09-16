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

        private bool isWiFiConnected;

        private CameraManager.CameraManager cameraManager;

        // コンストラクター
        public MainPage()
        {
            InitializeComponent();

            // ApplicationBar をローカライズするためのサンプル コード
            //BuildLocalizedApplicationBar();

            string json = "{\"result\": [[\"http://ip:port/postview/postview.jpg\"]],\"id\": 1}";

            ResultHandler.ActTakePicture(json, new Action<int>(HandleError), new Action<string[]>(HandleActTakePictureResult));

            // get current network status
            UpdateNetworkStatus();

            cameraManager = CameraManager.CameraManager.GetInstance();
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
            isWiFiConnected = false;

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

            isWiFiConnected = DeviceNetworkInformation.IsWiFiEnabled;

        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ConnectionSettingsTask connectionSettingsTask = new ConnectionSettingsTask();
            connectionSettingsTask.ConnectionSettingsType = ConnectionSettingsType.WiFi;
            connectionSettingsTask.Show();
        }

        private void SearchButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            cameraManager.RequestSearchDevices();
        }

        // ローカライズされた ApplicationBar を作成するためのサンプル コード
        //private void BuildLocalizedApplicationBar()
        //{
        //    // ページの ApplicationBar を ApplicationBar の新しいインスタンスに設定します。
        //    ApplicationBar = new ApplicationBar();

        //    // 新しいボタンを作成し、テキスト値を AppResources のローカライズされた文字列に設定します。
        //    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
        //    appBarButton.Text = AppResources.AppBarButtonText;
        //    ApplicationBar.Buttons.Add(appBarButton);

        //    // AppResources のローカライズされた文字列で、新しいメニュー項目を作成します。
        //    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
        //    ApplicationBar.MenuItems.Add(appBarMenuItem);
        //}
    }
}