using Microsoft.Phone.Reactive;
using Microsoft.Xna.Framework.Media;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using Windows.Devices.Geolocation;

namespace Kazyx.WPPMM.CameraManager
{
    class Downloader
    {

        public Downloader()
        {
        }

        internal void DownloadImageFile(Uri uri, Geoposition position, Action<Picture> OnCompleted, Action<ImageDLError> OnError)
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
                    // geo tagging
                    if (position != null)
                    {

                        byte[] buf = new byte[1000000];

                        if (strm.CanRead)
                        {
                            int read;
                            read = strm.Read(buf, 0, (int)strm.Length);
                            if (read > 0)
                            {
                                var image = new byte[read];
                                Array.Copy(buf, image, read);
                                var NewImage = NtImageProcessor.MetaData.MetaDataOperator.AddGeoposition(image, position);
                                return new MediaLibrary().SavePictureToCameraRoll( string.Format("Geo_CameraRemote{0:yyyyMMdd_HHmmss}.jpg", DateTime.Now), NewImage);
                            }
                        }

                    }
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
