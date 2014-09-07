using Kazyx.WPPMM.DataModel;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Kazyx.WPMMM.Controls
{
    public partial class SettingSection : UserControl
    {
        string Title
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

        public SettingSection(string SectionTitle)
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
