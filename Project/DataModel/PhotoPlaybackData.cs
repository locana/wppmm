using Kazyx.WPPMM.Resources;
using Kazyx.WPPMM.Utils;
using NtImageProcessor.MetaData.Structure;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Kazyx.WPPMM.DataModel
{
    public class PhotoPlaybackData : INotifyPropertyChanged
    {
        public PhotoPlaybackData() { }
        private ObservableCollection<EntryViewData> _EntryList = new ObservableCollection<EntryViewData>();
        public ObservableCollection<EntryViewData> EntryList
        {
            get { return _EntryList; }
            set
            {
                _EntryList = value;
                OnPropertyChanged("EntryList");
            }
        }

        private JpegMetaData _MetaData;
        public JpegMetaData MetaData
        {
            get { return _MetaData; }
            set
            {
                _MetaData = value;
                if (value == null)
                {
                    ShowInvalidData();
                }
                else
                {
                    UpdateEntryList(value);
                    if (EntryList.Count == 0)
                    {
                        ShowInvalidData();
                    }
                }
            }
        }



        uint[] GeneralMetaDataKeys = new uint[] 
        { 
            ExifKeys.Fnumber,
            ExifKeys.Iso,
            ExifKeys.DateTime,
        };

        void ShowInvalidData()
        {
            EntryList.Clear();
            EntryList.Add(new EntryViewData() { Name = "NO DATA", Value = "" });
        }

        private void UpdateEntryList(JpegMetaData metadata)
        {
            EntryList.Clear();

            var exposureModeEntry = FindFirstEntry(metadata, ExifKeys.ExposureProgram);
            if (exposureModeEntry != null)
            {
                EntryList.Add(new EntryViewData()
                {
                    Name = MetaDataValueConverter.MetaDataEntryName(ExifKeys.ExposureProgram),
                    Value = MetaDataValueConverter.ExposuteProgramName(exposureModeEntry.UIntValues[0]),
                });
            }

            var ssEntry = FindFirstEntry(metadata, ExifKeys.ExposureTime);
            if (ssEntry != null)
            {
                EntryList.Add(new EntryViewData()
                {
                    Name = MetaDataValueConverter.MetaDataEntryName(ExifKeys.ExposureTime),
                    Value = ssEntry.UFractionValues[0].Numerator + "/" + ssEntry.UFractionValues[0].Denominator + " sec.",
                });
            }

            var focalLengthEntry = FindFirstEntry(metadata, ExifKeys.FocalLength);
            if (focalLengthEntry != null)
            {
                EntryList.Add(new EntryViewData()
                {
                    Name = MetaDataValueConverter.MetaDataEntryName(ExifKeys.FocalLength),
                    Value = GetStringValue(metadata, ExifKeys.FocalLength) + "mm",
                });
            }

            foreach (uint key in GeneralMetaDataKeys)
            {
                if (FindFirstEntry(metadata, key) == null) { continue; }
                EntryList.Add(new EntryViewData()
                {
                    Name = MetaDataValueConverter.MetaDataEntryName(key),
                    Value = GetStringValue(metadata, key)
                });
            }

            var wbEntry = FindFirstEntry(metadata, ExifKeys.WhiteBalanceMode);
            var wbDetailEntry = FindFirstEntry(metadata, ExifKeys.WhiteBalanceDetailType);
            if (wbEntry != null && wbDetailEntry != null)
            {
                string value;
                if (wbEntry.UIntValues[0] == 0x0)
                {
                    value = AppResources.WB_Auto;
                }
                else
                {
                    value = MetaDataValueConverter.WhitebalanceName(wbDetailEntry.UIntValues[0]);
                }
                EntryList.Add(new EntryViewData() { Name = MetaDataValueConverter.MetaDataEntryName(ExifKeys.WhiteBalanceMode), Value = value });
            }

            var heightEntry = FindFirstEntry(metadata, ExifKeys.ImageHeight);
            var widthEntry = FindFirstEntry(metadata, ExifKeys.ImageWidth);
            if (heightEntry != null && widthEntry != null)
            {
                EntryList.Add(new EntryViewData()
                {
                    Name = AppResources.MetaDataName_ImageSize,
                    Value = widthEntry.UIntValues[0] + " x " + heightEntry.UIntValues[0],
                });
            }

            var makerEntry = FindFirstEntry(metadata, ExifKeys.CameraMaker);
            var modelEntry = FindFirstEntry(metadata, ExifKeys.CameraModel);
            if (makerEntry != null && modelEntry != null)
            {
                EntryList.Add(new EntryViewData()
                {
                    Name = MetaDataValueConverter.MetaDataEntryName(ExifKeys.CameraModel),
                    Value = makerEntry.StringValue + " " + modelEntry.StringValue,
                });
            }
        }

        string GetStringValue(JpegMetaData metadata, uint key)
        {
            var entry = FindFirstEntry(metadata, key);
            if (entry == null) { return "null"; }
            switch (entry.Type)
            {
                case Entry.EntryType.Ascii:
                    return entry.StringValue;
                case Entry.EntryType.Byte:
                    return entry.value.ToString();
                case Entry.EntryType.Long:
                case Entry.EntryType.Short:
                    return entry.UIntValues[0].ToString();
                case Entry.EntryType.SLong:
                case Entry.EntryType.SShort:
                    return entry.IntValues[0].ToString();
                case Entry.EntryType.Rational:
                case Entry.EntryType.SRational:
                    return entry.DoubleValues[0].ToString();
            }
            return "--";
        }

        Entry FindFirstEntry(JpegMetaData metadata, uint key)
        {
            if (metadata == null) { return null; }

            if (metadata.PrimaryIfd != null && metadata.PrimaryIfd.Entries.ContainsKey(key))
            {
                return metadata.PrimaryIfd.Entries[key];
            }
            else if (metadata.ExifIfd != null && metadata.ExifIfd.Entries.ContainsKey(key))
            {
                return metadata.ExifIfd.Entries[key];
            }
            else if (metadata.GpsIfd != null && metadata.GpsIfd.Entries.ContainsKey(key))
            {
                return metadata.GpsIfd.Entries[key];
            }
            return null;
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

    public class EntryViewData
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public static class ExifKeys
    {
        public const uint Fnumber = 0x829D;
        public const uint ExposureTime = 0x829A;
        public const uint Iso = 0x8827;
        public const uint FocalLength = 0x920A;
        public const uint CameraModel = 0x0110;
        public const uint CameraMaker = 0x010F;
        public const uint ImageWidth = 0xA002;
        public const uint ImageHeight = 0xA003;
        public const uint DateTime = 0x9003;
        public const uint ExposureProgram = 0x8822;
        public const uint WhiteBalanceMode = 0xA403;
        public const uint WhiteBalanceDetailType = 0x9208;
    }
}
