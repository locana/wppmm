using Microsoft.Xna.Framework.Media;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace Kazyx.WPPMM.DataModel
{
    public class PostViewData : INotifyPropertyChanged
    {
        private BitmapImage _postview = null;

        public BitmapImage postview
        {
            internal set
            {
                _postview = value;
                _postview.Dispatcher.BeginInvoke(() =>
                {
                    OnPropertyChanged("postview");
                });
            }
            get { return _postview; }
        }

        private BitmapImage tmp = new BitmapImage()
        {
            CreateOptions = BitmapCreateOptions.None
        };

        public Picture PictureData
        {
            set
            {
                if (value == null)
                {
                    postview = null;
                }
                else
                {
                    var stream = value.GetImage();
                    tmp.SetSource(stream);
                    postview = tmp;
                }
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
                    Debug.WriteLine("Caught COMException: PostviewData");
                }
            }
        }
    }
}
