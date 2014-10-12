using Kazyx.WPPMM.Utils;
using NtImageProcessor.MetaData.Structure;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
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
                OnPropertyChanged("ImageSizeValue");
            }
        }

        public string IsoValue { get { return GetIntValue(0x0083); } }
        public string FValue { get { return GetDoubleValue(0x829D); } }
        public string ShutterSpeedValue { get { return GetDoubleValue(0x829A); } }
        public string FileNameValue { get { return "name"; } }
        public string ImageSizeValue
        {
            get
            {
                return GetIntValue(0x0100) + "x" + GetIntValue(0x0101);
            }
        }

        string GetStringValue(uint key)
        {
            if (MetaData == null) { return "--"; }
            var entry = FindFirstEntry(key);
            if (entry == null) { return "--"; }
            else { return entry.StringValue; }
        }

        string GetIntValue(uint key)
        {
            if (MetaData == null) { return "--"; }
            var entry = FindFirstEntry(key);
            if (entry == null) { return "--"; }
            else { return entry.IntValues[0].ToString(); }
        }

        string GetDoubleValue(uint key)
        {
            if (MetaData == null) { return "--"; }
            var entry = FindFirstEntry(key);
            if (entry == null) { return "--"; }
            else { return entry.DoubleValues[0].ToString(); }
        }

        Entry FindFirstEntry(uint key)
        {
            if (MetaData == null) { return null; }

            if (MetaData.PrimaryIfd != null && MetaData.PrimaryIfd.Entries.ContainsKey(key))
            {
                return MetaData.PrimaryIfd.Entries[key];
            }
            else if (MetaData.ExifIfd != null && MetaData.ExifIfd.Entries.ContainsKey(key))
            {
                return MetaData.ExifIfd.Entries[key];
            }
            else if (MetaData.GpsIfd != null && MetaData.GpsIfd.Entries.ContainsKey(key))
            {
                return MetaData.GpsIfd.Entries[key];
            }
            return null;
        }

        private Visibility _DetailInfoVisibility = Visibility.Visible;
        public Visibility DetailInfoVisibility
        {
            get { return _DetailInfoVisibility; }
            set
            {
                if (_DetailInfoVisibility != value)
                {
                    _DetailInfoVisibility = value;
                    OnPropertyChanged("DetailInfoVisibility");
                }
            }
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
