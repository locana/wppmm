using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.UI.Xaml.Media.Imaging;

namespace WRTPMM
{
    class ImageData : INotifyPropertyChanged
    {
        BitmapImage _image = null;
        public BitmapImage image
        {
            set
            {
                _image = value;
                OnPropertyChanged("image");
            }
            get { return _image; }
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
