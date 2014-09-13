using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Kazyx.WPPMM.DataModel;

namespace Kazyx.WPMMM.Controls
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
