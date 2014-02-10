using System;
using System.Diagnostics;
using System.IO.IsolatedStorage;

namespace WPPMM.Utils
{
    public class Preference
    {
        public const string postview_key = "transfer_postview";
        public const string interval_enable_key = "interval_enable";
        public const string interval_time_key = "interval_time";

        public static bool IsPostviewTransferEnabled()
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if (settings.Contains(postview_key))
            {
                return (Boolean)settings[postview_key];
            }
            else
            {
                return true;
            }
        }

        public static void SetPostviewTransferEnabled(bool enable)
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if (settings.Contains(postview_key))
            {
                settings.Remove(postview_key);
            }
            settings.Add(postview_key, enable);
        }

        public static bool IsIntervalShootingEnabled()
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if (settings.Contains(interval_enable_key))
            {
                return (Boolean)settings[interval_enable_key];
            }
            else
            {
                return false;
            }
        }

        public static void SetIntervalShootingEnabled(bool enable)
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if (settings.Contains(interval_enable_key))
            {
                settings.Remove(interval_enable_key);
            }
            settings.Add(interval_enable_key, enable);
        }

        public static int IntervalTime()
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if (settings.Contains(interval_time_key))
            {
                return (int)settings[interval_time_key];
            }
            else
            {
                return 10;
            }
        }

        public static void SetIntervalTime(int time)
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if (settings.Contains(interval_time_key))
            {
                settings.Remove(interval_time_key);
            }
            settings.Add(interval_time_key, time);
        }
    }
}
