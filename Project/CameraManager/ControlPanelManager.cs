using Kazyx.RemoteApi;
using Kazyx.WPMMM.Resources;
using Kazyx.WPPMM.DataModel;
using Kazyx.WPPMM.Utils;
using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;


namespace Kazyx.WPPMM.CameraManager
{
    public class ControlPanelManager
    {
        private CameraManager manager;
        private StackPanel panel;
        private CameraStatus status;
        private ControlPanelViewData data;

        private Dictionary<string, StackPanel> Panels = new Dictionary<string, StackPanel>();

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

            Panels.Add("ShootMode", CreateStatusPanel("ShootMode", AppResources.ShootMode, OnShootModeChanged));
            Panels.Add("SelfTimer", CreateStatusPanel("SelfTimer", AppResources.SelfTimer, OnSelfTimerChanged));
            Panels.Add("PostViewSize", CreateStatusPanel("PostviewSize", AppResources.Setting_PostViewImageSize, OnPostViewSizeChanged));
            Panels.Add("IntervalSwitch", CreateIntervalEnableSettingPanel());
            Panels.Add("IntervalValue", CreateIntervalTimeSliderPanel());
            Panels.Add("ExposureMode", CreateStatusPanel("ExposureMode", AppResources.ExposureMode, OnExposureModeChanged));
            Panels.Add("ExposureCompensation", CreateExposureCompensationSliderPanel());

            manager.MethodTypesUpdateNotifer += () => { Initialize(); };
            manager.VersionDetected += (version) => { if (version.IsLiberated) { DetectLiberated(); } };
        }

        public void ReplacePanel(StackPanel panel)
        {
            this.panel = panel;
        }

        public void Dispose()
        {
            manager = null;
            panel = null;
            status = null;
            data = null;
            Panels = null;
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

        private void DetectLiberated()
        {
            Panels["ExposureMode"].Visibility = Visibility.Visible;
        }

        private void Initialize()
        {
            panel.Children.Clear();

            if (status.IsSupported("setShootMode"))
            {
                panel.Children.Add(Panels["ShootMode"]);
            }

            if (status.IsSupported("setExposureMode"))
            {
                panel.Children.Add(Panels["ExposureMode"]);
                if (!status.Version.IsLiberated)
                {
                    Panels["ExposureMode"].Visibility = Visibility.Collapsed;
                }
            }

            if (status.IsSupported("setSelfTimer"))
            {
                panel.Children.Add(Panels["SelfTimer"]);
            }
            if (status.IsSupported("setPostviewImageSize"))
            {
                panel.Children.Add(Panels["PostViewSize"]);
            }

            if (status.IsSupported("actTakePicture"))
            {
                panel.Children.Add(Panels["IntervalSwitch"]);
                panel.Children.Add(Panels["IntervalValue"]);
            }

            Debug.WriteLine("panels has set!");

            panel.Margin = new Thickness(8, 24, 4, 24);
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
            var child = CreatePanel(AppResources.PostviewTransferSetting);

            var toggle = CreateToggle();
            var checkbind = new Binding()
            {
                Source = ApplicationSettings.GetInstance(),
                Path = new PropertyPath("IsPostviewTransferEnabled"),
                Mode = BindingMode.TwoWay
            };

            var enableBind = new Binding()
            {
                Source = data,
                Path = new PropertyPath("CpIsAvailableStillImageFunctions"),
                Mode = BindingMode.OneWay
            };

            toggle.SetBinding(ToggleSwitch.IsCheckedProperty, checkbind);
            toggle.SetBinding(ToggleSwitch.IsEnabledProperty, enableBind);

            child.Children.Add(toggle);
            return child;
        }

        private StackPanel CreateIntervalEnableSettingPanel()
        {
            var child = CreatePanel(AppResources.IntervalSetting);

            var toggle = CreateToggle();
            var checkbind = new Binding()
            {
                Source = ApplicationSettings.GetInstance(),
                Path = new PropertyPath("IsIntervalShootingEnabled"),
                Mode = BindingMode.TwoWay
            };
            toggle.SetBinding(ToggleSwitch.IsCheckedProperty, checkbind);

            var enableBind = new Binding()
            {
                Source = data,
                Path = new PropertyPath("CpIsAvailableStillImageFunctions"),
                Mode = BindingMode.OneWay
            };
            toggle.SetBinding(ToggleSwitch.IsEnabledProperty, enableBind);

            child.Children.Add(toggle);
            return child;
        }


        private StackPanel CreateIntervalTimeSliderPanel()
        {
            var child = CreatePanel(AppResources.IntervalTime);
            Debug.WriteLine("create panel: " + AppResources.IntervalTime);

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

            var enableBind = new Binding()
            {
                Source = data,
                Path = new PropertyPath("CpIsAvailableStillImageFunctions"),
                Mode = BindingMode.OneWay
            };

            var indicator = new TextBlock
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = System.Windows.VerticalAlignment.Top,
                    Style = Application.Current.Resources["JumpListStringStyle"] as Style,
                    Margin = new Thickness(10, 18, 0, 0),
                    MinWidth = 25
                };
            indicator.SetBinding(TextBlock.TextProperty, selectedbind);
            slider.SetBinding(Slider.ValueProperty, selectedbind);
            slider.SetBinding(Slider.IsEnabledProperty, enableBind);

            hPanel.Children.Add(indicator);
            hPanel.Children.Add(slider);

            child.Children.Add(hPanel);
            return child;
        }

        private StackPanel CreateExposureCompensationSliderPanel()
        {
            var child = CreatePanel(AppResources.ExposureCompensation);
            var slider = CreateSlider(-10, 10);
            slider.Value = 0;

            slider.ManipulationCompleted += (sender, e) =>
            {
                if (status == null || status.EvInfo == null)
                {
                    Debug.WriteLine("return null.");
                    return;
                }
                var selected = (int)(sender as Slider).Value;

                //

                try
                {
                    Debug.WriteLine("Set EV Index: " + selected);
                    manager.SetExposureCompensation(selected);
                }
                catch (InvalidOperationException)
                {
                    Debug.WriteLine("Not ready to call Web API");
                }
            };

            var hPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center
            };

            var selectedbind = new Binding()
            {
                Source = data,
                Path = new PropertyPath("CpSelectedIndexExposureCompensation"),
                Mode = BindingMode.TwoWay
            };

            var enableBind = new Binding()
            {
                Source = data,
                Path = new PropertyPath("CpIsAvailableExposureCompensation"),
                Mode = BindingMode.OneWay
            };

            var maxBind = new Binding()
            {
                Source = data,
                Path = new PropertyPath("CpMaxExposureCompensation"),
                Mode = BindingMode.OneWay
            };

            var minBind = new Binding()
            {
                Source = data,
                Path = new PropertyPath("CpMinExposureCompensation"),
                Mode = BindingMode.OneWay
            };

            var displayValueBind = new Binding()
            {
                Source = data,
                Path = new PropertyPath("CpDisplayValueExposureCompensation"),
                Mode = BindingMode.OneWay,
            };

            var indicator = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                Style = Application.Current.Resources["JumpListStringStyle"] as Style,
                Margin = new Thickness(10, 18, 0, 0),
                MinWidth = 25
            };
            indicator.SetBinding(TextBlock.TextProperty, displayValueBind);
            slider.SetBinding(Slider.ValueProperty, selectedbind);
            slider.SetBinding(Slider.IsEnabledProperty, enableBind);
            slider.SetBinding(Slider.MaximumProperty, maxBind);
            slider.SetBinding(Slider.MinimumProperty, minBind);

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
                Margin = new Thickness(10, -5, 10, -40)
            };
        }

        private static Slider CreateSlider(int min, int max)
        {
            return new Slider
            {
                Maximum = max,
                Minimum = min,
                Margin = new Thickness(5, 12, 10, -36),
                MinWidth = 185,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                Background = Application.Current.Resources["PhoneProgressBarBackgroundBrush"] as Brush
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

        public void OnControlPanelPropertyChanged(String name)
        {
            data.OnControlPanelPropertyChanged(name);
        }

        private async void OnShootModeChanged(object sender, SelectionChangedEventArgs arg)
        {
            if (status.ShootModeInfo == null || status.ShootModeInfo.candidates == null || status.ShootModeInfo.candidates.Length == 0)
                return;
            var picker = sender as ListPicker;
            var selected = picker.SelectedIndex;
            if (SettingsValueConverter.GetSelectedIndex(status.ShootModeInfo) != selected)
            {
                return;
            }
            try
            {
                await manager.SetShootModeAsync(status.ShootModeInfo.candidates[selected]);
            }
            catch (InvalidOperationException)
            {
                Debug.WriteLine("Not ready to call Web API");
            }
            catch (RemoteApiException e)
            {
                Debug.WriteLine("Failed to set shootmode: " + e.code);
                manager.RefreshEventObserver();
            }
        }

        private async void OnSelfTimerChanged(object sender, SelectionChangedEventArgs arg)
        {
            if (status.SelfTimerInfo == null || status.SelfTimerInfo.candidates == null || status.SelfTimerInfo.candidates.Length == 0)
                return;
            var selected = (sender as ListPicker).SelectedIndex;
            if (SettingsValueConverter.GetSelectedIndex(status.SelfTimerInfo) != selected)
            {
                return;
            }
            try
            {
                await manager.SetSelfTimerAsync(status.SelfTimerInfo.candidates[selected]);
            }
            catch (InvalidOperationException)
            {
                Debug.WriteLine("Not ready to call Web API");
            }
            catch (RemoteApiException e)
            {
                Debug.WriteLine("Failed to set selftimer: " + e.code);
                manager.RefreshEventObserver();
            }
        }

        private async void OnPostViewSizeChanged(object sender, SelectionChangedEventArgs arg)
        {
            if (status.PostviewSizeInfo == null || status.PostviewSizeInfo.candidates == null || status.PostviewSizeInfo.candidates.Length == 0)
                return;
            var selected = (sender as ListPicker).SelectedIndex;
            if (SettingsValueConverter.GetSelectedIndex(status.PostviewSizeInfo) != selected)
            {
                return;
            }
            try
            {
                await manager.SetPostViewImageSizeAsync(status.PostviewSizeInfo.candidates[selected]);
            }
            catch (InvalidOperationException)
            {
                Debug.WriteLine("Not ready to call Web API");
            }
            catch (RemoteApiException e)
            {
                Debug.WriteLine("Failed to set postview image size: " + e.code);
                manager.RefreshEventObserver();
            }
        }

        private async void OnExposureModeChanged(object sender, SelectionChangedEventArgs arg)
        {
            if (status.ExposureMode == null || status.ExposureMode.candidates == null || status.ExposureMode.candidates.Length == 0)
            {
                return;
            }
            var selected = (sender as ListPicker).SelectedIndex;
            if (SettingsValueConverter.GetSelectedIndex(status.ExposureMode) != selected)
            {
                return;
            }
            try
            {
                await manager.SetExporeModeAsync(status.ExposureMode.candidates[selected]);
            }
            catch (InvalidOperationException)
            {
                Debug.WriteLine("Not ready to call Web API");
            }
            catch (RemoteApiException e)
            {
                manager.RefreshEventObserver();
            }
        }
    }
}
