using Kazyx.RemoteApi;
using Kazyx.WPMMM.Resources;
using Kazyx.WPPMM.DataModel;
using Kazyx.WPPMM.Utils;
using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
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

            // Key of the Dictionary is the name of setter API in most cases. Uses to check availability.
            Panels.Add("setShootMode", CreateStatusPanel("ShootMode", AppResources.ShootMode, OnShootModeChanged));
            Panels.Add("setExposureMode", CreateStatusPanel("ExposureMode", AppResources.ExposureMode, OnExposureModeChanged));
            Panels.Add("setMovieQuality", CreateStatusPanel("MovieQuality", AppResources.MovieQuality, OnMovieQualityChanged));
            Panels.Add("setSteadyMode", CreateStatusPanel("SteadyMode", AppResources.SteadyShot, OnSteadyModeChanged));
            Panels.Add("setSelfTimer", CreateStatusPanel("SelfTimer", AppResources.SelfTimer, OnSelfTimerChanged));
            Panels.Add("setStillSize", CreateStatusPanel("StillImageSize", AppResources.StillImageSize, OnStillImageSizeChanged));
            Panels.Add("setPostViewSize", CreateStatusPanel("PostviewSize", AppResources.Setting_PostViewImageSize, OnPostViewSizeChanged));
            Panels.Add("setViewAngle", CreateStatusPanel("ViewAngle", AppResources.ViewAngle, OnViewAngleChanged));
            Panels.Add("setBeepMode", CreateStatusPanel("BeepMode", AppResources.BeepMode, OnBeepModeChanged));
            Panels.Add("IntervalSwitch", CreateIntervalEnableSettingPanel());
            Panels.Add("IntervalValue", CreateIntervalTimeSliderPanel());

            manager.MethodTypesUpdateNotifer += () => { Initialize(); };
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

        private void Initialize()
        {
            panel.Children.Clear();

            var visibility = new Binding()
            {
                Source = status,
                Path = new PropertyPath("IsRestrictedApiVisible"),
                Mode = BindingMode.OneWay,
                FallbackValue = Visibility.Collapsed
            };

            foreach (var key in Panels.Keys)
            {
                if (status.IsSupported(key))
                {
                    panel.Children.Add(Panels[key]);
                    if (status.IsRestrictedApi(key))
                    {
                        Panels[key].SetBinding(StackPanel.VisibilityProperty, visibility);
                    }
                }
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

        private static ListPicker CreatePicker()
        {
            return new ListPicker
            {
                SelectionMode = SelectionMode.Single,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(10, -5, 10, 0)
            };
        }

        private static ToggleSwitch CreateToggle()
        {
            return new ToggleSwitch
            {
                Margin = new Thickness(10, -5, 10, -40),
                BorderThickness = new Thickness(1),
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
            await OnPickerChanged<string>(sender, status.ShootModeInfo,
                async (selected) => { await manager.SetShootModeAsync(selected); });
        }

        private async void OnSelfTimerChanged(object sender, SelectionChangedEventArgs arg)
        {
            await OnPickerChanged<int>(sender, status.SelfTimerInfo,
                async (selected) => { await manager.SetSelfTimerAsync(selected); });
        }

        private async void OnPostViewSizeChanged(object sender, SelectionChangedEventArgs arg)
        {
            await OnPickerChanged<string>(sender, status.PostviewSizeInfo,
                async (selected) => { await manager.SetPostViewImageSizeAsync(selected); });
        }

        private async void OnExposureModeChanged(object sender, SelectionChangedEventArgs arg)
        {
            await OnPickerChanged<string>(sender, status.ExposureMode,
                async (selected) => { await manager.SetExporeModeAsync(selected); });
        }

        private async void OnBeepModeChanged(object sender, SelectionChangedEventArgs arg)
        {
            await OnPickerChanged<string>(sender, status.BeepMode,
                async (selected) => { await manager.SetBeepModeAsync(selected); });
        }

        private async void OnViewAngleChanged(object sender, SelectionChangedEventArgs arg)
        {
            await OnPickerChanged<int>(sender, status.ViewAngle,
                async (selected) => { await manager.SetViewAngleAsync(selected); });
        }

        private async void OnSteadyModeChanged(object sender, SelectionChangedEventArgs arg)
        {
            await OnPickerChanged<string>(sender, status.SteadyMode,
                async (selected) => { await manager.SetSteadyModeAsync(selected); });
        }

        private async void OnMovieQualityChanged(object sender, SelectionChangedEventArgs arg)
        {
            await OnPickerChanged<string>(sender, status.MovieQuality,
                async (selected) => { await manager.SetMovieQualityAsync(selected); });
        }

        private async void OnStillImageSizeChanged(object sender, SelectionChangedEventArgs arg)
        {
            await OnPickerChanged<StillImageSize>(sender, status.StillImageSize,
                async (selected) => { await manager.SetStillImageSizeAsync(selected); });
        }

        private async Task OnPickerChanged<T>(object sender, Capability<T> param, AsyncAction<T> action)
        {
            if (param == null || param.candidates == null || param.candidates.Length == 0)
                return;
            var selected = (sender as ListPicker).SelectedIndex;
            if (SettingsValueConverter.GetSelectedIndex(param) != selected)
            {
                return;
            }
            try
            {
                await action.Invoke(param.candidates[selected]);
            }
            catch (InvalidOperationException)
            {
                Debug.WriteLine("Not ready to call Web API");
            }
            catch (RemoteApiException e)
            {
                Debug.WriteLine("Failed to set: " + e.code);
                manager.RefreshEventObserver();
            }
        }

        private delegate Task AsyncAction<T>(T arg);
    }
}
