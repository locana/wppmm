#if DEBUG
using Kazyx.WPPMM.Utils;
using System.Collections.Generic;
#endif
using Microsoft.Phone.Controls;
using System.Windows.Controls;

namespace Kazyx.WPPMM.Pages
{
    public partial class LogViewerPage : PhoneApplicationPage
    {
        public LogViewerPage()
        {
            InitializeComponent();
#if DEBUG
            DebugUtil.Flush();
            files = new List<string>(DebugUtil.LogFiles());
            FileListBox.ItemsSource = files;
#endif
        }

#if DEBUG
        List<string> files = new List<string>();
#endif

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
#if DEBUG
            var box = sender as ListBox;
            var filepath = box.SelectedValue as string;
            ContentHeader.Text = filepath;
            LogContent.Text = DebugUtil.GetFile(filepath);
            LogContent.InvalidateArrange();
#endif
        }

        private void LogContent_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
#if DEBUG
            var textblock = sender as TextBlock;
            DebugUtil.ComposeDebugMail(textblock.Text);
#endif
        }
    }
}