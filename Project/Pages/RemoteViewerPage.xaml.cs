using Kazyx.RemoteApi.AvContent;
using Kazyx.WPPMM.DataModel;
using Kazyx.WPPMM.PlaybackMode;
using Kazyx.WPPMM.Utils;
using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace Kazyx.WPPMM.Pages
{
    public partial class RemoteViewerPage : PhoneApplicationPage
    {
        private AppBarManager abm = new AppBarManager();

        public RemoteViewerPage()
        {
            InitializeComponent();
            abm.SetEvent(IconMenu.HideHeader, (sender, e) => { SwitchHeader(); });
            abm.SetEvent(IconMenu.ShowHeader, (sender, e) => { SwitchHeader(); });
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            GridSource = new DateGroupCollection();
            ImageGrid.ItemsSource = GridSource;

            SetVisibility(false);

            Initialize();

            AddDummyContentsAsync();
        }

        private string CurrentUuid { set; get; }

        private async void AddDummyContentsAsync()
        {
            if (CurrentUuid == null)
            {
                CurrentUuid = DummyContentsGenerator.RandomUuid();
            }

            for (int i = 0; i < 1; i++)
            {
                foreach (var date in DummyContentsGenerator.RandomDateList(50))
                {
                    var list = new List<RemoteThumbnail>();
                    foreach (var content in DummyContentsGenerator.RandomContentList(50))
                    {
                        list.Add(new RemoteThumbnail(CurrentUuid, date, content));
                    }
                    await Task.Delay(500);
                    Dispatcher.BeginInvoke(() =>
                    {
                        if (GridSource != null)
                        {
                            GridSource.AddRange(list);
                        }
                    });
                }
                await Task.Delay(500);
            }
        }

        private CancellationTokenSource Canceller;

        private DateGroupCollection GridSource;

        private async void Initialize()
        {
            var cm = CameraManager.CameraManager.GetInstance();
            if (cm.CurrentDeviceInfo == null)
            {
                UpdateTitleHeader("Device not found");
                return;
            }
            CurrentUuid = cm.CurrentDeviceInfo.UDN;

            if (cm.AvContentApi == null)
            {
                Debug.WriteLine("AvContent service is not supported");
                UpdateTitleHeader("AvContent service is not supported");
                GoBack();
                return;
            }

            try
            {
                ChangeProgressText("Chaging camera state...");
                await PlaybackModeUtility.MoveToContentTransferModeAsync(cm.CameraApi, cm.cameraStatus);

                ChangeProgressText("Checking storage capability...");
                if (!await PlaybackModeUtility.IsStorageSupportedAsync(cm.AvContentApi))
                {
                    Debug.WriteLine("storage scheme is not supported");
                    UpdateTitleHeader("storage scheme is not supported");
                    GoBack();
                    return;
                }

                ChangeProgressText("Checking storage uri...");
                var storages = await PlaybackModeUtility.GetStoragesUriAsync(cm.AvContentApi);
                if (storages.Count == 0)
                {
                    Debug.WriteLine("No storages");
                    UpdateTitleHeader("No storages");
                    GoBack();
                    return;
                }

                Canceller = new CancellationTokenSource();

                ChangeProgressText("Fetching date list...");
                await PlaybackModeUtility.GetDateListAsEventsAsync(cm.AvContentApi, storages[0], OnDateListUpdated, Canceller);
            }
            catch (Exception e)
            {
                UpdateTitleHeader(e.GetType().ToString());
                Debug.WriteLine(e.StackTrace);
                GoBack();
            }
        }

        private void UpdateTitleHeader(string text)
        {
            Debug.WriteLine(text);
            Dispatcher.BeginInvoke(() =>
            {
                TitleHeader.Text = text;
            });
        }

        private async void OnDateListUpdated(DateListEventArgs args)
        {
            foreach (var date in args.DateList)
            {
                try
                {
                    ChangeProgressText("Fetching contents...");
                    await PlaybackModeUtility.GetContentsOfDayAsEventsAsync(
                        CameraManager.CameraManager.GetInstance().AvContentApi, date, true, OnContentListUpdated, Canceller);
                    HideProgress();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.StackTrace);
                    GoBack();
                }
            }
        }

        private void OnContentListUpdated(ContentListEventArgs args)
        {
            var list = new List<RemoteThumbnail>();
            foreach (var content in args.ContentList)
            {
                list.Add(new RemoteThumbnail(CameraManager.CameraManager.GetInstance().CurrentDeviceInfo.UDN, args.DateInfo, content));
            }

            Dispatcher.BeginInvoke(() =>
            {
                if (GridSource != null)
                {
                    GridSource.AddRange(list);
                }
            });
        }

        private void HideProgress()
        {
            Dispatcher.BeginInvoke(() =>
            {
                progress.IsVisible = false;
            });
        }

        private void ChangeProgressText(string text)
        {
            Debug.WriteLine(text);
            Dispatcher.BeginInvoke(() =>
            {
                progress.Text = text;
                progress.IsVisible = true;
            });
        }

        private void GoBack()
        {
            Dispatcher.BeginInvoke(() =>
            {
                // progress.IsVisible = false;
                // NavigationService.GoBack();
            });
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (Canceller != null)
            {
                Canceller.Cancel();
            }
            GridSource.Clear();
            GridSource = null;
            HideProgress();

            if (CurrentUuid != null)
            {
                ThumbnailCacheLoader.INSTANCE.DeleteCacheDirectory(CurrentUuid);
            }
            CurrentUuid = null;

            if (e.NavigationMode != NavigationMode.Back)
            {
                CameraManager.CameraManager.GetInstance().Refresh();
            }

            base.OnNavigatedFrom(e);
        }

        private void ThumbnailImage_Tap(object sender, GestureEventArgs e)
        {
            progress.IsVisible = true;
            var img = sender as Image;
            var data = img.DataContext as RemoteThumbnail;
            UpdateTitleHeader(data.Source.Name + " - " + data.Source.ContentType);
        }

        private void ImageGrid_Loaded(object sender, RoutedEventArgs e)
        {
            var selector = sender as LongListSelector;
            selector.ItemsSource = GridSource;
        }

        private void ImageGrid_Unloaded(object sender, RoutedEventArgs e)
        {
            var selector = sender as LongListSelector;
            selector.ItemsSource = null;
        }

        private async void ImageGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var content = (sender as LongListSelector).SelectedItem as RemoteThumbnail;
            if (content != null)
            {
                UpdateTitleHeader(content.Source.Name + " - " + content.Source.ContentType);
                switch (content.Source.ContentType)
                {
                    case ContentKind.StillImage:
                        ChangeProgressText("Fetching detail image...");
                        try
                        {
                            using (var strm = await Downloader.GetResponseStreamAsync(new Uri(content.Source.LargeUrl)))
                            {
                                var replica = new MemoryStream();

                                strm.CopyTo(replica); // Copy to the new stream to avoid stream crash issue.
                                if (replica.Length <= 0)
                                {
                                    return;
                                }
                                replica.Seek(0, SeekOrigin.Begin);

                                Dispatcher.BeginInvoke(() =>
                                {
                                    try
                                    {
                                        _bitmap = new BitmapImage();
                                        _bitmap.SetSource(replica);
                                        InitBitmapBeforeOpen();
                                        DetailImage.Source = _bitmap;
                                        SetVisibility(true);
                                    }
                                    finally
                                    {
                                        if (replica != null)
                                        {
                                            replica.Dispose();
                                        }
                                    }
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.StackTrace);
                            UpdateTitleHeader("Failed to fetch detail image");
                            HideProgress();
                        }
                        break;
                    default:
                        break;
                }
            }
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

        void InitBitmapBeforeOpen()
        {
            Debug.WriteLine("Before open");
            _scale = 0;
            CoerceScale(true);
            _scale = _coercedScale;

            ResizeImage(true);
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

        private void PhoneApplicationPage_BackKeyPress(object sender, CancelEventArgs e)
        {
            if (IsViewingDetail)
            {
                ReleaseDetail();
                e.Cancel = true;
            }
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

        private bool IsViewingDetail = false;

        private async void ImageGrid_ItemRealized(object sender, ItemRealizationEventArgs e)
        {
            if (e.ItemKind == LongListSelectorItemKind.Item)
            {
                var content = e.Container.Content as RemoteThumbnail;
                if (content != null)
                {
                    await content.FetchThumbnailAsync();
                }
            }
        }

        private TitleBarState TitleBarState = TitleBarState.Displayed;

        /*
        private void ImageGrid_ManipulationDelta(object sender, System.Windows.Input.ManipulationDeltaEventArgs e)
        {
            if (TitleBarState == TitleBarState.Translating)
            {
                return;
            }

            Debug.WriteLine("TranslationY: " + e.DeltaManipulation.Translation.Y);

            if (e.DeltaManipulation.Translation.Y > 0 && TitleBarState == TitleBarState.Hidden)
            {
                TranslateElementY(0, 109, TitleBarState.Displayed);
            }
            else if (e.DeltaManipulation.Translation.Y < 0 && TitleBarState == TitleBarState.Displayed)
            {
                TranslateElementY(109, 0, TitleBarState.Hidden);
            }
        }
         * */

        private void SwitchHeader()
        {
            switch (TitleBarState)
            {
                case TitleBarState.Displayed:
                    TranslateElementY(109, 0, TitleBarState.Hidden);
                    ApplicationBar = abm.Clear().Enable(IconMenu.ShowHeader).CreateNew(0.0);
                    break;
                case TitleBarState.Hidden:
                    TranslateElementY(0, 109, TitleBarState.Displayed);
                    ApplicationBar = abm.Clear().Enable(IconMenu.HideHeader).CreateNew(0.0);
                    break;
                case TitleBarState.Translating:
                    break;
            }
        }

        private void TranslateElementY(double from, double to, TitleBarState nextState)
        {
            TitleBarState = TitleBarState.Translating;

            var duration = new Duration(TimeSpan.FromSeconds(0.3));
            var story = new Storyboard();
            story.Duration = duration;
            var animation = new DoubleAnimation();
            animation.Duration = duration;
            animation.From = from;
            animation.To = to;
            animation.EasingFunction = new ExponentialEase() { EasingMode = EasingMode.EaseIn, Exponent = 3.0 };

            TitleBlock.RenderTransform = new CompositeTransform();
            Storyboard.SetTarget(animation, TitleBlock);
            Storyboard.SetTargetProperty(animation, new PropertyPath("UIElement.Height"));

            story.Children.Add(animation);

            story.Completed += (sender, e) =>
            {
                Debug.WriteLine("TitleBar animation completed: " + nextState);
                TitleBarState = nextState;
            };
            story.Begin();
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            ApplicationBar = abm.Clear().Enable(IconMenu.HideHeader).CreateNew(0.0);
        }

        /*
        private void ImageGrid_InnerManipulationDelta(object sender, System.Windows.Input.ManipulationDeltaEventArgs e)
        {
            Debug.WriteLine("InnerManipulationDelta");
            ImageGrid_ManipulationDelta(sender, e);
        }

        private void ImageGrid_InnerManipulationStateChanged(object sender, System.Windows.Controls.Primitives.ManipulationStateChangedEventArgs e)
        {
            var lls = sender as ViewportControl;
            switch (lls.ManipulationState)
            {
                case ManipulationState.Idle:
                    Debug.WriteLine("InnerManipulationState: Idle");
                    break;
                case ManipulationState.Manipulating:
                    Debug.WriteLine("InnerManipulationState: Manipulating");
                    break;
                case ManipulationState.Animating:
                    Debug.WriteLine("InnerManipulationState: Animating");
                    break;
            }
        }
         * */
    }

    enum TitleBarState
    {
        Displayed,
        Hidden,
        Translating
    }
}
