using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using WPPMM.Resources;
using WPPMM.Controls;

namespace WPPMM.Pages
{
    public partial class AppSettingPage : PhoneApplicationPage
    {
        public AppSettingPage()
        {
            InitializeComponent();

            SettingList.Children.Add(new CheckBoxSetting(AppResources.DisplayTakeImageButtonSetting, AppResources.Guide_DisplayTakeImageButtonSetting, CheckBoxSetting.SettingType.displayShootbutton));
        }
    }
}