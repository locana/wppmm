using Microsoft.Phone.Controls;
using System;
using System.Diagnostics;
using WPPMM.Json;

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

            string json = "{\"result\": [[\"http://ip:port/postview/postview.jpg\"]],\"id\": 1}";

            ResultHandler.ActTakePicture(json, new Action<int>(HandleError), new Action<string[]>(HandleActTakePictureResult));
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