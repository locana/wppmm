﻿using Microsoft.Phone.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

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
            manager.MethodTypesUpdateNotifer += () => { Initialize(); };
        }

        public bool IsShowing()
        {
            return panel.Visibility == Visibility.Visible;
        }

        public int ItemCount
        {
            get { return panel.Children.Count - 1; }
        }

        public void Show()
        {
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

            panel.Visibility = Visibility.Visible;
        }

        private void Initialize()
        {
            panel.Children.Clear();

            var title = new TextBlock
            {
                Text = Resources.AppResources.ControlPanel,
                HorizontalAlignment = HorizontalAlignment.Left,
                Style = Application.Current.Resources["PhoneTextLargeStyle"] as Style,
                Margin = new Thickness(-5, 15, 0, -10)
            };
            panel.Children.Add(title);

            if (status.IsSupported("setShootMode"))
            {
                panel.Children.Add(CreatePanel("ShootMode", Resources.AppResources.ShootMode,
                     (sender, arg) =>
                     {
                         if (status.ShootModeInfo == null || status.ShootModeInfo.candidates == null)
                             return;
                         var selected = (sender as ListPicker).SelectedIndex;
                         manager.SetShootMode(status.ShootModeInfo.candidates[selected]);
                     }));
            }
            if (status.IsSupported("setSelfTimer"))
            {
                panel.Children.Add(CreatePanel("SelfTimer", Resources.AppResources.SelfTimer,
                     (sender, arg) =>
                     {
                         if (status.SelfTimerInfo == null || status.SelfTimerInfo.candidates == null)
                             return;
                         var selected = (sender as ListPicker).SelectedIndex;
                         manager.SetSelfTimer(status.SelfTimerInfo.candidates[selected]);
                     }));
            }
            if (status.IsSupported("setPostviewImageSize"))
            {
                panel.Children.Add(CreatePanel("PostviewSize", Resources.AppResources.Setting_PostViewImageSize,
                    (sender, arg) =>
                    {
                        if (status.PostviewSizeInfo == null || status.PostviewSizeInfo.candidates == null)
                            return;
                        var selected = (sender as ListPicker).SelectedIndex;
                        manager.SetPostViewImageSize(status.PostviewSizeInfo.candidates[selected]);
                    }));
            }
            panel.Width = double.NaN;
        }

        public void Hide()
        {
            panel.Visibility = Visibility.Collapsed;
        }

        private StackPanel CreatePanel(string id, string title, SelectionChangedEventHandler handler)
        {
            var child = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            child.Children.Add(
                new TextBlock
                {
                    Text = title,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Style = Application.Current.Resources["PhoneTextStyle"] as Style,
                    Margin = new Thickness(5, 20, 0, 0)
                }
            );

            var statusbind = new Binding()
            {
                Source = status,
                Path = new PropertyPath("CpIsAvailable" + id),
                Mode = BindingMode.OneWay
            };
            var selectedbind = new Binding()
            {
                Source = status,
                Path = new PropertyPath("CpSelectedIndex" + id),
                Mode = BindingMode.TwoWay
            };
            var candidatesbind = new Binding()
            {
                Source = status,
                Path = new PropertyPath("CpCandidates" + id),
                Mode = BindingMode.OneWay
            };
            var picker = new ListPicker
            {
                SelectionMode = SelectionMode.Single,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(10, -5, 10, 0)
            };
            picker.SetBinding(ListPicker.IsEnabledProperty, statusbind);
            picker.SetBinding(ListPicker.ItemsSourceProperty, candidatesbind);
            picker.SetBinding(ListPicker.SelectedIndexProperty, selectedbind);
            picker.SelectionChanged += handler;

            child.Children.Add(picker);
            //child.Width = double.NaN;
            child.Width = 240;
            return child;
        }
    }
}
