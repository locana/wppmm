using Microsoft.Phone.Controls;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xml.Linq;
using Windows.ApplicationModel;
using WPPMM.Resources;

namespace WPPMM.Pages
{
    public partial class AboutPage : PhoneApplicationPage
    {
        private static string version = "";
        private static string license = "";
        private static string copyright = "";
        public AboutPage()
        {
            InitializeComponent();
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(version))
            {
                version = XDocument.Load("WMAppManifest.xml").Root.Element("App").Attribute("Version").Value;
            }
            VERSION_STR.Text = version;
            FormatRichText(Repository, AppResources.RepoURL);
            if (string.IsNullOrEmpty(copyright))
            {
                var attr = (AssemblyCopyrightAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyCopyrightAttribute));
                copyright = attr.Copyright;
            }
            COPYRIGHT.Text = copyright;
            LoadLicenseFile();
        }

        private async void LoadLicenseFile()
        {
            if (string.IsNullOrEmpty(license))
            {
                var installedFolder = Package.Current.InstalledLocation;
                var folder = await installedFolder.GetFolderAsync("Assets");
                var file = await folder.GetFileAsync("License.txt");
                var stream = await file.OpenReadAsync();
                var reader = new StreamReader(stream.AsStreamForRead());
                license = reader.ReadToEnd();
            }
            Dispatcher.BeginInvoke(() => { FormatRichText(Contents, license); });
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