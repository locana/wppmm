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


namespace WPPMM.Pages
{
    public partial class LiveViewScreen : PhoneApplicationPage
    {

        private CameraManager.CameraManager cameraManager = null;
        private bool isRequestingLiveview = false;
        private BitmapImage screenBitmapImage;
        private MemoryStream screenMemoryStream;

        private byte[] screenData;
        private int screenDataLen;

        private Direct3DInterop m_d3dInterop = null;

        public LiveViewScreen()
        {
            InitializeComponent();

            cameraManager = CameraManager.CameraManager.GetInstance();
            cameraManager.RegisterUpdateListener(UpdateListener);
            cameraManager.StartLiveView();
            cameraManager.SetLiveViewUpdateListener(LiveViewUpdateListener);

            isRequestingLiveview = true;

            screenBitmapImage = new BitmapImage();
            screenBitmapImage.CreateOptions = BitmapCreateOptions.DelayCreation;

            screenData = new byte[1];
            screenDataLen = screenData.Length;
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
            bitmap.CreateOptions = BitmapCreateOptions.DelayCreation;
            bitmap.SetSource(ms);
            ScreenImage.Source = bitmap;
        }

        public void LiveViewUpdateListener(byte[] data)
        {
            int size = data.Length;
            Debug.WriteLine("[LiveViewScreen] Jpeg retrived. " + size + "bytes.");

            screenData = data;
            screenDataLen = data.Length;

            return;

            screenMemoryStream = new MemoryStream(data, 0, data.Length);

            screenBitmapImage.SetSource(screenMemoryStream);
            ScreenImage.Source = screenBitmapImage;
            screenMemoryStream.Close();
        }

        private void DrawingSurface_Loaded(object sender, RoutedEventArgs e)
        {
            
            if (m_d3dInterop == null)
            {
                m_d3dInterop = new Direct3DInterop();

                

                // Set window bounds in dips
                m_d3dInterop.WindowBounds = new Windows.Foundation.Size(
                    (float)ScreenSurface.ActualWidth,
                    (float)ScreenSurface.ActualHeight
                    );

                // Set native resolution in pixels
                m_d3dInterop.NativeResolution = new Windows.Foundation.Size(
                    (float)Math.Floor(ScreenSurface.ActualWidth * Application.Current.Host.Content.ScaleFactor / 100.0f + 0.5f),
                    (float)Math.Floor(ScreenSurface.ActualHeight * Application.Current.Host.Content.ScaleFactor / 100.0f + 0.5f)
                    );

                // Set render resolution to the full native resolution
                m_d3dInterop.RenderResolution = m_d3dInterop.NativeResolution;

                // m_d3dInterop.SetTestNum(101);


               
                // m_d3dInterop.SetDataPtr(out screenData[0], out screenDataLen);

                // Hook-up native component to DrawingSurface
                ScreenSurface.SetContentProvider(m_d3dInterop.CreateContentProvider());
                ScreenSurface.SetManipulationHandler(m_d3dInterop);

            }
        }
    }
}