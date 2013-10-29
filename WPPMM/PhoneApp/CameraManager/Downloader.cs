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

        internal void DownloadImageFile(Uri uri, Action<Picture> OnCompleted, Action<ImageDLError> OnError)
        {
            WebRequest request;
            try { request = HttpWebRequest.Create(uri); }
            catch (Exception e)
            {
                Debug.WriteLine("Exception: HttpWebRequest.create(uri): " + e.Message);
                Deployment.Current.Dispatcher.BeginInvoke(OnError);
                return;
            }

            Observable.FromAsyncPattern<WebResponse>(request.BeginGetResponse, request.EndGetResponse)()
            .Select(res =>
            {
                try
                {
                    if (res == null)
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(OnError, ImageDLError.Network);
                        return null;
                    }
                    var strm = res.GetResponseStream();
                    if (strm == null)
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(OnError, ImageDLError.Network);
                    }
                    return strm;
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Caught exception at getting stream: " + e.Message);
                    Deployment.Current.Dispatcher.BeginInvoke(OnError, ImageDLError.Network);
                    return null;
                }
            })
            .Select(strm =>
            {
                if (strm == null) { return null; }
                try
                {
                    var pic = new MediaLibrary().SavePictureToCameraRoll(string.Format("CameraRemote{0:yyyyMMdd_HHmmss}.jpg", DateTime.Now), strm);
                    if (pic == null)
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(OnError, ImageDLError.Saving);
                    }
                    return pic;
                }
                catch (Exception e)
                {
                    // Some devices throws exception while saving picture to camera roll.
                    // e.g.) HTC 8S
                    Debug.WriteLine("Caught exception at saving picture: " + e.Message);
                    Deployment.Current.Dispatcher.BeginInvoke(OnError, ImageDLError.Unknown);
                    return null;
                }
            })
            .ObserveOnDispatcher()
            .Subscribe(pic =>
            {
                if (pic != null)
                {
                    OnCompleted(pic);
                }
            }
            , e =>
            {
                OnError.Invoke(ImageDLError.Unknown);
            });
        }
    }

    public enum ImageDLError
    {
        Network,
        Saving,
        Unknown
    }
}
