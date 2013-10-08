using Microsoft.Phone.Reactive;
using Microsoft.Xna.Framework.Media;
using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Resources;

namespace WPPMM.CameraManager
{
    class Downloader
    {

        public Downloader()
        {
        }


        internal void AddDownloadRequest(String url)
        {
            var webClient = new WebClient();
            webClient.OpenReadCompleted += WebClientOpenReadCompleted;
            webClient.OpenReadAsync(new Uri(url, UriKind.Absolute));
        }

        void WebClientOpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {

            const string tempJpeg = "TempJPEG";
            var streamResourceInfo = new StreamResourceInfo(e.Result, null);

            var userStoreForApplication = IsolatedStorageFile.GetUserStoreForApplication();
            if (userStoreForApplication.FileExists(tempJpeg))
            {
                userStoreForApplication.DeleteFile(tempJpeg);
            }

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                var isolatedStorageFileStream = userStoreForApplication.CreateFile(tempJpeg);
                var bitmapImage = new BitmapImage { CreateOptions = BitmapCreateOptions.None };
                bitmapImage.SetSource(streamResourceInfo.Stream);

                var writeableBitmap = new WriteableBitmap(bitmapImage);
                writeableBitmap.SaveJpeg(isolatedStorageFileStream, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight, 0, 85);
                isolatedStorageFileStream.Close();
                isolatedStorageFileStream = userStoreForApplication.OpenFile(tempJpeg, FileMode.Open, FileAccess.Read);

                var mediaLibrary = new MediaLibrary();
                mediaLibrary.SavePicture(string.Format("SavedPicture{0}.jpg", DateTime.Now), isolatedStorageFileStream);
                MessageBox.Show("Picture saved Successfully");

                isolatedStorageFileStream.Close();
            });



        }

        internal void DownloadImageFile(Uri uri, Action<Picture> OnCompleted, Action OnError)
        {
            var request = HttpWebRequest.Create(uri);
            Observable.FromAsyncPattern<WebResponse>(request.BeginGetResponse, request.EndGetResponse)()
            .Select(res => res.GetResponseStream())
            .ObserveOnDispatcher()
            .Subscribe(src =>
            {
                var mediaLibrary = new MediaLibrary();
                var pic = mediaLibrary.SavePicture(string.Format("SavedPicture{0}.jpg", DateTime.Now), src);
                src.Close();
                if (pic == null)
                {
                    OnError.Invoke();
                }
                OnCompleted.Invoke(pic);
            }
            , e =>
            {
                OnError.Invoke();
            });
        }
    }
}
