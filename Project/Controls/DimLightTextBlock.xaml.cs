using System;
using System.Windows;
using System.Windows.Controls;

namespace Kazyx.WPMMM.Controls
{
    public partial class DimLightTextBlock : UserControl
    {
        public event EventHandler AnimationCompleted;

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            "Title",
            typeof(string),
            typeof(DimLightTextBlock),
            new PropertyMetadata(
                new PropertyChangedCallback(OnTitleChanged)
                )
            );

        private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var dim = d as DimLightTextBlock;
            var title = e.NewValue as string;
            dim.MainText.Text = title;
        }

        public string Title
        {
            set { SetValue(TitleProperty, value); }
            get { return GetValue(TitleProperty) as string; }
        }

        protected void OnFinished()
        {
            if (AnimationCompleted != null)
            {
                AnimationCompleted(this, new EventArgs());
            }
        }

        public DimLightTextBlock()
        {
            InitializeComponent();
        }

        private void Storyboard_Completed(object sender, EventArgs e)
        {
            OnFinished();
        }
    }
}
