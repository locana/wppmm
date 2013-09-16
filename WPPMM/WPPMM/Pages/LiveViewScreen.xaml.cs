using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using WPPMM.CameraManager;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using Microsoft.Phone;
using Microsoft.Xna.Framework.Media;
using System.Windows.Resources;
using System.Windows.Media.Imaging;

namespace WPPMM.Pages
{
    public partial class LiveViewScreen : PhoneApplicationPage
    {

        private CameraManager.CameraManager cameraManager = null;
        private bool isRequestingLiveview = false;

        public LiveViewScreen()
        {
            InitializeComponent();

            cameraManager = CameraManager.CameraManager.GetInstance();
            cameraManager.RegisterUpdateListener(UpdateListener);
            cameraManager.StartLiveView();
            cameraManager.SetLiveViewUpdateListener(LiveViewUpdateListener);

            isRequestingLiveview = true;
            
        }

        public void UpdateListener()
        {
            if (isRequestingLiveview && CameraManager.CameraManager.GetLiveviewUrl() != null)
            {
                // starting liveview
                cameraManager.ConnectLiveView();
            }
                
        }

        public void LiveViewUpdateListener(MemoryStream ms)
        {
            Debug.WriteLine("Live view update listener");
            BitmapImage bitmap = new BitmapImage();
            bitmap.CreateOptions = BitmapCreateOptions.None;
            bitmap.SetSource(ms);
            ScreenImage.Source = bitmap;
        }
    }
}