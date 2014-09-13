using Kazyx.WPPMM.DataModel;
using System;
using System.Windows.Controls;

namespace Kazyx.WPPMM.Controls
{
    public partial class CheckBoxSetting : UserControl
    {
        public CheckBoxSetting(AppSettingData<bool> data)
        {
            InitializeComponent();
            if (data == null)
            {
                throw new ArgumentNullException("AppSettingData must not be null");
            }
            this.DataContext = data;
        }
    }
}
