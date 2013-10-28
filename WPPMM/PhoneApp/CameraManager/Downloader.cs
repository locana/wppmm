using Microsoft.Phone.Reactive;
using Microsoft.Xna.Framework.Media;
using System;
using System.Diagnostics;
using System.Net;
using System.Windows;

namespace WPPMM.CameraManager
{
    class Downloader
    {

        public Downloader()
        {
        }

        internal void DownloadImageFile(Uri uri, Action<Picture> OnCompleted, Action OnError)
        {
            WebRequest request;
            try { request = HttpWebRequest.Create(uri); }
            catch (Exception e) {
                Debug.WriteLine("Exception: HttpWebRequest.create(uri): " + e.Message);
                Deployment.Current.Dispatcher.BeginInvoke(OnError); 
                return; }

            Observable.FromAsyncPattern<WebResponse>(request.BeginGetResponse, request.EndGetResponse)()
            .Select(res =>
            {
                try { return res.GetResponseStream(); }
                catch (Exception e) {
                    Debug.WriteLine("Exception: GetResponseStream: " + e.Message); 
                    return null;
                }
            })
            .Select(strm =>
            {
                if (strm == null) { return null; }
                try { 
                    
                    return new MediaLibrary().SavePictureToCameraRoll(string.Format("SavedPicture{0}.jpg", DateTime.Now), strm); }
                catch (Exception e) {
                    Debug.WriteLine("exception: savePicture: " + e.Message);
                    return null; }
            })
            .ObserveOnDispatcher()
            .Subscribe(pic =>
            {
                if (pic == null)
                {
                    Debug.WriteLine("Saved Picture is null");
                    OnError();
                }
                else
                {
                    OnCompleted(pic);
                }
            }
            , e =>
            {
                OnError.Invoke();
            });
        }
    }
}
