using Kazyx.WPPMM.DataModel;
using System;
using System.Windows.Controls;

namespace Kazyx.WPPMM.Controls
{
    public partial class ListPickerSetting : UserControl
    {
        public ListPickerSetting(AppSettingData<int> data)
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
