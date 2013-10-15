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
using System.ComponentModel;


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

        private double screenWidth;
        private double screenHeight;

        private bool InProgress;
        private bool OnZooming;

        public LiveViewScreen()
        {
            InitializeComponent();

            cameraManager = CameraManager.CameraManager.GetInstance();

            cameraManager.UpdateEvent += UpdateListener;
            

            cameraManager.StartLiveView();
            cameraManager.SetLiveViewUpdateListener(LiveViewUpdateListener);

            Init();

        }

        private void Init()
        {
            isRequestingLiveview = true;

            screenBitmapImage = new BitmapImage();
            screenBitmapImage.CreateOptions = BitmapCreateOptions.None;


            screenData = new byte[1];

            watch = new Stopwatch();
            watch.Start();

            ShootButton.IsEnabled = false;
            InProgress = true;

            screenWidth = ScreenImage.ActualWidth;
            screenHeight = LayoutRoot.ActualHeight;

            OnZooming = false;
        }

        internal void UpdateListener(WPPMM.CameraManager.Status cameraStatus)
        {
            if (isRequestingLiveview && 
                cameraStatus.isConnected　&&
                !cameraStatus.isAvailableShooting)
            {
                // starting liveview
                cameraManager.ConnectLiveView();
            }

            if (cameraStatus.isTakingPicture)
            {
                SetInProgress(true);
            }
            else if (InProgress && !cameraStatus.isTakingPicture)
            {
                SetInProgress(false);
            }

            if (cameraStatus.isAvailableShooting)
            {
                ShootButton.IsEnabled = true;
            }


                
        }


        public void LiveViewUpdateListener(byte[] data)
        {
     
            // Debug.WriteLine("[" + watch.ElapsedMilliseconds + "ms" + "][LiveViewScreen] from last calling. ");

            int size = data.Length;
            ScreenImage.Source = null;
            
            screenMemoryStream = new MemoryStream(data, 0, size);
            screenBitmapImage.SetSource(screenMemoryStream);
            ScreenImage.Source = screenBitmapImage;
            screenMemoryStream.Close();

        }

        private void takeImageButton_Click(object sender, RoutedEventArgs e)
        {
            ShootButton.IsEnabled = false;
            cameraManager.RequestActTakePicture();
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            cameraManager.UpdateEvent -= UpdateListener;
            Init();
            cameraManager.RequestCloseLiveView();
        }

        private void SetInProgress(bool progress)
        {
            InProgress = progress;

            if (InProgress)
            {
                ShootingProgressBar.Visibility = System.Windows.Visibility.Visible;
                ProgressScreen.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                ShootingProgressBar.Visibility = System.Windows.Visibility.Collapsed;
                ProgressScreen.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void OnZoomInClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Stop Zoom In (if started)");
            if (OnZooming)
            {
                cameraManager.RequestActZoom(RemoteApi.ApiParams.ZoomDirIn, RemoteApi.ApiParams.ZoomActStop);
            }

        }

        private void OnZoomOutClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Stop zoom out (if started)");
            if (OnZooming)
            {
                cameraManager.RequestActZoom(RemoteApi.ApiParams.ZoomDirOut, RemoteApi.ApiParams.ZoomActStop);
            }
        }

        private void OnZoomInHold(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Debug.WriteLine("Zoom In: Start");
            cameraManager.RequestActZoom(RemoteApi.ApiParams.ZoomDirIn, RemoteApi.ApiParams.ZoomActStart);
            OnZooming = true;
        }

        private void OnZoomOutHold(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Debug.WriteLine("Zoom Out: Start");
            cameraManager.RequestActZoom(RemoteApi.ApiParams.ZoomDirOut, RemoteApi.ApiParams.ZoomActStart);
            OnZooming = true;
        }

        private void OnZoomInTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Debug.WriteLine("Zoom In: OneShot");
            cameraManager.RequestActZoom(RemoteApi.ApiParams.ZoomDirIn, RemoteApi.ApiParams.ZoomAct1Shot);
        }

        private void OnZoomOutTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Debug.WriteLine("Zoom In: OneShot");
            cameraManager.RequestActZoom(RemoteApi.ApiParams.ZoomDirOut, RemoteApi.ApiParams.ZoomAct1Shot);
        }
    }
}