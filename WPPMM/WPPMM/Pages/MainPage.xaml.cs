using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using WPPMM.Json;
using WPPMM.Ssdp;

namespace WPPMM
{
    public partial class MainPage : PhoneApplicationPage
    {
        // コンストラクター
        public MainPage()
        {
            InitializeComponent();

            // ApplicationBar をローカライズするためのサンプル コード
            //BuildLocalizedApplicationBar();

            string req = Request.actTakePicture();
            Debug.WriteLine(req);

            req = Request.actZoom("in", "1shot");
            Debug.WriteLine(req);

            req = Request.setSelfTimer(1);
            Debug.WriteLine(req);

            string json = "{\"result\": [[\"http://ip:port/postview/postview.jpg\"]],\"id\": 1}";

            ResultHandler.ActTakePicture(json, HandleError, HandleActTakePictureResult);

            DeviceDiscovery.SearchScalarDevices(10, HandleDDLocation, HandleSsdpError);
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

        private void HandleDDLocation(string dd_url)
        {
            Debug.WriteLine("handle dd location: " + dd_url);
            DeviceDiscovery.RetrieveEndpoints(dd_url, HandleEndpoints, HandleSsdpError);
        }

        private void HandleSsdpError()
        {
            Debug.WriteLine("handle ssdp error");
        }

        private void HandleEndpoints(Dictionary<string, string> endpoints)
        {
            Debug.WriteLine("handle endpoints");
            foreach (var service in endpoints.Keys)
            {
                Debug.WriteLine("Endpoint of " + service + ": " + endpoints[service]);
            }
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