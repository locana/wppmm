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
using WPPMM.DataModel;

namespace WPPMM.Controls
{
    public partial class CheckBoxSetting : UserControl
    {
        private SettingType type;

        public enum SettingType
        {
            displayShootbutton,
        };

        public CheckBoxSetting(string title, string guide, SettingType setting)
        {   
            InitializeComponent();

            SettingName.Text = title;
            SettingGuide.Text = guide;
            type = setting;

            SettingCheckBox.Checked += SettingCheckBox_Checked;
            SettingCheckBox.Unchecked += SettingCheckBox_Unchecked;

            if (ApplicationSettings.GetInstance().IsShootButtonDisplayed)
            {
                SettingCheckBox.IsChecked = true;
            }
            else
            {
                SettingCheckBox.IsChecked = false;
            }
        }

        void SettingCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            switch (type)
            {
                case SettingType.displayShootbutton:
                    ApplicationSettings.GetInstance().IsShootButtonDisplayed = false;
                    break;
            }
        }

        void SettingCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            switch (type)
            {
                case SettingType.displayShootbutton:
                    ApplicationSettings.GetInstance().IsShootButtonDisplayed = true;
                    break;
            }
        }
    }
}
