using Kazyx.WPPMM.Utils;
using NtImageProcessor.MetaData.Structure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Kazyx.WPPMM.DataModel
{
    public class PhotoPlaybackData : INotifyPropertyChanged
    {
        public PhotoPlaybackData() { }

        private BitmapImage _Image = null;
        public BitmapImage Image
        {
            get { return _Image; }
            set
            {
                _Image = value;
                OnPropertyChanged("Image");
            }
        }

        private JpegMetaData _MetaData;
        public JpegMetaData MetaData
        {
            get { return _MetaData; }
            set
            {
                _MetaData = value;
                OnPropertyChanged("MetaData");
                OnPropertyChanged("IsoValue");
                OnPropertyChanged("FValue");
                OnPropertyChanged("ShutterSpeedValue");
            }
        }

        public string IsoValue { get { return GetIntValue(0x0083); } }
        public string FValue { get { return GetDoubleValue(0x829D); } }
        public string ShutterSpeedValue { get { return GetDoubleValue(0x829A); } }

        string GetIntValue(uint key)
        {
            if (MetaData == null) { return "--"; }

            if (MetaData.PrimaryIfd != null && MetaData.PrimaryIfd.Entries.ContainsKey(key))
            {
                return MetaData.PrimaryIfd.Entries[key].IntValues[0].ToString();
            }
            else if (MetaData.ExifIfd != null && MetaData.ExifIfd.Entries.ContainsKey(key))
            {
                return MetaData.ExifIfd.Entries[key].IntValues[0].ToString();
            }
            else if (MetaData.GpsIfd != null && MetaData.GpsIfd.Entries.ContainsKey(key))
            {
                return MetaData.GpsIfd.Entries[key].IntValues[0].ToString();
            }
            return "--";
        }

        string GetDoubleValue(uint key)
        {
            if (MetaData == null) { return "--"; }

            if (MetaData.PrimaryIfd != null && MetaData.PrimaryIfd.Entries.ContainsKey(key))
            {
                return MetaData.PrimaryIfd.Entries[key].DoubleValues[0].ToString();
            }
            else if (MetaData.ExifIfd != null && MetaData.ExifIfd.Entries.ContainsKey(key))
            {
                return MetaData.ExifIfd.Entries[key].DoubleValues[0].ToString();
            }
            else if (MetaData.GpsIfd != null && MetaData.GpsIfd.Entries.ContainsKey(key))
            {
                return MetaData.GpsIfd.Entries[key].DoubleValues[0].ToString();
            }
            return "--";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                DebugUtil.Log("Property changed: " + name);
                try
                {
                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(name));
                    }
                }
                catch (COMException)
                {
                    DebugUtil.Log("Caught COMException: PhotoPlaybackData");
                }
                catch (NullReferenceException e)
                {
                    DebugUtil.Log(e.StackTrace);
                }
            });
        }
    }
}
