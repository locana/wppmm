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
            Debug.WriteLine("interval setting saved: " + enable);
            settings.Add(interval_enable_key, enable);
        }
    }
}
