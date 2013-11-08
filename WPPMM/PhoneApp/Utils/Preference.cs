using System;
using System.IO.IsolatedStorage;

namespace WPPMM.Utils
{
    public class Preference
    {
        public const string postview_key = "transfer_postview";

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
    }
}
