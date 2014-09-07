using Kazyx.WPPMM.PlaybackMode;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace Kazyx.WPPMM.DataModel
{
    public class RemoteThumbnailData : INotifyPropertyChanged
    {
        public RemoteThumbnailData(string uuid, DateInfo date, ContentInfo content)
        {
            GroupTitle = date.Title;
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

        public string GroupTitle { private set; get; }

        private string _CachePath = null;
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

    public class RemoteThumbnailGroup : INotifyPropertyChanged
    {
        ObservableCollection<RemoteThumbnailData> _Group = new ObservableCollection<RemoteThumbnailData>();

        public ObservableCollection<RemoteThumbnailData> Group
        {
            set
            {
                _Group = value;
                OnPropertyChanged("Group");
            }
            get { return _Group; }
        }

        public void Add(RemoteThumbnailData data)
        {
            _Group.Add(data);
            OnPropertyChanged("Group");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                try
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(name));
                }
                catch (COMException)
                {
                }
            }
        }
    }
}
