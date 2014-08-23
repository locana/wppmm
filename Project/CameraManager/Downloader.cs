using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using Windows.Devices.Geolocation;

namespace Kazyx.WPPMM.CameraManager
{
    public class Downloader
    {
        private Task task;

        private readonly Queue<DownloadRequest> DownloadQueue = new Queue<DownloadRequest>();
        internal Action<int> QueueStatusUpdated;

        internal void AddDownloadQueue(Uri uri, Geoposition position, Action<Picture> OnCompleted, Action<ImageDLError> OnError)
        {
            var req = new DownloadRequest { Uri = uri, GeoPosition = position, Completed = OnCompleted, Error = OnError };
            Debug.WriteLine("Enqueue " + uri.AbsoluteUri);
            DownloadQueue.Enqueue(req);
            if (QueueStatusUpdated != null)
            {
                QueueStatusUpdated(DownloadQueue.Count);
            }
            ProcessQueueSequentially();
        }

        private void ProcessQueueSequentially()
        {
            if (task == null)
            {
                Debug.WriteLine("Create new task");
                task = Task.Factory.StartNew(async () =>
                {
                    while (DownloadQueue.Count != 0)
                    {
                        Debug.WriteLine("Dequeue - remaining " + DownloadQueue.Count);
                        await DownloadImage(DownloadQueue.Dequeue());
                        if (QueueStatusUpdated != null)
                        {
                            QueueStatusUpdated(DownloadQueue.Count);
                        }
                    }
                    Debug.WriteLine("Queue end. Kill task");
                    task = null;
                });
            }
        }

        private async Task DownloadImage(DownloadRequest req)
        {
            Debug.WriteLine("Run DownloadImage task");
            try
            {
                var strm = await GetResponseStreamAsync(req.Uri);
                // geo tagging
                var GeoTaggingError = ImageDLError.None;
                if (req.GeoPosition != null)
                {
                    try
                    {
                        strm = NtImageProcessor.MetaData.MetaDataOperator.AddGeoposition(strm, req.GeoPosition);
                    }
                    catch (NtImageProcessor.MetaData.Misc.GpsInformationAlreadyExistsException)
                    {
                        Debug.WriteLine("Caught GpsInformationAlreadyExistsException.");
                        GeoTaggingError = ImageDLError.GeotagAlreadyExists;
                    }
                    catch (Exception)
                    {
                        Debug.WriteLine("Caught exception during geotagging");
                        GeoTaggingError = ImageDLError.GeotagAddition;
                    }

                }
                var pic = new MediaLibrary().SavePictureToCameraRoll(//
                        string.Format("CameraRemote{0:yyyyMMdd_HHmmss}.jpg", DateTime.Now), strm);
                strm.Dispose();

                if (GeoTaggingError == ImageDLError.GeotagAddition)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() => req.Error.Invoke(ImageDLError.GeotagAddition));
                }
                else if (GeoTaggingError == ImageDLError.GeotagAlreadyExists)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() => req.Error.Invoke(ImageDLError.GeotagAlreadyExists));
                }

                if (pic == null)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() => req.Error.Invoke(ImageDLError.Saving));
                    return;
                }
                else
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() => req.Completed.Invoke(pic));
                }
            }
            catch (HttpStatusException e)
            {
                if (e.StatusCode == HttpStatusCode.Gone)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() => req.Error.Invoke(ImageDLError.Gone));
                }
                else
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() => req.Error.Invoke(ImageDLError.Network));
                }
            }
            catch (WebException)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() => req.Error.Invoke(ImageDLError.Network));
            }
            catch (Exception e)
            {
                // Some devices throws exception while saving picture to camera roll.
                // e.g.) HTC 8S
                Debug.WriteLine("Caught exception at saving picture: " + e.Message);
                Debug.WriteLine(e.StackTrace);
                Deployment.Current.Dispatcher.BeginInvoke(() => req.Error.Invoke(ImageDLError.DeviceInternal));
            }
        }

        private static Task<Stream> GetResponseStreamAsync(Uri uri)
        {
            var tcs = new TaskCompletionSource<Stream>();

            var request = HttpWebRequest.Create(uri) as HttpWebRequest;
            request.Method = "GET";

            var ResponseHandler = new AsyncCallback((res) =>
            {
                try
                {
                    var result = request.EndGetResponse(res) as HttpWebResponse;
                    if (result == null)
                    {
                        tcs.TrySetException(new WebException("No HttpWebResponse"));
                        return;
                    }
                    var code = result.StatusCode;
                    if (code == HttpStatusCode.OK)
                    {
                        tcs.TrySetResult(result.GetResponseStream());
                    }
                    else
                    {
                        Debug.WriteLine("Http Status Error: " + code);
                        tcs.TrySetException(new HttpStatusException(code));
                    }
                }
                catch (WebException e)
                {
                    var result = e.Response as HttpWebResponse;
                    if (result != null)
                    {
                        Debug.WriteLine("Http Status Error: " + result.StatusCode);
                        tcs.TrySetException(new HttpStatusException(result.StatusCode));
                    }
                    else
                    {
                        Debug.WriteLine("WebException: " + e.Status);
                        tcs.TrySetException(e);
                    }
                };
            });

            request.BeginGetResponse(ResponseHandler, null);
            return tcs.Task;
        }
    }

    class HttpStatusException : Exception
    {
        public readonly HttpStatusCode StatusCode;
        public HttpStatusException(HttpStatusCode code)
        {
            this.StatusCode = code;
        }
    }

    class DownloadRequest
    {
        public Uri Uri;
        public Geoposition GeoPosition;
        public Action<Picture> Completed;
        public Action<ImageDLError> Error;
    }

    public enum ImageDLError
    {
        Network,
        Saving,
        Argument,
        DeviceInternal,
        GeotagAlreadyExists,
        GeotagAddition,
        Gone,
        Unknown,
        None,
    }
}
