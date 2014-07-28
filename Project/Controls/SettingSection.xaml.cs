using Kazyx.WPPMM.DataModel;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Kazyx.WPMMM.Controls
{
    public partial class SettingSection : UserControl
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

        public SettingSection(String SectionTitle)
        {
            InitializeComponent();
            TitleTextBlock.Text = SectionTitle;
        }

        public void Add(UIElement child)
        {
            SettingItems.Children.Add(child);
        }
    }
}
