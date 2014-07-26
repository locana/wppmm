using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Kazyx.WPMMM.Controls
{
    public partial class SettingSectionTitle : UserControl
    {
        String Title
        {
            get
            {
                return TitleTextBlock.Text;
            }
            set
            {
                TitleTextBlock.Text = value;
            }
        }

        public SettingSectionTitle(String SectionTitle)
        {
            InitializeComponent();
            TitleTextBlock.Text = SectionTitle;
        }
    }
}
