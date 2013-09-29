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
        private WriteableBitmap screenWritableBitmap;

        private byte[] screenData;
        private int screenDataLen;

        private Direct3DInterop m_d3dInterop = null;
        private Stopwatch watch;

        private System.Text.StringBuilder stringBuilder;


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
            screenDataLen = screenData.Length;

            screenWritableBitmap = new WriteableBitmap(640, 480);

            screenMemoryStream = new MemoryStream();
            watch = new Stopwatch();
            watch.Start();
            stringBuilder = new System.Text.StringBuilder();

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
            // Debug.WriteLine("debug value: " + m_d3dInterop.GetDebugValue());
            stringBuilder.Clear();
            stringBuilder.Append("data: ");
            for (int i = 1000; i < 1050; i++)
            {
                stringBuilder.Append(" ");
                stringBuilder.Append(data[i].ToString());
            }
            Debug.WriteLine(stringBuilder.ToString());

            ScreenImage.Source = null;
            
            screenMemoryStream = new MemoryStream(data, 0, data.Length);

            
            screenBitmapImage.SetSource(screenMemoryStream);
            
           //  WriteableBitmap bmp = new WriteableBitmap(screenBitmapImage);
            // screenWritableBitmap.SetSource(screenMemoryStream);

            Debug.WriteLine("[" + watch.ElapsedMilliseconds + "ms" + "][LiveViewScreen] set source to WritableBitmap. " + size + "bytes. ");
            

            // m_d3dInterop.setTexture(out screenWritableBitmap.Pixels[0], screenWritableBitmap.PixelWidth, screenWritableBitmap.PixelHeight);
            
            ScreenImage.Source = screenBitmapImage;
            


            screenMemoryStream.Close();

        }


    }
}