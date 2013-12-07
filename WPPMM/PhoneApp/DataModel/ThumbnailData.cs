using Microsoft.Xna.Framework.Media;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace WPPMM.DataModel
{
    public class ThumbnailData : INotifyPropertyChanged
    {
        public readonly Picture picture;

        private BitmapImage bitmap;

        public ThumbnailData(Picture picture)
        {
            this.picture = picture;
        }

        public BitmapImage RowImage
        {
            get
            {
                if (bitmap != null)
                {
                    return bitmap;
                }
                var tmp = new BitmapImage() { CreateOptions = BitmapCreateOptions.None };
                //tmp.ImageOpened += (sender, e) => { RowImage = tmp; };
                using (var strm = picture.GetThumbnail())
                {
                    tmp.SetSource(strm);
                }
                bitmap = tmp;
                return bitmap;
            }
            private set
            {
                bitmap = value;
                bitmap.Dispatcher.BeginInvoke(() =>
                {
                    OnPropertyChanged("RowImage");
                });
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            //Debug.WriteLine("OnPropertyChanged: " + name);
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

    public class ThumbnailGroup : INotifyPropertyChanged
    {
        ObservableCollection<ThumbnailData> _Group = new ObservableCollection<ThumbnailData>();

        public ObservableCollection<ThumbnailData> Group
        {
            set
            {
                _Group = value;
                OnPropertyChanged("Group");
            }
            get { return _Group; }
        }

        public void Add(ThumbnailData data)
        {
            _Group.Add(data);
            OnPropertyChanged("Group");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            //Debug.WriteLine("OnPropertyChanged: " + name);
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
