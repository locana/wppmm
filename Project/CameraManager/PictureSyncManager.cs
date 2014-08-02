using Kazyx.WPMMM.Resources;
using Kazyx.WPPMM.CameraManager;
using Kazyx.WPPMM.DataModel;
using Microsoft.Xna.Framework.Media;
using System;
using System.Diagnostics;
using System.Windows;
using Windows.Devices.Geolocation;

namespace Kazyx.WPMMM.CameraManager
{
    class PictureSyncManager
    {
        private PictureSyncManager()
        {
        }

        private static readonly PictureSyncManager _Instance = new PictureSyncManager();

        internal static PictureSyncManager Instance
        {
            get { return _Instance; }
        }

        public Downloader Downloader = new Downloader();

        public Action<Picture, Geoposition> Fetched;

        public Action<ImageDLError> Failed;

        public Action<string> Message;

        protected void OnMessage(string message)
        {
            Debug.WriteLine("PictureSyncManager: OnMessage" + message);
            if (Message != null)
            {
                Message(message);
            }
        }

        protected void OnFetched(Picture picture, Geoposition pos)
        {
            Debug.WriteLine("PictureSyncManager: OnFetched");
            if (Fetched != null)
            {
                Fetched(picture, pos);
            }
        }

        protected void OnFailed(ImageDLError error)
        {
            Debug.WriteLine("PictureSyncManager: OnFailed" + error);
            if (Failed != null)
            {
                Failed(error);
            }
        }

        internal void Enque(Uri uri)
        {
            Debug.WriteLine("PictureSyncManager: Enque " + uri.AbsolutePath);
            Deployment.Current.Dispatcher.BeginInvoke(async () =>
            {
                Geoposition pos = null;
                if (ApplicationSettings.GetInstance().GeotagEnabled)
                {
                    if (GeopositionManager.GetInstance().LatestPosition == null)
                    {
                        // takes some more time
                        OnMessage(AppResources.WaitingGeoposition);
                    }
                    pos = await GeopositionManager.GetInstance().AcquireGeoPosition();
                }
                Downloader.AddDownloadQueue(uri, pos, (pic) => { OnFetched(pic, pos); }, (error) => { OnFailed(error); });
            });
        }
    }
}
