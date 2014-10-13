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

    }
}
