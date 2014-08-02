using Kazyx.WPMMM.Resources;
using Kazyx.WPPMM.DataModel;
using Microsoft.Phone.Controls;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace Kazyx.WPPMM.Pages
{
    public partial class ViewerPage : PhoneApplicationPage
    {
        public ViewerPage()
        {
            InitializeComponent();
        }

        private bool IsViewingDetail = false;

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SetVisibility(false);
            var lib = new MediaLibrary();
            PictureAlbum CameraRoll = null;
            foreach (var album in lib.RootPictureAlbum.Albums)
            {
                if (album.Name == "Camera Roll")
                {
                    CameraRoll = album;
                    break;
                }
            }
            if (CameraRoll == null)
            {
                NavigationService.GoBack();
                return;
            }
            LoadThumbnails(CameraRoll);

            CameraManager.CameraManager cm = CameraManager.CameraManager.GetInstance();
            cm.Downloader.QueueStatusUpdated += OnFetchingImages;
            cm.PictureFetched += OnPictureFetched;

            cm.StopEventObserver();
            cm.Refresh();
            await Task.Delay(1000);
            TryRunEventObserver();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            CameraManager.CameraManager cm = CameraManager.CameraManager.GetInstance();
            cm.Downloader.QueueStatusUpdated -= OnFetchingImages;
            cm.PictureFetched -= OnPictureFetched;
            cm.StopEventObserver();
            cm.Refresh();
            base.OnNavigatedFrom(e);
        }

        private void TryRunEventObserver()
        {
            Debug.WriteLine("TryRunEventObserver");
            CameraManager.CameraManager cm = CameraManager.CameraManager.GetInstance();
            if (cm.IsClientReady())
            {
                return;
            }
            cm.RequestSearchDevices(() =>
            {
                Debug.WriteLine("Device discovered");
                cm.RunEventObserver();
            }, () => { TryRunEventObserver(); });
        }

        private void OnPictureFetched(Picture picture)
        {
            Dispatcher.BeginInvoke(() =>
            {
                var groups = ImageGrid.DataContext as ThumbnailGroup;
                if (groups == null)
                {
                    return;
                }
                groups.Group.Insert(0, new ThumbnailData(picture));
            });
        }

        private void OnFetchingImages(int count)
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (count != 0)
                {
                    progress.Text = AppResources.ProgressMessageFetching;
                    progress.IsVisible = true;
                }
                else
                {
                    progress.IsVisible = false;
                }
            });
        }

        private void ReleaseDetail()
        {
            if (DetailImage.Source != null)
            {
                DetailImage.Source = null;
            }
            _bitmap = null;
            SetVisibility(false);
        }

        private void SetVisibility(bool visible)
        {
            if (visible)
            {
                progress.IsVisible = false;
                IsViewingDetail = true;
                viewport.Visibility = Visibility.Visible;
                DetailImage.Visibility = Visibility.Visible;
                TouchBlocker.Visibility = Visibility.Visible;
                ImageGrid.IsEnabled = false;
            }
            else
            {
                progress.IsVisible = false;
                IsViewingDetail = false;
                DetailImage.Visibility = Visibility.Collapsed;
                TouchBlocker.Visibility = Visibility.Collapsed;
                viewport.Visibility = Visibility.Collapsed;
                ImageGrid.IsEnabled = true;
            }
        }

        private async void LoadThumbnails(PictureAlbum album)
        {
            progress.Text = "Loading images...";
            progress.IsVisible = true;
            var group = new List<ThumbnailData>();
            await Task.Run(() =>
            {
                foreach (var pic in album.Pictures)
                {
                    group.Add(new ThumbnailData(pic));
                }
            });
            group.Reverse();
            var groups = new ThumbnailGroup();
            groups.Group = new ObservableCollection<ThumbnailData>(group);
            ImageGrid.DataContext = groups;
            Dispatcher.BeginInvoke(() => { progress.IsVisible = false; });
        }

        private async void ThumbnailImage_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (IsViewingDetail)
            {
                return;
            }
            progress.Text = "Opening image...";
            progress.IsVisible = true;
            var img = sender as Image;
            var thumb = img.DataContext as ThumbnailData;
            await Task.Run(() =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    using (var strm = thumb.picture.GetImage())
                    {
                        using (var replica = new MemoryStream())
                        {
                            strm.CopyTo(replica); // Copy to the new stream to avoid stream crash issue.
                            if (replica.Length <= 0)
                            {
                                return;
                            }
                            replica.Seek(0, SeekOrigin.Begin);

                            _bitmap = new BitmapImage();
                            _bitmap.SetSource(replica);
                            InitBitmapBeforeOpen();
                            DetailImage.Source = _bitmap;
                            SetVisibility(true);
                        }
                    }
                });
            });
        }

        void InitBitmapBeforeOpen()
        {
            Debug.WriteLine("Before open");
            _scale = 0;
            CoerceScale(true);
            _scale = _coercedScale;

            ResizeImage(true);
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (IsViewingDetail)
            {
                ReleaseDetail();
                e.Cancel = true;
            }
        }

        private void PhoneApplicationPage_Unloaded(object sender, RoutedEventArgs e)
        {
            ReleaseDetail();
        }

        const double MaxScale = 1.0;

        double _scale = 1.0;
        double _minScale;
        double _coercedScale;
        double _originalScale;

        Size _viewportSize;
        bool _pinching;
        Point _screenMidpoint;
        Point _relativeMidpoint;

        BitmapImage _bitmap;

        private void viewport_ManipulationStarted(object sender, System.Windows.Input.ManipulationStartedEventArgs e)
        {
            _pinching = false;
            _originalScale = _scale;
        }

        private void viewport_ManipulationDelta(object sender, System.Windows.Input.ManipulationDeltaEventArgs e)
        {
            if (e.PinchManipulation != null)
            {
                e.Handled = true;

                if (!_pinching)
                {
                    _pinching = true;
                    var center = e.PinchManipulation.Original.Center;
                    _relativeMidpoint = new Point(center.X / DetailImage.ActualWidth, center.Y / DetailImage.ActualHeight);

                    var xform = DetailImage.TransformToVisual(viewport);
                    _screenMidpoint = xform.Transform(center);
                }

                _scale = _originalScale * e.PinchManipulation.CumulativeScale;

                CoerceScale(false);
                ResizeImage(false);
            }
            else if (_pinching)
            {
                _pinching = false;
                _originalScale = _scale = _coercedScale;
            }
        }

        private void viewport_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            _pinching = false;
            _scale = _coercedScale;
        }

        private void viewport_ViewportChanged(object sender, System.Windows.Controls.Primitives.ViewportChangedEventArgs e)
        {
            var newSize = new Size(viewport.Viewport.Width, viewport.Viewport.Height);
            if (newSize != _viewportSize)
            {
                _viewportSize = newSize;
                CoerceScale(true);
                ResizeImage(false);
            }
        }

        void ResizeImage(bool center)
        {
            if (_coercedScale != 0 && _bitmap != null)
            {
                double newWidth = canvas.Width = Math.Round(_bitmap.PixelWidth * _coercedScale);
                double newHeight = canvas.Height = Math.Round(_bitmap.PixelHeight * _coercedScale);

                xform.ScaleX = xform.ScaleY = _coercedScale;

                viewport.Bounds = new Rect(0, 0, newWidth, newHeight);

                if (center)
                {
                    viewport.SetViewportOrigin(
                        new Point(
                            Math.Round((newWidth - viewport.ActualWidth) / 2),
                            Math.Round((newHeight - viewport.ActualHeight) / 2)
                            ));
                }
                else
                {
                    var newImgMid = new Point(newWidth * _relativeMidpoint.X, newHeight * _relativeMidpoint.Y);
                    var origin = new Point(newImgMid.X - _screenMidpoint.X, newImgMid.Y - _screenMidpoint.Y);
                    viewport.SetViewportOrigin(origin);
                }
            }
        }

        void CoerceScale(bool recompute)
        {
            if (recompute && _bitmap != null && viewport != null)
            {
                // Calculate the minimum scale to fit the viewport 
                var minX = viewport.ActualWidth / _bitmap.PixelWidth;
                var minY = viewport.ActualHeight / _bitmap.PixelHeight;

                _minScale = Math.Min(minX, minY);
                Debug.WriteLine("Minimum scale: " + _minScale);
            }

            _coercedScale = Math.Min(MaxScale, Math.Max(_scale, _minScale));
            //Debug.WriteLine("Coerced scale: " + _coercedScale);
        }
    }
}