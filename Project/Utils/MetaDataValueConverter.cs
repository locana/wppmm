using Kazyx.WPPMM.DataModel;
using Kazyx.WPPMM.Resources;
using System.Collections.Generic;

namespace Kazyx.WPPMM.Utils
{
    public class MetaDataValueConverter
    {

        private static Dictionary<uint, string> MetaDataNames = new Dictionary<uint, string>(){
            {ExifKeys.FocalLength, AppResources.MetaDataName_FocalLength},
            {ExifKeys.Fnumber, AppResources.MetaDataName_Fnumber},
            {ExifKeys.ExposureTime, AppResources.MetaDataName_ExposureTime},
            {ExifKeys.Iso, AppResources.MetaDataName_Iso},
            {ExifKeys.CameraModel, AppResources.MetaDataName_CameraModel},
            {ExifKeys.DateTime, AppResources.MetaDataName_DateTime},
            {ExifKeys.ExposureProgram, AppResources.MetaDataName_ExposureProgram},
            {ExifKeys.WhiteBalanceMode, AppResources.WhiteBalance},
            {ExifKeys.LensModel, AppResources.MetaDataName_LensModel},
            {ExifKeys.ExposureCompensation, AppResources.MetaDataName_ExposureCompensation},
            {ExifKeys.Flash, AppResources.MetaDataName_Flash},
            {ExifKeys.MeteringMode, AppResources.MetaDataName_MeteringMode},
        };

        public static string MetaDataEntryName(uint key)
        {
            if (MetaDataNames.ContainsKey(key))
            {
                return MetaDataNames[key];
            }
            return key.ToString("X4");
        }

        private static Dictionary<uint, string> ExposureProgramNames = new Dictionary<uint, string>()
        {
            {0x0, AppResources.ExifExposureProgram_Unknown},
            {0x1, AppResources.ExifExposureProgram_M},
            {0x2, AppResources.ExifExposureProgram_P},
            {0x3, AppResources.ExifExposureProgram_A},
            {0x4, AppResources.ExifExposureProgram_S},
            {0x5, AppResources.ExifExposureProgram_SlowSpeed},
            {0x6, AppResources.ExifExposureProgram_HighSpeed},
            {0x7, AppResources.ExifExposureProgram_Portrait},
            {0x8, AppResources.ExifExposureProgram_Landscape},
            {0x9, AppResources.ExifExposureProgram_Bulb},
        };

        public static string ExposuteProgramName(uint key)
        {
            if (ExposureProgramNames.ContainsKey(key))
            {
                return ExposureProgramNames[key];
            }
            return key.ToString("X2");
        }

        private static Dictionary<uint, string> WhitebalanceNames = new Dictionary<uint, string>()
        {
            { 0, AppResources.ExifWBValue_Unknown},
            { 1, AppResources.ExifWBValue_Daylight},
            { 2, AppResources.ExifWBValue_Fluorescent},
            { 3, AppResources.ExifWBValue_TungstenIncandescent},
            { 4, AppResources.ExifWBValue_Flash},
            { 9, AppResources.ExifWBValue_FineWeather},
            { 10, AppResources.ExifWBValue_Cloudy},
            { 11, AppResources.ExifWBValue_Shade},
            { 12, AppResources.ExifWBValue_DaylightFluorescent},
            { 13, AppResources.ExifWBValue_DayWhiteFluorescent},
            { 14, AppResources.ExifWBValue_CoolWhiteFluorescent},
            { 15, AppResources.ExifWBValue_WhiteFluorescent},
            { 16, AppResources.ExifWBValue_WarmWhiteFluorescent},
            { 17, AppResources.ExifWBValue_StandardLightA},
            { 18, AppResources.ExifWBValue_StandardLightB},
            { 19, AppResources.ExifWBValue_StandardLightC},
            { 20, AppResources.ExifWBValue_D55},
            { 21, AppResources.ExifWBValue_D65},
            { 22, AppResources.ExifWBValue_D75},
            { 23, AppResources.ExifWBValue_D50},
            { 24, AppResources.ExifWBValue_ISOStudioTungsten},
            { 255, AppResources.ExifWBValue_Other},
        };

        public static string WhitebalanceName(uint value)
        {
            if (WhitebalanceNames.ContainsKey(value))
            {
                return WhitebalanceNames[value];
            }
            return value.ToString();
        }

        private static Dictionary<uint, string> MeteringModeNames = new Dictionary<uint, string>(){
            {0, AppResources.Exif_MeteringMode_Unknown},
            {1, AppResources.Exif_MeteringMode_Average},
            {2, AppResources.Exif_MeteringMode_CenterWeightedAverage},
            {3, AppResources.Exif_MeteringMode_Spot},
            {4, AppResources.Exif_MeteringMode_MultiSpot},
            {5, AppResources.Exif_MeteringMode_MultiSegment},
            {6, AppResources.Exif_MeteringMode_Partial},
            {255, AppResources.Exif_MeteringMode_Other},
        };

        public static string MeteringModeName(uint value)
        {
            if (MeteringModeNames.ContainsKey(value))
            {
                return MeteringModeNames[value];
            }
            return AppResources.Exif_MeteringMode_Unknown;
        }

        public static string FlashNames(uint value)
        {
            switch (value)
            {
                case 0x0:
                    return AppResources.Exif_Flash_NoFlash;
                case 0x01:
                case 0x05:
                case 0x07:
                    return AppResources.Exif_Flash_Fired;
                case 0x08:
                    return AppResources.Exif_Flash_On_NotFired;
                case 0x09:
                case 0x0d:
                case 0x0f:
                    return AppResources.Exif_Flash_On_Fired;
                case 0x10:
                case 0x14:
                    return AppResources.Exif_Flash_Off_NotFired;
                case 0x18:
                    return AppResources.Exif_Flash_Auto_NotFired;
                case 0x19:
                case 0x1d:
                case 0x1f:
                    return AppResources.Exif_Flash_Auto_Fired;
                case 0x20:
                    return AppResources.Exif_Flash_NoFlashFunction;
                case 0x30:
                    return AppResources.Exif_Flash_NoFlashFunction;
                case 0x41:
                case 0x45:
                case 0x47:
                    return AppResources.Exif_Flash_Fired_RedEyeReduction;
                case 0x49:
                case 0x4d:
                case 0x4f:
                    return AppResources.Exif_Flash_On_Fired_RedEyeReduction;
                case 0x50:
                    return AppResources.Exif_Flash_Off_NotFired_RedEyeReduction;
                case 0x58:
                    return AppResources.Exif_Flash_Auto_NotFired_RedEyeReduction;
                case 0x59:
                case 0x5d:
                case 0x5f:
                    return AppResources.Exif_Flash_Auto_Fired_RedEyeReduction;
                default:
                    return AppResources.Exif_Flash_Unknown;
            }
        }

        public static string EV(double value)
        {
            if (value > 0)
            {
                return "+" + value + "EV";
            }
            return value + "EV";
        }

        public static string ShutterSpeed(uint numerator, uint denominator)
        {
            if (numerator == 1)
            {
                return numerator + "/" + denominator + AppResources.Seconds;
            }
            else if (denominator == 1)
            {
                return numerator + AppResources.Seconds;
            }

            // difficult cases,,
            if (numerator > denominator)
            {
                // longer than 1 sec.
                double val = numerator / denominator;
                return val.ToString() + AppResources.Seconds;
            }

            // reduction forcibly
            int newDenominator = (int)((double)denominator / (double)numerator);
            return "1/" + newDenominator + AppResources.Seconds;
        }
    }
}
