using Kazyx.WPPMM.DataModel;
using Kazyx.WPPMM.PlaybackMode;
using Microsoft.Phone.Controls;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Navigation;

namespace Kazyx.WPPMM.Pages
{
    public partial class RemoteViewerPage : PhoneApplicationPage
    {
        public RemoteViewerPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Contents.Clear();
            Initialize();
        }

        private CancellationTokenSource canceller;

        private RemoteThumbnailGroup GridSource = new RemoteThumbnailGroup();
        private ObservableCollection<RemoteThumbnailData> Contents = new ObservableCollection<RemoteThumbnailData>();

        private async void Initialize()
        {
            var cm = CameraManager.CameraManager.GetInstance();
            if (cm.AvContentApi == null)
            {
                Debug.WriteLine("AvContent service is not supported");
                GoBack();
                return;
            }

            try
            {
                ChangeProgressText("Chaging camera state...");
                progress.IsVisible = true;
                await PlaybackModeUtility.MoveToContentTransferModeAsync(cm.CameraApi, cm.cameraStatus);

                ChangeProgressText("Checking storage capability...");
                if (!await PlaybackModeUtility.IsStorageSupportedAsync(cm.AvContentApi))
                {
                    Debug.WriteLine("storage scheme is not supported");
                    GoBack();
                    return;
                }

                ChangeProgressText("Checking storage uri...");
                var storages = await PlaybackModeUtility.GetStoragesUriAsync(cm.AvContentApi);
                if (storages.Count == 0)
                {
                    Debug.WriteLine("No storages");
                    GoBack();
                    return;
                }

                canceller = new CancellationTokenSource();

                ChangeProgressText("Fetching date list...");
                await PlaybackModeUtility.GetDateListAsEventsAsync(cm.AvContentApi, storages[0], OnDateListUpdated, canceller);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.StackTrace);
                GoBack();
            }
        }

        private async void OnDateListUpdated(DateListEventArgs args)
        {
            foreach (var date in args.DateList)
            {
                try
                {
                    ChangeProgressText("Fetching contents...");
                    await PlaybackModeUtility.GetContentsOfDayAsEventsAsync(
                        CameraManager.CameraManager.GetInstance().AvContentApi, date, true, OnContentListUpdated, canceller);
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
            Dispatcher.BeginInvoke(() =>
            {
                foreach (var content in args.ContentList)
                {
                    Contents.Add(
                        new RemoteThumbnailData(CameraManager.CameraManager.GetInstance().CurrentDeviceInfo.UDN, args.DateInfo, content));
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
            Dispatcher.BeginInvoke(() =>
            {
                progress.Text = text;
            });
        }

        private void GoBack()
        {
            Dispatcher.BeginInvoke(() =>
            {
                progress.IsVisible = false;
                // NavigationService.GoBack();
            });
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (canceller != null)
            {
                canceller.Cancel();
            }
            Contents.Clear();
            HideProgress();
            base.OnNavigatedFrom(e);
        }

        private void ThumbnailImage_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }

        private void PhoneApplicationPage_Unloaded(object sender, RoutedEventArgs e)
        {
        }

        private void ImageGrid_Loaded(object sender, RoutedEventArgs e)
        {
            var selector = sender as LongListSelector;
            selector.DataContext = GridSource;
        }

        private void ImageGrid_Unloaded(object sender, RoutedEventArgs e)
        {
            var selector = sender as LongListSelector;
            selector.DataContext = null;
        }
    }
}