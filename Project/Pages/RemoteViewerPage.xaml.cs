using Kazyx.RemoteApi.AvContent;
using Kazyx.RemoteApi.Camera;
using Kazyx.WPPMM.Controls;
using Kazyx.WPPMM.DataModel;
using Kazyx.WPPMM.PlaybackMode;
using Kazyx.WPPMM.Resources;
using Kazyx.WPPMM.Utils;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Reactive;
using Microsoft.Xna.Framework.Media;
using NtImageProcessor.MetaData.Misc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Windows.Devices.Geolocation;

namespace Kazyx.WPPMM.Pages
{
    public partial class RemoteViewerPage : PhoneApplicationPage
    {
        public RemoteViewerPage()
        {
            InitializeComponent();
            abm.SetEvent(IconMenu.DownloadMultiple, (sender, e) =>
            {
                DebugUtil.Log("Download clicked");
                if (GridSource != null)
                {
                    GridSource.SelectivityFactor = SelectivityFactor.CopyToPhone;
                }
                RemoteImageGrid.IsSelectionEnabled = true;
            });
            abm.SetEvent(IconMenu.DeleteMultiple, (sender, e) =>
            {
                DebugUtil.Log("Delete clicked");
                if (GridSource != null)
                {
                    GridSource.SelectivityFactor = SelectivityFactor.Delete;
                }
                RemoteImageGrid.IsSelectionEnabled = true;
            });
            abm.SetEvent(IconMenu.ShowDetailInfo, (sender, e) =>
            {
                PhotoPlaybackScreen.DetailInfoVisibility = System.Windows.Visibility.Visible;
                ApplicationBar = abm.Clear().Enable(IconMenu.HideDetailInfo).CreateNew(0.5);
            });
            abm.SetEvent(IconMenu.HideDetailInfo, (sender, e) =>
            {
                PhotoPlaybackScreen.DetailInfoVisibility = System.Windows.Visibility.Collapsed;
                ApplicationBar = abm.Clear().Enable(IconMenu.ShowDetailInfo).CreateNew(0.5);
            });

            abm.SetEvent(IconMenu.Ok, (sender, e) =>
            {
                DebugUtil.Log("Ok clicked");
                switch (InnerState)
                {
                    case ViewerState.AppSettings:
                        CloseAppSettingPanel();
                        break;
                    case ViewerState.RemoteSelecting:
                        switch (GridSource.SelectivityFactor)
                        {
                            case SelectivityFactor.CopyToPhone:
                                UpdateInnerState(ViewerState.Sync);
                                FetchSelectedImages();
                                break;
                            case SelectivityFactor.Delete:
                                UpdateInnerState(ViewerState.RemoteSingle);
                                DeleteSelectedImages();
                                break;
                            default:
                                DebugUtil.Log("Nothing to do for current SelectivityFactor");
                                break;
                        }
                        break;
                    default:
                        DebugUtil.Log("Nothing to do for current InnerState");
                        break;
                }
            });
            abm.SetEvent(IconMenu.ApplicationSetting, (sender, e) =>
            {
                DebugUtil.Log("AppSettings clicked");
                OpenAppSettingPanel();
            });

            var storage_access_settings = new SettingSection(AppResources.SettingSection_ContentsSync);
            AppSettings.Children.Add(storage_access_settings);
            storage_access_settings.Add(new CheckBoxSetting(
                new AppSettingData<bool>(AppResources.Setting_PrioritizeOriginalSize, AppResources.Guide_PrioritizeOriginalSize,
                    () => { return ApplicationSettings.GetInstance().PrioritizeOriginalSizeContents; },
                    enabled => { ApplicationSettings.GetInstance().PrioritizeOriginalSizeContents = enabled; })));

            HideSettingAnimation.Completed += HideSettingAnimation_Completed;

            // TODO: If seek is supported, set vallback of seek bar and enable it.
            //MoviePlaybackScreen.SeekOperated += (NewValue) =>
            //{
            //};
        }

        private ViewerState InnerState = ViewerState.Local;

        private void UpdateInnerState(ViewerState state)
        {
            InnerState = state;
            Dispatcher.BeginInvoke(() =>
            {
                switch (InnerState)
                {
                    case ViewerState.Local:
                    case ViewerState.Sync:
                    case ViewerState.RemoteUnsupported:
                    case ViewerState.RemoteMulti:
                    case ViewerState.RemoteNoMedia:
                    case ViewerState.RemoteMoviePlayback:
                        ApplicationBar = null;
                        break;
                    case ViewerState.RemoteSelecting:
                        ApplicationBar = abm.Clear().Enable(IconMenu.Ok).CreateNew(0.5);
                        break;
                    case ViewerState.RemoteSingle:
                        ApplicationBar = abm.Clear().Enable(IconMenu.DownloadMultiple).Enable(IconMenu.DeleteMultiple).Enable(IconMenu.ApplicationSetting).CreateNew(0.5);
                        break;
                    case ViewerState.AppSettings:
                        ApplicationBar = abm.Clear().Enable(IconMenu.Ok).CreateNew(0.5);
                        break;
                    case ViewerState.LocalStillPlayback:
                    case ViewerState.RemoteStillPlayback:
                        if (PhotoPlaybackScreen.DetailInfoVisibility == System.Windows.Visibility.Visible)
                        {
                            ApplicationBar = abm.Clear().Enable(IconMenu.HideDetailInfo).CreateNew(0.5);
                        }
                        else
                        {
                            ApplicationBar = abm.Clear().Enable(IconMenu.ShowDetailInfo).CreateNew(0.5);
                        }
                        break;
                }
            });
        }

        private void UnlockPivot()
        {
            if (CameraManager.CameraManager.GetInstance().Status.StorageAccessSupported)
            {
                PivotRoot.IsLocked = false;
            }
        }

        private readonly AppBarManager abm = new AppBarManager();

        private bool IsRemoteInitialized = false;
        internal PhotoPlaybackData PhotoData = new PhotoPlaybackData();

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode != NavigationMode.New)
            {
                NavigationService.GoBack();
                return;
            }

            UpdateInnerState(ViewerState.Local);

            DeleteRemoteGridFacially();
            UpdateStorageInfo();
            UnsupportedMessage.Visibility = Visibility.Collapsed;

            GridSource = new DateGroupCollection();
            RemoteImageGrid.ItemsSource = GridSource;

            groups = new ThumbnailGroup();
            LocalImageGrid.DataContext = groups;

            CloseMovieStream();
            MovieDrawer.DataContext = MovieStreamHandler.INSTANCE.MoviePlaybackData;

            PhotoPlaybackScreen.DataContext = PhotoData;
            SetStillDetailVisibility(false);
            LoadLocalContents();

#if DEBUG
            // AddDummyContentsAsync();
#endif
            PictureSyncManager.Instance.Failed += OnDLError;
            PictureSyncManager.Instance.Fetched += OnFetched;
            PictureSyncManager.Instance.Downloader.QueueStatusUpdated += OnFetchingImages;
            CameraManager.CameraManager.GetInstance().Status.PropertyChanged += Status_PropertyChanged;
            MovieStreamHandler.INSTANCE.StreamClosed += MovieStreamHandler_StreamClosed;
            MovieStreamHandler.INSTANCE.StatusChanged += MovieStream_StatusChanged;
        }

        void MovieStream_StatusChanged(object sender, StreamingStatusEventArgs e)
        {
            DebugUtil.Log("StreamStatusChanged: " + e.Status.Status + " - " + e.Status.Factor);
            switch (e.Status.Factor)
            {
                case StreamStatusChangeFactor.FileError:
                case StreamStatusChangeFactor.MediaError:
                case StreamStatusChangeFactor.OtherError:
                    ShowToast(AppResources.Viewer_StreamClosedByExternalCause);
                    CloseMovieStream();
                    break;
                default:
                    break;
            }
        }

        private void CloseMovieStream()
        {
            UpdateInnerState(ViewerState.RemoteSingle);
            MovieStreamHandler.INSTANCE.Finish();
            Dispatcher.BeginInvoke(() =>
            {
                UnlockPivot();
                MoviePlaybackScreen.Reset();
                MovieDrawer.Visibility = Visibility.Collapsed;
            });
        }

        void MovieStreamHandler_StreamClosed(object sender, EventArgs e)
        {
            DebugUtil.Log("StreamClosed");
            CloseMovieStream();
        }

        void Status_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Storages":
                    UpdateStorageInfo();
                    break;
            }
        }

        private void UpdateStorageInfo()
        {
            var storages = CameraManager.CameraManager.GetInstance().Status.Storages;
            StorageAvailable = storages != null && storages.Count != 0 && storages[0].StorageID != StorageId.NoMedia;
        }

        ThumbnailGroup groups = null;

        private void LoadLocalContents()
        {
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
                DebugUtil.Log("No camera roll. Going back");
                NavigationService.GoBack();
                return;
            }
            LoadThumbnails(CameraRoll);
        }

        private async void LoadThumbnails(PictureAlbum album)
        {
            ChangeProgressText(AppResources.Progress_LoadingLocalContents);
            var group = new List<ThumbnailData>();
            await Task.Run(() =>
            {
                foreach (var pic in album.Pictures)
                {
                    group.Add(new ThumbnailData(pic));
                }
            });
            group.Reverse();

            Dispatcher.BeginInvoke(() =>
            {
                if (group != null)
                {
                    groups.Group = new ObservableCollection<ThumbnailData>(group);
                }
            });
            HideProgress();
        }

        private string CurrentUuid { set; get; }

#if DEBUG
        private async void AddDummyContentsAsync()
        {
            CameraManager.CameraManager.GetInstance().Status.StorageAccessSupported = true;
            UnlockPivot();

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
#endif

        private CancellationTokenSource Canceller;

        private DateGroupCollection GridSource;

        private bool CheckRemoteCapability()
        {
            var cm = CameraManager.CameraManager.GetInstance();
            if (cm.CurrentDeviceInfo == null)
            {
                DebugUtil.Log("Device not found");
                return false;
            }
            CurrentUuid = cm.CurrentDeviceInfo.UDN;

            if (cm.AvContentApi == null)
            {
                DebugUtil.Log("AvContent service is not supported");
                return false;
            }

            return true;
        }

        private void OnStorageAvailabilityChanged(bool availability)
        {
            DebugUtil.Log("RemoteViewerPage: OnStorageAvailabilityChanged - " + availability);

            if (availability)
            {
                if (PivotRoot.SelectedIndex == 1 && CheckRemoteCapability())
                {
                    var storages = CameraManager.CameraManager.GetInstance().Status.Storages;
                    if (storages[0].RecordableImages != -1 || storages[0].RecordableMovieLength != -1)
                    {
                        ShowToast(AppResources.Viewer_RefreshAutomatically);
                        InitializeRemote();
                    }
                }
            }
            else
            {
                DeleteRemoteGridFacially();
                ShowToast(AppResources.Viewer_StorageDetached);
                UpdateInnerState(ViewerState.RemoteNoMedia);
                var device = CameraManager.CameraManager.GetInstance().CurrentDeviceInfo;
                if (device != null && device.UDN != null)
                {
                    ThumbnailCacheLoader.INSTANCE.DeleteCache(device.UDN);
                }
            }
        }

        private void DeleteRemoteGridFacially()
        {
            IsRemoteInitialized = false;
            Dispatcher.BeginInvoke(() => { GridSource.Clear(); });
        }

        private bool _StorageAvailable = false;
        private bool StorageAvailable
        {
            set
            {
                var notify = value != _StorageAvailable;
                _StorageAvailable = value;
                if (notify)
                {
                    OnStorageAvailabilityChanged(value);
                }
            }
            get { return _StorageAvailable; }
        }

        private async void InitializeRemote()
        {
            IsRemoteInitialized = true;
            var cm = CameraManager.CameraManager.GetInstance();
            try
            {
                ChangeProgressText(AppResources.Progress_ChangingCameraState);
                await PlaybackModeUtility.MoveToContentTransferModeAsync(cm.CameraApi, cm.Status, 20000);

                ChangeProgressText(AppResources.Progress_CheckingStorage);
                if (!await PlaybackModeUtility.IsStorageSupportedAsync(cm.AvContentApi))
                {
                    DebugUtil.Log("storage scheme is not supported");
                    //GoBack();
                    return;
                }

                var storages = await PlaybackModeUtility.GetStoragesUriAsync(cm.AvContentApi);
                if (storages.Count == 0)
                {
                    DebugUtil.Log("No storages");
                    //GoBack();
                    return;
                }

                Canceller = new CancellationTokenSource();

                ChangeProgressText(AppResources.Progress_FetchingContents);
                await PlaybackModeUtility.GetDateListAsEventsAsync(cm.AvContentApi, storages[0], OnDateListUpdated, Canceller);
            }
            catch (Exception e)
            {
                DebugUtil.Log(e.StackTrace);
                //GoBack();
            }
        }

        private async void OnDateListUpdated(DateListEventArgs args)
        {
            foreach (var date in args.DateList)
            {
                try
                {
                    ChangeProgressText(AppResources.Progress_FetchingContents);
                    await PlaybackModeUtility.GetContentsOfDayAsEventsAsync(
                        CameraManager.CameraManager.GetInstance().AvContentApi, date, true, OnContentListUpdated, Canceller);
                    HideProgress();
                }
                catch (Exception e)
                {
                    DebugUtil.Log(e.StackTrace);
                    //GoBack();
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
            DebugUtil.Log(text);
            Dispatcher.BeginInvoke(() =>
            {
                progress.Text = text;
                progress.IsVisible = true;
            });
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            MovieStreamHandler.INSTANCE.StreamClosed -= MovieStreamHandler_StreamClosed;
            MovieStreamHandler.INSTANCE.StatusChanged -= MovieStream_StatusChanged;
            CameraManager.CameraManager.GetInstance().Status.PropertyChanged -= Status_PropertyChanged;
            PictureSyncManager.Instance.Failed -= OnDLError;
            PictureSyncManager.Instance.Fetched -= OnFetched;
            PictureSyncManager.Instance.Downloader.QueueStatusUpdated -= OnFetchingImages;

            CloseMovieStream();
            MovieDrawer.DataContext = null;

            if (Canceller != null)
            {
                Canceller.Cancel();
            }
            if (GridSource != null)
            {
                GridSource.Clear();
                GridSource = null;
            }

            if (groups != null && groups.Group != null)
            {
                groups.Group.Clear();
                groups = null;
            }

            HideProgress();

            CurrentUuid = null;

            base.OnNavigatedFrom(e);
        }

        private void OnFetched(Picture pic, Geoposition pos)
        {
            DebugUtil.Log("ViewerPage: OnFetched");
            Dispatcher.BeginInvoke(() =>
            {
                var groups = LocalImageGrid.DataContext as ThumbnailGroup;
                if (groups == null)
                {
                    return;
                }
                groups.Group.Insert(0, new ThumbnailData(pic));
            });
        }

        private void OnDLError(ImageDLError error)
        {
            DebugUtil.Log("ViewerPage: OnDLError");
        }

        private void OnFetchingImages(int count)
        {
            if (count != 0)
            {
                ChangeProgressText(string.Format(AppResources.ProgressMessageFetching, count));
            }
            else
            {
                HideProgress();
            }
        }

        private async void ThumbnailImage_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (IsViewingDetail)
            {
                return;
            }
            ChangeProgressText(AppResources.Progress_OpeningDetailImage);
            var img = sender as Image;
            var thumb = img.DataContext as ThumbnailData;
            await Task.Run(() =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    try
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

                                var _bitmap = new BitmapImage();
                                _bitmap.SetSource(replica);
                                InitBitmapBeforeOpen();
                                // DetailImage.Source = _bitmap;
                                PhotoData.Image = _bitmap;
                                try
                                {
                                    PhotoData.MetaData = NtImageProcessor.MetaData.JpegMetaDataParser.ParseImage((Stream)replica);
                                }
                                catch (UnsupportedFileFormatException)
                                {
                                    PhotoData.MetaData = null;
                                    PhotoData.DetailInfoVisibility = System.Windows.Visibility.Collapsed;
                                }
                                SetStillDetailVisibility(true);
                            }
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        ShowToast(AppResources.Viewer_FailedToOpenDetail);
                    }
                });
            });
        }

        private void ImageGrid_Loaded(object sender, RoutedEventArgs e)
        {
            var selector = sender as LongListMultiSelector;
            selector.ItemsSource = GridSource;
        }

        private void ImageGrid_Unloaded(object sender, RoutedEventArgs e)
        {
            var selector = sender as LongListMultiSelector;
            selector.ItemsSource = null;
        }

        private void ImageGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selector = sender as LongListMultiSelector;
            if (selector.IsSelectionEnabled)
            {
                DebugUtil.Log("SelectionChanged in multi mode");
                var contents = selector.SelectedItems;
                DebugUtil.Log("Selected Items: " + contents.Count);
                if (contents.Count > 0)
                {
                    UpdateInnerState(ViewerState.RemoteSelecting);
                }
                else
                {
                    UpdateInnerState(ViewerState.RemoteMulti);
                }
            }
        }

        private void SetStillDetailVisibility(bool visible)
        {
            if (visible)
            {
                PivotRoot.IsLocked = true;
                progress.IsVisible = false;
                IsViewingDetail = true;
                PhotoPlaybackScreen.Visibility = System.Windows.Visibility.Visible;
                TouchBlocker.Visibility = Visibility.Visible;
                RemoteImageGrid.IsEnabled = false;
                LocalImageGrid.IsEnabled = false;
                if (PivotRoot.SelectedIndex == 0)
                {
                    UpdateInnerState(ViewerState.LocalStillPlayback);
                }
                else if (PivotRoot.SelectedIndex == 1)
                {
                    UpdateInnerState(ViewerState.RemoteStillPlayback);
                }
            }
            else
            {
                UnlockPivot();
                progress.IsVisible = false;
                IsViewingDetail = false;
                PhotoPlaybackScreen.Visibility = System.Windows.Visibility.Collapsed;
                TouchBlocker.Visibility = Visibility.Collapsed;
                RemoteImageGrid.IsEnabled = true;
                LocalImageGrid.IsEnabled = true;
                if (PivotRoot.SelectedIndex == 1)
                {
                    UpdateInnerState(ViewerState.RemoteSingle);
                }
            }
        }

        void InitBitmapBeforeOpen()
        {
            DebugUtil.Log("Before open");
            PhotoPlaybackScreen.Init();
        }

        private void viewport_ManipulationStarted(object sender, System.Windows.Input.ManipulationStartedEventArgs e)
        {
            PhotoPlaybackScreen.viewport_ManipulationStarted(sender, e);
        }

        private void viewport_ManipulationDelta(object sender, System.Windows.Input.ManipulationDeltaEventArgs e)
        {
            PhotoPlaybackScreen.viewport_ManipulationDelta(sender, e);
        }

        private void viewport_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            PhotoPlaybackScreen.viewport_ManipulationCompleted(sender, e);
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, CancelEventArgs e)
        {
            if (IsViewingDetail)
            {
                ReleaseDetail();
                e.Cancel = true;
            }
            if (MovieDrawer.Visibility == Visibility.Visible || MovieStreamHandler.INSTANCE.IsProcessing)
            {
                CloseMovieStream();
                e.Cancel = true;
            }

            if (RemoteImageGrid.IsSelectionEnabled)
            {
                RemoteImageGrid.IsSelectionEnabled = false;
                e.Cancel = true;
            }

            if (AppSettingPanel.Visibility == Visibility.Visible)
            {
                CloseAppSettingPanel();
                e.Cancel = true;
            }
        }

        private void ReleaseDetail()
        {
            PhotoPlaybackScreen.ReleaseImage();
            SetStillDetailVisibility(false);
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

        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RemoteImageGrid.IsSelectionEnabled = false;
            var pivot = sender as Pivot;
            switch (pivot.SelectedIndex)
            {
                case 0:
                    UpdateInnerState(ViewerState.Local);
                    break;
                case 1:
                    if (CheckRemoteCapability())
                    {
                        if (IsRemoteInitialized)
                        {
                            UpdateInnerState(ViewerState.RemoteSingle);
                        }
                        else
                        {
                            if (StorageAvailable)
                            {
                                UpdateInnerState(ViewerState.RemoteSingle);
                                InitializeRemote();
                            }
                            else
                            {
                                ShowToast(AppResources.Viewer_NoStorage);
                                UpdateInnerState(ViewerState.RemoteNoMedia);
                            }
                        }
                    }
                    else
                    {
                        UpdateInnerState(ViewerState.RemoteUnsupported);
                        ShowToast(AppResources.Viewer_StorageAccessNotSupported);
                        UnsupportedMessage.Visibility = Visibility.Visible;
                    }
                    break;
            }
        }

        private void DeleteSelectedImages()
        {
            var items = RemoteImageGrid.SelectedItems;
            if (items.Count == 0)
            {
                HideProgress();
                return;
            }
            var contents = new TargetContents();
            contents.ContentUris = new List<string>();
            foreach (var item in items)
            {
                var data = item as RemoteThumbnail;
                contents.ContentUris.Add(data.Source.Uri);
            }
            DeleteContents(contents);
            RemoteImageGrid.IsSelectionEnabled = false;
        }

        private void FetchSelectedImages()
        {
            var items = RemoteImageGrid.SelectedItems;
            if (items.Count == 0)
            {
                HideProgress();
                return;
            }
            foreach (var item in items)
            {
                try
                {
                    EnqueueImageDownload(item as RemoteThumbnail);
                }
                catch (Exception e)
                {
                    DebugUtil.Log(e.StackTrace);
                }
            }
            RemoteImageGrid.IsSelectionEnabled = false;
        }

        private void EnqueueImageDownload(RemoteThumbnail source)
        {
            if (ApplicationSettings.GetInstance().PrioritizeOriginalSizeContents && source.Source.OriginalUrl != null)
            {
                PictureSyncManager.Instance.Enqueue(new Uri(source.Source.OriginalUrl));
                return;
            }
            // Fallback to large size image
            PictureSyncManager.Instance.Enqueue(new Uri(source.Source.LargeUrl));
        }

        private void ShowToast(string message)
        {
            Dispatcher.BeginInvoke(() =>
            {
                ToastMessage.Text = message;
                ToastApparance.Begin();
            });
        }

        private void ToastApparance_Completed(object sender, EventArgs e)
        {
            Scheduler.Dispatcher.Schedule(() =>
            {
                ToastDisApparance.Begin();
            }, TimeSpan.FromSeconds(3));
        }

        private void RemoteImageGrid_IsSelectionEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var selector = (sender as LongListMultiSelector);
            if (!selector.IsSelectionEnabled && GridSource != null)
            {
                GridSource.SelectivityFactor = SelectivityFactor.None;
                HeaderBlocker.Visibility = Visibility.Collapsed;
                UnlockPivot();
            }
            if (selector.IsSelectionEnabled && GridSource != null)
            {
                switch (GridSource.SelectivityFactor)
                {
                    case SelectivityFactor.CopyToPhone:
                        HeaderBlockerText.Text = AppResources.Viewer_Header_SelectingToDownload;
                        HeaderBlocker.Visibility = Visibility.Visible;
                        break;
                    case SelectivityFactor.Delete:
                        HeaderBlockerText.Text = AppResources.Viewer_Header_SelectingToDelete;
                        HeaderBlocker.Visibility = Visibility.Visible;
                        break;
                }
                PivotRoot.IsLocked = true;
            }
            if (PivotRoot.SelectedIndex == 1)
            {
                if (selector.IsSelectionEnabled)
                {
                    UpdateInnerState(ViewerState.RemoteMulti);
                }
                else
                {
                    UpdateInnerState(ViewerState.RemoteSingle);
                }
            }
        }

        private void PhoneApplicationPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            DebugUtil.Log("Orientation changed: " + e.Orientation);
            switch (e.Orientation)
            {
                case PageOrientation.LandscapeLeft:
                    MoviePlaybackScreen.Margin = new Thickness(12, 12, 72, 12);
                    break;
                case PageOrientation.LandscapeRight:
                    MoviePlaybackScreen.Margin = new Thickness(72, 12, 12, 12);
                    break;
                case PageOrientation.Portrait:
                    MoviePlaybackScreen.Margin = new Thickness(12);
                    break;
            }
            PhotoPlaybackScreen.OrientationChanged(e.Orientation);
        }

        private void RemoteThumbnail_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (RemoteImageGrid.IsSelectionEnabled)
            {
                DebugUtil.Log("Ignore tap in multi-selection mode.");
                return;
            }

            var image = sender as Image;
            var content = image.DataContext as RemoteThumbnail;
            PlaybackContent(content);
        }

        private void Playback_Click(object sender, RoutedEventArgs e)
        {
            var item = sender as MenuItem;
            var content = item.DataContext as RemoteThumbnail;
            PlaybackContent(content);
        }

        private async void PlaybackContent(RemoteThumbnail content)
        {
            if (content != null)
            {
                switch (content.Source.ContentType)
                {
                    case ContentKind.StillImage:
                        ChangeProgressText(AppResources.Progress_OpeningDetailImage);
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
                                        var _bitmap = new BitmapImage();
                                        _bitmap.SetSource(replica);
                                        InitBitmapBeforeOpen();
                                        PhotoData.Image = _bitmap;
                                        try
                                        {
                                            PhotoData.MetaData = NtImageProcessor.MetaData.JpegMetaDataParser.ParseImage((Stream)replica);
                                        }
                                        catch (UnsupportedFileFormatException)
                                        {
                                            PhotoData.MetaData = null;
                                            PhotoData.DetailInfoVisibility = System.Windows.Visibility.Collapsed;
                                        }
                                        SetStillDetailVisibility(true);
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
                            DebugUtil.Log(ex.StackTrace);
                            HideProgress();
                        }
                        break;
                    case ContentKind.MovieMp4:
                    case ContentKind.MovieXavcS:
                        if (MovieStreamHandler.INSTANCE.IsProcessing)
                        {
                            MovieStreamHandler.INSTANCE.Finish();
                        }
                        var av = CameraManager.CameraManager.GetInstance().AvContentApi;

                        if (av != null)
                        {
                            if (content.Source.RemotePlaybackAvailable)
                            {
                                PivotRoot.IsLocked = true;
                                UpdateInnerState(ViewerState.RemoteMoviePlayback);
                                MovieDrawer.Visibility = Visibility.Visible;
                                ChangeProgressText(AppResources.Progress_OpeningMovieStream);
                                var started = await MovieStreamHandler.INSTANCE.Start(av, new PlaybackContent
                                {
                                    Uri = content.Source.Uri,
                                    RemotePlayType = RemotePlayMode.SimpleStreaming
                                }, content.Source.Name);
                                if (!started)
                                {
                                    ShowToast(AppResources.Viewer_FailedPlaybackMovie);
                                    CloseMovieStream();
                                }
                                HideProgress();
                            }
                            else
                            {
                                ShowToast(AppResources.Viewer_UnplayableContent);
                            }
                        }
                        else
                        {
                            DebugUtil.Log("Not ready to start stream: " + content.Source.Uri);
                            ShowToast(AppResources.Viewer_NoAvContentApi);
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private void CopyToPhone_Click(object sender, RoutedEventArgs e)
        {
            var item = sender as MenuItem;
            try
            {
                EnqueueImageDownload(item.DataContext as RemoteThumbnail);
            }
            catch (Exception ex)
            {
                DebugUtil.Log(ex.StackTrace);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var item = sender as MenuItem;
            var data = item.DataContext as RemoteThumbnail;

            var contents = new TargetContents();
            contents.ContentUris = new List<string>();
            contents.ContentUris.Add(data.Source.Uri);
            DeleteContents(contents);
        }

        private async void DeleteContents(TargetContents contents)
        {
            var av = CameraManager.CameraManager.GetInstance().AvContentApi;
            if (av != null && contents != null)
            {
                ChangeProgressText(AppResources.Progress_DeletingSelectedContents);
                try
                {
                    await av.DeleteContentAsync(contents);
                    DeleteRemoteGridFacially();
                    if (PivotRoot.SelectedIndex == 1)
                    {
                        InitializeRemote();
                    }
                }
                catch (Exception e)
                {
                    DebugUtil.Log("Failed to delete contents");
                }
                HideProgress();
            }
            DebugUtil.Log("Not ready to delete contents");
        }

        private void OpenAppSettingPanel()
        {
            AppSettingPanel.Visibility = System.Windows.Visibility.Visible;
            UpdateInnerState(ViewerState.AppSettings);
            ShowSettingAnimation.Begin();
        }

        private void CloseAppSettingPanel()
        {
            HideSettingAnimation.Begin();
            UpdateInnerState(ViewerState.RemoteSingle);
        }

        void HideSettingAnimation_Completed(object sender, EventArgs e)
        {
            AppSettingPanel.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void PivotRoot_Loaded(object sender, RoutedEventArgs e)
        {
            var pivot = sender as Pivot;
            pivot.IsLocked = !CameraManager.CameraManager.GetInstance().Status.StorageAccessSupported;
        }
    }

    public enum ViewerState
    {
        Local,
        LocalStillPlayback,
        RemoteUnsupported,
        RemoteNoMedia,
        RemoteSingle,
        RemoteMulti,
        RemoteSelecting,
        Sync,
        RemoteStillPlayback,
        RemoteMoviePlayback,
        AppSettings,
    }
}
