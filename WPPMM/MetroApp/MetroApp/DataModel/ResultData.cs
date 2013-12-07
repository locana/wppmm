using System.ComponentModel;
using System.Runtime.InteropServices;

namespace WRTPMM
{
    class ResultData : INotifyPropertyChanged
    {
        string _result = "";
        public string result
        {
            set
            {
                if (value == null)
                {
                    _result = "";
                }
                else
                {
                    _result = value;
                }
                OnPropertyChanged("result");
            }
            get { return _result; }
        }

        public void add(string text)
        {
            result = _result + "\n" + text;
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
