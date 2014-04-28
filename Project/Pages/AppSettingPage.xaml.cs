using Kazyx.WPMMM.Resources;
using Kazyx.WPPMM.Controls;
using Microsoft.Phone.Controls;

namespace Kazyx.WPPMM.Pages
{
    public partial class AppSettingPage : PhoneApplicationPage
    {
        public AppSettingPage()
        {
            InitializeComponent();

            SettingList.Children.Add(new CheckBoxSetting(AppResources.DisplayTakeImageButtonSetting, AppResources.Guide_DisplayTakeImageButtonSetting, CheckBoxSetting.SettingType.displayShootbutton));
            SettingList.Children.Add(new CheckBoxSetting(AppResources.PostviewTransferSetting, CheckBoxSetting.SettingType.postviewImageTransfer));
        }
    }
}