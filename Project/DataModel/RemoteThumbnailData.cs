using Kazyx.WPMMM.PlaybackMode;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace Kazyx.WPMMM.DataModel
{
    public class RemoteThumbnailData : INotifyPropertyChanged
    {
        public RemoteThumbnailData(string uuid, ContentInfo content)
        {
            FetchThumbnailData(uuid, content);
        }

        private async void FetchThumbnailData(string uuid, ContentInfo content)
        {
            try
            {
                CachePath = await ThumbnailCacheLoader.INSTANCE.GetCachePath(uuid, content);
            }
            catch (Exception)
            {
                Debug.WriteLine("Failed to fetch thumbnail image: " + content.ThumbnailUrl);
            }
        }

        public string _CachePath = null;
        public string CachePath
        {
            set
            {
                _CachePath = value;
                OnPropertyChanged("CachePath");
            }
            get
            {
                return CachePath;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (PropertyChanged != null)
                {
                    try
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(name));
                    }
                    catch (COMException)
                    {
                        Debug.WriteLine("Caught COMException: RemoteThumbnailData");
                    }
                }
            });
        }
    }
}
