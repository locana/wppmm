using Microsoft.Phone.Controls;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using WPPMM.CameraManager;
using WPPMM.RemoteApi;


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

        internal void UpdateListener(Status cameraStatus)
        {
            if (isRequestingLiveview &&
                cameraStatus.isConnected &&
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


            // change visibility of items for zoom
            if (cameraStatus.MethodTypes.Contains("actZoom"))
            {
                SetZoomDisp(true);

                if (cameraStatus.ZoomInfo != null)
                {
                    // dumpZoomInfo(cameraStatus.ZoomInfo);
                    double margin_left = cameraStatus.ZoomInfo.position_in_current_box * 156 / 100;
                    ZoomCursor.Margin = new Thickness(15 + margin_left, 2, 0, 0);
                    Debug.WriteLine("zoom bar display update: " + margin_left);
                }
            }
            else
            {
                SetZoomDisp(false);
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
                ShootingProgressBar.Visibility = Visibility.Visible;
                ProgressScreen.Visibility = Visibility.Visible;
            }
            else
            {
                ShootingProgressBar.Visibility = Visibility.Collapsed;
                ProgressScreen.Visibility = Visibility.Collapsed;
            }
        }

        private void OnZoomInClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Stop Zoom In (if started)");
            if (OnZooming)
            {
                cameraManager.RequestActZoom(ApiParams.ZoomDirIn, ApiParams.ZoomActStop);
            }

        }

        private void OnZoomOutClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Stop zoom out (if started)");
            if (OnZooming)
            {
                cameraManager.RequestActZoom(ApiParams.ZoomDirOut, ApiParams.ZoomActStop);
            }
        }

        private void OnZoomInHold(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Debug.WriteLine("Zoom In: Start");
            cameraManager.RequestActZoom(ApiParams.ZoomDirIn, ApiParams.ZoomActStart);
            OnZooming = true;
        }

        private void OnZoomOutHold(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Debug.WriteLine("Zoom Out: Start");
            cameraManager.RequestActZoom(ApiParams.ZoomDirOut, ApiParams.ZoomActStart);
            OnZooming = true;
        }

        private void OnZoomInTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Debug.WriteLine("Zoom In: OneShot");
            cameraManager.RequestActZoom(ApiParams.ZoomDirIn, ApiParams.ZoomAct1Shot);
        }

        private void OnZoomOutTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Debug.WriteLine("Zoom In: OneShot");
            cameraManager.RequestActZoom(ApiParams.ZoomDirOut, ApiParams.ZoomAct1Shot);
        }

        private void SetZoomDisp(bool disp)
        {
            if (disp)
            {
                if (ZoomElements.Visibility == System.Windows.Visibility.Collapsed)
                {
                    ZoomElements.Visibility = System.Windows.Visibility.Visible;
                }
            }
            else
            {
                if (ZoomElements.Visibility == System.Windows.Visibility.Visible)
                {
                    ZoomElements.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
        }

        private void dumpZoomInfo(ZoomInfo info)
        {
            Debug.WriteLine("boxes: " + info.current_box_index + " / " + info.number_of_boxes);
            Debug.WriteLine("position: " + info.position);
            Debug.WriteLine("position in current box: " + info.position_in_current_box);
            

        }

    }
}