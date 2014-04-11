using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using WPPMM.Controls;
using WPPMM.Resources;
using WPPMM.Utils;

namespace WPPMM.Pages
{
    public partial class ApplicationSetting : PhoneApplicationPage
    {
        public ApplicationSetting()
        {
            InitializeComponent();

            SettingList.Children.Add(new CheckBoxSetting(AppResources.DisplayTakeImageButtonSetting, AppResources.Guide_DisplayTakeImageButtonSetting, Preference.display_take_image_button_key));
        }
    }
}