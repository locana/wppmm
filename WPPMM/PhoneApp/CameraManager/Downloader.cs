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
                Deployment.Current.Dispatcher.BeginInvoke(() => OnError.Invoke(ImageDLError.Argument));
                return;
            }

            Observable.FromAsyncPattern<WebResponse>(request.BeginGetResponse, request.EndGetResponse)()
            .Select(res =>
            {
                if (res == null)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() => OnError.Invoke(ImageDLError.Network));
                    return null;
                }
                try
                {
                    return res.GetResponseStream();
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Caught exception at getting stream: " + e.Message);
                    return null;
                }
            })
            .Select(strm =>
            {
                if (strm == null)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() => OnError.Invoke(ImageDLError.Network));
                }
                return strm;
            })
            .Where(strm => strm != null)
            .Select(strm =>
            {
                try
                {
                    var pic = new MediaLibrary().SavePictureToCameraRoll(//
                        string.Format("CameraRemote{0:yyyyMMdd_HHmmss}.jpg", DateTime.Now), strm);
                    strm.Dispose();
                    if (pic == null)
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(() => OnError.Invoke(ImageDLError.Saving));
                    }
                    return pic;
                }
                catch (Exception e)
                {
                    // Some devices throws exception while saving picture to camera roll.
                    // e.g.) HTC 8S
                    Debug.WriteLine("Caught exception at saving picture: " + e.Message);
                    Deployment.Current.Dispatcher.BeginInvoke(() => OnError.Invoke(ImageDLError.DeviceInternal));
                    return null;
                }
            })
            .Where(pic => pic != null)
            .ObserveOnDispatcher()
            .Subscribe(pic => { OnCompleted(pic); }, e => { OnError.Invoke(ImageDLError.Unknown); });
        }
    }

    public enum ImageDLError
    {
        Network,
        Saving,
        Argument,
        DeviceInternal,
        Unknown
    }
}
