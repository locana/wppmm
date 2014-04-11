using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using WPPMM.Utils;

namespace WPPMM.Controls
{
    public partial class CheckBoxSetting : UserControl
    {
        private string settingKey;

        public CheckBoxSetting(string title, string guide, string key)
        {   
            InitializeComponent();

            SettingName.Text = title;
            SettingGuide.Text = guide;
            settingKey = key;

            SettingCheckBox.Checked += SettingCheckBox_Checked;
            SettingCheckBox.Unchecked += SettingCheckBox_Unchecked;

            SettingCheckBox.IsChecked = Preference.GetPreference(settingKey);
        }

        void SettingCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Preference.SetPreference(settingKey, false);
        }

        void SettingCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Preference.SetPreference(settingKey, true);
        }
    }
}
