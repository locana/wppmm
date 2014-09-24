using Kazyx.WPPMM.Utils;
using System;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Kazyx.WPPMM.PlaybackMode
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

                var bmp = new BitmapImage();
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
                DebugUtil.Log(e.StackTrace);
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ConvertBack is not implemented.");
        }
    }
}
