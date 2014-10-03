using System.IO.IsolatedStorage;

namespace Kazyx.WPPMM.Utils
{
    public class Preference
    {
        public const string postview_key = "transfer_postview";
        public const string interval_enable_key = "interval_enable";
        public const string interval_time_key = "interval_time";
        public const string display_take_image_button_key = "display_take_image_button";
        public const string display_histogram_key = "display_histogram";
        public const string add_geotag = "add_geotag";
        public const string fraiming_grids = "fraiming_grids";
        public const string framing_grids_color = "framing_grids_color";
        public const string fibonacci_origin = "fibonacci_origin";

        public static bool IsPostviewTransferEnabled()
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if (settings.Contains(postview_key))
            {
                return (bool)settings[postview_key];
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
                return (bool)settings[interval_enable_key];
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

        public static bool IsShootButtonDisplayed()
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if (settings.Contains(display_take_image_button_key))
            {
                return (bool)settings[display_take_image_button_key];
            }
            else
            {
                return true;
            }
        }

        public static void SetShootButtonDisplayed(bool enable)
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if (settings.Contains(display_take_image_button_key))
            {
                settings.Remove(display_take_image_button_key);
            }
            settings.Add(display_take_image_button_key, enable);
        }

        public static bool IsHistogramDisplayed()
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if (settings.Contains(display_histogram_key))
            {
                return (bool)settings[display_histogram_key];
            }
            return true;
        }

        public static void SetHistogramDisplayed(bool enable)
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if (settings.Contains(display_histogram_key))
            {
                settings.Remove(display_histogram_key);
            }
            settings.Add(display_histogram_key, enable);
        }

        public static bool GeotagEnabled()
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if (settings.Contains(add_geotag))
            {
                return (bool)settings[add_geotag];
            }
            return false;
        }

        public static void SetGeotagEnabled(bool enable)
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if (settings.Contains(add_geotag))
            {
                settings.Remove(add_geotag);
            }
            settings.Add(add_geotag, enable);
        }

        public static string FramingGridsType()
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if (settings.Contains(fraiming_grids))
            {
                return (string)settings[fraiming_grids];
            }
            return null;
        }

        public static void SetFramingGridsType(string type)
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if (settings.Contains(fraiming_grids))
            {
                settings.Remove(fraiming_grids);
            }
            settings.Add(fraiming_grids, type);
        }

        public static string FramingGridsColor()
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if (settings.Contains(framing_grids_color))
            {
                return (string)settings[framing_grids_color];
            }
            return null;
        }

        public static void SetFramingGridsColor(string type)
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if (settings.Contains(framing_grids_color))
            {
                settings.Remove(framing_grids_color);
            }
            settings.Add(framing_grids_color, type);
        }

        public static string FibonacciOrigin()
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if (settings.Contains(fibonacci_origin))
            {
                return (string)settings[fibonacci_origin];
            }
            DebugUtil.Log("Fibonacci origin returns null.");
            return null;
        }

        public static void SetFibonacciOrigin(string type)
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if (settings.Contains(fibonacci_origin))
            {
                settings.Remove(fibonacci_origin);
            }
            settings.Add(fibonacci_origin, type);
        }

        public static void SetPreference(string key, bool enable)
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if (settings.Contains(key))
            {
                settings.Remove(key);
            }
            settings.Add(key, enable);
        }

        public static bool GetPreference(string key)
        {
            var settings = IsolatedStorageSettings.ApplicationSettings;
            if (settings.Contains(key))
            {
                return (bool)settings[key];
            }
            else
            {
                return false;
            }
        }



    }
}
