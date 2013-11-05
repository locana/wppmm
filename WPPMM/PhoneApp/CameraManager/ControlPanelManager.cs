using Microsoft.Phone.Controls;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using WPPMM.RemoteApi;
using WPPMM.Utils;

namespace WPPMM.CameraManager
{
    public class ControlPanelManager
    {
        private CameraManager manager;
        private Status status;
        private StackPanel panel;

        public ControlPanelManager(StackPanel panel)
        {
            this.manager = CameraManager.GetInstance();
            this.status = manager.cameraStatus;
            this.panel = panel;
        }

        public bool IsShowing()
        {
            return panel.Visibility == Visibility.Visible;
        }

        public void Show()
        {
            panel.Children.Clear();
            panel.Children.Add(new TextBlock
            {
                Text = Resources.AppResources.ControlPanel,
                HorizontalAlignment = HorizontalAlignment.Left,
                Style = Application.Current.Resources["PhoneTextLargeStyle"] as Style,
                Margin = new Thickness(-5, 15, 0, -10)
            });

            if (status.MethodTypes.Contains("setShootMode"))
            {
                panel.Children.Add(CreatePanel(Resources.AppResources.ShootMode, status.IsAvailable("setShootMode"),
                    SettingsValueConverter.FromShootMode(status.ShootModeInfo), (sender, arg) =>
                    {
                        var selected = (sender as ListPicker).SelectedIndex;
                        manager.SetShootMode(status.ShootModeInfo.candidates[selected]);
                    }));
            }
            if (status.MethodTypes.Contains("setSelfTimer"))
            {
                panel.Children.Add(CreatePanel(Resources.AppResources.SelfTimer, status.IsAvailable("setSelfTimer"),
                    SettingsValueConverter.FromSelfTimer(status.SelfTimerInfo), (sender, arg) =>
                {
                    var selected = (sender as ListPicker).SelectedIndex;
                    manager.SetSelfTimer(status.SelfTimerInfo.candidates[selected]);
                }));
            }
            if (status.MethodTypes.Contains("setPostviewImageSize"))
            {
                panel.Children.Add(CreatePanel(Resources.AppResources.Setting_PostViewImageSize, status.IsAvailable("setPostviewImageSize"),
                    SettingsValueConverter.FromPostViewSize(status.PostviewSizeInfo), (sender, arg) =>
                {
                    var selected = (sender as ListPicker).SelectedIndex;
                    manager.SetPostViewImageSize(status.PostviewSizeInfo.candidates[selected]);
                }));
            }

            // Test code
            /*
            var info = new BasicInfo<string>
            {
                current = "test2",
                candidates = new string[] { "test1", "test2", "test3" }
            };
            var info2 = new BasicInfo<string>
            {
                current = "testtesttest2",
                candidates = new string[] { "test1", "testtesttest2", "aaaaaaaaaaaaaaaaaaaaaatest3" }
            };
            panel.Children.Add(CreatePanel("Test", info, (sender, arg) => { }));
            panel.Children.Add(CreatePanel("Test", info, (sender, arg) => { }));
            panel.Children.Add(CreatePanel("Test222222222222", info2, (sender, arg) => { }));
            panel.Children.Add(CreatePanel("Test", info, (sender, arg) => { }));
            panel.Children.Add(CreatePanel("Test", info, (sender, arg) => { }));
            panel.Children.Add(CreatePanel("Test", info, (sender, arg) => { }));
            panel.Children.Add(CreatePanel("Test", info, (sender, arg) => { }));
            panel.Children.Add(CreatePanel("Test", info, (sender, arg) => { }));
            panel.Children.Add(CreatePanel("Test", info, (sender, arg) => { }));
            */
            // end test code

            panel.Width = double.NaN;

            panel.Visibility = Visibility.Visible;
        }

        public void Hide()
        {
            panel.Visibility = Visibility.Collapsed;
        }

        private static StackPanel CreatePanel(string title, bool isEnabled, BasicInfo<string> info, SelectionChangedEventHandler handler)
        {
            var child = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            child.Children.Add(CreateTitle(title));
            var picker = CreateListPicker(info);
            picker.IsEnabled = isEnabled;
            if (isEnabled)
            {
                picker.SelectionChanged += handler;
            }
            child.Children.Add(picker);
            //child.Width = double.NaN;
            child.Width = 240;
            return child;
        }

        private static TextBlock CreateTitle(string title)
        {
            return new TextBlock
            {
                Text = title,
                HorizontalAlignment = HorizontalAlignment.Left,
                Style = Application.Current.Resources["PhoneTextStyle"] as Style,
                Margin = new Thickness(5, 20, 0, 0)
            };
        }

        private static ListPicker CreateListPicker(BasicInfo<string> info)
        {
            var currentindex = 0;
            for (int i = 0; i < info.candidates.Length; i++)
            {
                if (info.candidates[i] == info.current)
                {
                    currentindex = i;
                    break;
                }
            }
            return new ListPicker
            {
                ItemsSource = info.candidates,
                SelectionMode = SelectionMode.Single,
                SelectedIndex = currentindex,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(10, -5, 10, 0)
            };
        }
    }
}
