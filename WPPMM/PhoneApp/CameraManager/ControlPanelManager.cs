﻿using Microsoft.Phone.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WPPMM.DataModel;

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
                panel.Children.Add(CreateStatusPanel("ShootMode", Resources.AppResources.ShootMode,
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
                panel.Children.Add(CreateStatusPanel("SelfTimer", Resources.AppResources.SelfTimer,
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
                panel.Children.Add(CreateStatusPanel("PostviewSize", Resources.AppResources.Setting_PostViewImageSize,
                    (sender, arg) =>
                    {
                        if (status.PostviewSizeInfo == null || status.PostviewSizeInfo.candidates == null)
                            return;
                        var selected = (sender as ListPicker).SelectedIndex;
                        manager.SetPostViewImageSize(status.PostviewSizeInfo.candidates[selected]);
                    }));
            }

            panel.Children.Add(CreatePostviewSettingPanel(Resources.AppResources.PostviewTransferSetting,
                (sender, arg) =>
                {
                    var selected = (sender as ListPicker).SelectedIndex;
                    ApplicationSettings.GetInstance().IsPostviewTransferEnabled = (selected == 0);
                }));

            panel.Width = double.NaN;
        }

        public void Hide()
        {
            panel.Visibility = Visibility.Collapsed;
        }

        private StackPanel CreateStatusPanel(string id, string title, SelectionChangedEventHandler handler)
        {
            var child = CreatePanel(title);

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
            return child;
        }

        private StackPanel CreatePostviewSettingPanel(string title, SelectionChangedEventHandler handler)
        {
            var child = CreatePanel(title);

            var selectedbind = new Binding()
            {
                Source = ApplicationSettings.GetInstance(),
                Path = new PropertyPath("SelectedIndexPostviewTransferEnabled"),
                Mode = BindingMode.OneWay
            };

            var picker = new ListPicker
            {
                SelectionMode = SelectionMode.Single,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(10, -5, 10, 0)
            };
            picker.ItemsSource = ApplicationSettings.GetInstance().CandidatesPostviewTransferEnabled;
            picker.SetBinding(ListPicker.SelectedIndexProperty, selectedbind);
            picker.SelectionChanged += handler;

            child.Children.Add(picker);
            return child;
        }

        private StackPanel CreatePanel(string title)
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
                    Margin = new Thickness(5, 20, 0, 0),
                    Width = 240
                }
            );
            return child;
        }
    }
}
