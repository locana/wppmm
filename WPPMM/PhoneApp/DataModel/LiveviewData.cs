using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WPPMM.DataModel
{
    public class LiveviewData : INotifyPropertyChanged
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
                    Debug.WriteLine("Caught COMException: LiveviewData");
                }
            }
        }
    }
}
