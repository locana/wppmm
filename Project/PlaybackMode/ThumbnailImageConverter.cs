using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Kazyx.WPMMM.PlaybackMode
{
    public class ThumbnailImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var path = value as string;
                if (string.IsNullOrEmpty(path))
                {
                    return null;
                }

                var size = int.Parse(parameter as string);
                var bmp = new BitmapImage();
                bmp.DecodePixelWidth = size;
                bmp.DecodePixelHeight = size;
                bmp.CreateOptions = BitmapCreateOptions.DelayCreation;

                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (var stream = store.OpenFile(path, FileMode.Open))
                    {
                        bmp.SetSource(stream);
                    }
                }

                return bmp;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.StackTrace);
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack is not implemented.");
        }
    }
}
