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
using WPPMMComp;
using System.Text;


namespace WPPMM.Pages
{
    public partial class LiveViewScreen : PhoneApplicationPage
    {

        private CameraManager.CameraManager cameraManager = null;
        private bool isRequestingLiveview = false;
        private BitmapImage screenBitmapImage;
        private MemoryStream screenMemoryStream;

        private byte[] screenData;
        private Stopwatch watch;

        public LiveViewScreen()
        {
            InitializeComponent();

            cameraManager = CameraManager.CameraManager.GetInstance();
            cameraManager.RegisterUpdateListener(UpdateListener);
            cameraManager.StartLiveView();
            cameraManager.SetLiveViewUpdateListener(LiveViewUpdateListener);

            isRequestingLiveview = true;

            screenBitmapImage = new BitmapImage();
            screenBitmapImage.CreateOptions = BitmapCreateOptions.None;


            screenData = new byte[1];

            watch = new Stopwatch();
            watch.Start();

        }

        public void UpdateListener()
        {
            if (isRequestingLiveview && CameraManager.CameraManager.GetLiveviewUrl() != null)
            {
                // starting liveview
                cameraManager.ConnectLiveView();
            }
                
        }


        public void LiveViewUpdateListener(byte[] data)
        {
     
            Debug.WriteLine("[" + watch.ElapsedMilliseconds + "ms" + "][LiveViewScreen] from last calling. ");

            int size = data.Length;
            ScreenImage.Source = null;
            
            screenMemoryStream = new MemoryStream(data, 0, size);
            screenBitmapImage.SetSource(screenMemoryStream);
            ScreenImage.Source = screenBitmapImage;
            screenMemoryStream.Close();

        }


    }
}