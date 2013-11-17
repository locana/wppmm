﻿using Microsoft.Phone.Controls;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WPPMM.DataModel;


namespace WPPMM.CameraManager 
{
    public class ControlPanelManager
    {
        private CameraManager manager;
        private StackPanel panel;
        private CameraStatus status;
        private ControlPanelViewData data;

        public Action<bool> SetPivotIsLocked
        {
            get;
            set;
        }

        public ControlPanelManager(StackPanel panel)
        {
            this.manager = CameraManager.GetInstance();
            this.status = manager.cameraStatus;
            this.data = new ControlPanelViewData(status);
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
            SetPivotIsLocked(true);
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

            panel.Children.Add(CreatePostviewSettingPanel());

            if (status.IsSupported("actTakePicture"))
            {
                panel.Children.Add(CreateIntervalEnableSettingPanel());
                panel.Children.Add(CreateIntervalTimeSliderPanel());
            }

            Debug.WriteLine("panels has set!");

            panel.Width = double.NaN;
        }

        public void Hide()
        {
            panel.Visibility = Visibility.Collapsed;
            SetPivotIsLocked(false);
        }

        private StackPanel CreateStatusPanel(string id, string title, SelectionChangedEventHandler handler)
        {
            var child = CreatePanel(title);

            var statusbind = new Binding()
            {
                Source = data,
                Path = new PropertyPath("CpIsAvailable" + id),
                Mode = BindingMode.OneWay
            };
            var selectedbind = new Binding()
            {
                Source = data,
                Path = new PropertyPath("CpSelectedIndex" + id),
                Mode = BindingMode.TwoWay
            };
            var candidatesbind = new Binding()
            {
                Source = data,
                Path = new PropertyPath("CpCandidates" + id),
                Mode = BindingMode.OneWay
            };

            var picker = CreatePicker();
            picker.SetBinding(ListPicker.IsEnabledProperty, statusbind);
            picker.SetBinding(ListPicker.ItemsSourceProperty, candidatesbind);
            picker.SetBinding(ListPicker.SelectedIndexProperty, selectedbind);
            picker.SelectionChanged += handler;

            child.Children.Add(picker);
            return child;
        }

        private StackPanel CreatePostviewSettingPanel()
        {
            var child = CreatePanel(Resources.AppResources.PostviewTransferSetting);

            var toggle = CreateToggle();
            var checkbind = new Binding()
            {
                Source = ApplicationSettings.GetInstance(),
                Path = new PropertyPath("IsPostviewTransferEnabled"),
                Mode = BindingMode.TwoWay
            };
            toggle.SetBinding(ToggleSwitch.IsCheckedProperty, checkbind);

            child.Children.Add(toggle);
            return child;
        }

        private StackPanel CreateIntervalEnableSettingPanel()
        {
            var child = CreatePanel(Resources.AppResources.IntervalSetting);

            var toggle = CreateToggle();
            var checkbind = new Binding()
            {
                Source = ApplicationSettings.GetInstance(),
                Path = new PropertyPath("IsIntervalShootingEnabled"),
                Mode = BindingMode.TwoWay
            };
            toggle.SetBinding(ToggleSwitch.IsCheckedProperty, checkbind);

            child.Children.Add(toggle);
            return child;
        }


        private StackPanel CreateIntervalTimeSliderPanel()
        {
            var child = CreatePanel(Resources.AppResources.IntervalTime);
            Debug.WriteLine("create panel: " + Resources.AppResources.IntervalTime);

            var slider = CreateSlider(5, 30);
            slider.Value = ApplicationSettings.GetInstance().IntervalTime;

            slider.ValueChanged += (sender, e) =>
            {
                ApplicationSettings.GetInstance().IntervalTime = (int)e.NewValue;
            };

            var hPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center
            };

            var selectedbind = new Binding()
            {
                Source = ApplicationSettings.GetInstance(),
                Path = new PropertyPath("IntervalTime"),
                Mode = BindingMode.TwoWay
            };
            var indicator = new TextBlock
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = System.Windows.VerticalAlignment.Top,
                    Style = Application.Current.Resources["PhoneTextNormalStyle"] as Style,
                    Margin = new Thickness(10, 15, 0, 0),
                    MinWidth = 25
                };
            indicator.SetBinding(TextBlock.TextProperty, selectedbind);
            slider.SetBinding(Slider.ValueProperty, selectedbind);

            hPanel.Children.Add(indicator);
            hPanel.Children.Add(slider);

            child.Children.Add(hPanel);
            return child;
        }

        private static ListPicker CreatePicker()
        {
            return new ListPicker
            {
                SelectionMode = SelectionMode.Single,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(10, -5, 10, 0)
            };
        }

        private static ToggleSwitch CreateToggle()
        {
            return new ToggleSwitch
            {
                Margin = new Thickness(10, -5, 10, -10)
            };
        }

        private static Slider CreateSlider(int min, int max)
        {
            return new Slider
            {
                Maximum = max,
                Minimum = min,
                Margin = new Thickness(5, 0, 10, 0),
                MinWidth = 185,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top
            };
        }

        private static StackPanel CreatePanel(string title)
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
