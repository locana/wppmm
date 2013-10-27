using Microsoft.Phone.Controls;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using WPPMM.DataModel;

namespace WPPMM.Pages
{
    public partial class ViewerPage : PhoneApplicationPage
    {
        public ViewerPage()
        {
            InitializeComponent();
        }

        private bool IsViewingDetail = false;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
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
        }

        private void ReleaseDetail()
        {
            if (DetailImage.Source != null)
            {
                DetailImage.Source = null;
            }
            SetVisibility(false);
        }

        private void SetVisibility(bool visible)
        {
            if (visible)
            {
                IsViewingDetail = true;
                DetailImage.Visibility = Visibility.Visible;
                TouchBlocker.Visibility = Visibility.Visible;
                ImageGrid.IsEnabled = false;
            }
            else
            {
                IsViewingDetail = false;
                DetailImage.Visibility = Visibility.Collapsed;
                TouchBlocker.Visibility = Visibility.Collapsed;
                ImageGrid.IsEnabled = true;
            }
        }

        private async void LoadThumbnails(PictureAlbum album)
        {
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
        }

        private void ThumbnailImage_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (IsViewingDetail)
            {
                return;
            }
            var img = sender as Image;
            var thumb = img.DataContext as ThumbnailData;
            var bmp = new BitmapImage();
            bmp.CreateOptions = BitmapCreateOptions.None;
            bmp.SetSource(thumb.picture.GetImage());
            DetailImage.Source = bmp;
            SetVisibility(true);
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
    }
}