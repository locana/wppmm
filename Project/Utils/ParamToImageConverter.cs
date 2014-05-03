using Kazyx.RemoteApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Kazyx.WPMMM.Utils
{
    public class ParamToImageConverter
    {
        private static readonly BitmapImage ExModeImage_IA = new BitmapImage(new Uri("/Assets/Screen/ExposureMode_iA.png", UriKind.Relative));
        private static readonly BitmapImage ExModeImage_IAPlus = new BitmapImage(new Uri("/Assets/Screen/ExposureMode_iAPlus.png", UriKind.Relative));
        private static readonly BitmapImage ExModeImage_A = new BitmapImage(new Uri("/Assets/Screen/ExposureMode_A.png", UriKind.Relative));
        private static readonly BitmapImage ExModeImage_S = new BitmapImage(new Uri("/Assets/Screen/ExposureMode_S.png", UriKind.Relative));
        private static readonly BitmapImage ExModeImage_P = new BitmapImage(new Uri("/Assets/Screen/ExposureMode_P.png", UriKind.Relative));

        public static BitmapImage ConvertToExposureModeImage(string param)
        {
            if (param == null)
            {
                return null;
            }
            switch (param)
            {
                case ExposureMode.Aperture:
                    return ExModeImage_A;
                case ExposureMode.SS:
                    return ExModeImage_S;
                case ExposureMode.Program:
                    return ExModeImage_P;
                case ExposureMode.Intelligent:
                    return ExModeImage_IA;
                case ExposureMode.Superior:
                    return ExModeImage_IAPlus;
                default:
                    return null;
            }
        }
    }
}
