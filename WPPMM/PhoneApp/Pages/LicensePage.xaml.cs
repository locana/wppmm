using Microsoft.Phone.Controls;
using System;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Windows.ApplicationModel;

namespace WPPMM.Pages
{
    public partial class LicensePage : PhoneApplicationPage
    {
        public LicensePage()
        {
            InitializeComponent();
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadLicenseFile();
        }

        private async void LoadLicenseFile()
        {
            var installedFolder = Package.Current.InstalledLocation;
            var folder = await installedFolder.GetFolderAsync("Assets");
            var file = await folder.GetFileAsync("License.txt");
            var stream = await file.OpenReadAsync();
            var reader = new StreamReader(stream.AsStreamForRead());
            var text = reader.ReadToEnd();
            Dispatcher.BeginInvoke(() => { FormatRichText(Contents, text); });
        }

        private static void FormatRichText(Paragraph place, string text)
        {
            if (text != null && text.Length != 0)
            {
                char[] separators = { ' ', '\n', '\t', '　' };
                string[] words = text.Split(separators);
                foreach (var word in words)
                {
                    if (word.StartsWith("http://") || word.StartsWith("https://"))
                    {
                        place.Inlines.Add(GetAsLink(word));
                        place.Inlines.Add(" ");
                    }
                    else
                    {
                        place.Inlines.Add(word + " ");
                    }
                }
            }
        }

        private static Hyperlink GetAsLink(string word)
        {
            var hl = new Hyperlink
            {
                NavigateUri = new Uri(word),
                TargetName = "_blank",
                Foreground = new SolidColorBrush((Color)Application.Current.Resources["PhoneAccentColor"])
            };

            hl.Inlines.Add(word);

            return hl;
        }
    }
}