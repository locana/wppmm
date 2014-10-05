using Kazyx.WPPMM.Utils;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace Kazyx.WPPMM.DataModel
{
    public class LiveviewData : INotifyPropertyChanged
    {
        BitmapImage _image = null;
        public BitmapImage Image
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
            //DebugUtil.Log("OnPropertyChanged: " + name);
            if (PropertyChanged != null)
            {
                // No need to switch to the UI thread. Already on.
                try
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(name));
                }
                catch (COMException)
                {
                    DebugUtil.Log("Caught COMException: LiveviewData");
                }
            }
        }
    }
}
