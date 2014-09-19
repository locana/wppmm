using Kazyx.RemoteApi;
using Kazyx.RemoteApi.Camera;
using Kazyx.WPMMM.Resources;
using Kazyx.WPMMM.Utils;
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

            // shoot settings.
            Panels.Add("setShootMode", CreateStatusPanel("ShootMode", AppResources.ShootMode, OnShootModeChanged));
            Panels.Add("setExposureMode", CreateStatusPanel("ExposureMode", AppResources.ExposureMode, OnExposureModeChanged));
            Panels.Add("setContShootingMode", CreateStatusPanel("ContShootingMode", AppResources.ContShootingMode, OnContShootingModeChanged));
            Panels.Add("setContShootingSpeed", CreateStatusPanel("ContShootingSpeed", AppResources.ContShootingSpeed, OnContShootingSpeedChanged));
            Panels.Add("setFocusMode", CreateStatusPanel("FocusMode", AppResources.FocusMode, OnFocusModeChanged));
            Panels.Add("setWhiteBalance", CreateStatusPanel("WhiteBalance", AppResources.WhiteBalance, OnWhiteBalanceChanged));
            Panels.Add("ColorTemperture", CreateColorTemperturePanel());
            Panels.Add("setFlashMode", CreateStatusPanel("FlashMode", AppResources.FlashMode, OnFlashModeChanged));
            Panels.Add("setZoomSetting", CreateStatusPanel("ZoomSetting", AppResources.ZoomSetting, OnZoomSettingChanged));
            Panels.Add("setSceneSelection", CreateStatusPanel("SceneSelection", AppResources.SceneSelection, OnSceneSelectionChanged));
            Panels.Add("setTrackingFocusMode", CreateStatusPanel("TrackingFocusMode", AppResources.TrackingFocusMode, OnTrackingFocusModeChanged));
            Panels.Add("setSteadyMode", CreateStatusPanel("SteadyMode", AppResources.SteadyShot, OnSteadyModeChanged));
            Panels.Add("setSelfTimer", CreateStatusPanel("SelfTimer", AppResources.SelfTimer, OnSelfTimerChanged));
            Panels.Add("setStillSize", CreateStatusPanel("StillImageSize", AppResources.StillImageSize, OnStillImageSizeChanged));
            Panels.Add("setMovieQuality", CreateStatusPanel("MovieQuality", AppResources.MovieQuality, OnMovieQualityChanged));
            Panels.Add("setImageQuality", CreateStatusPanel("ImageQuality", AppResources.ImageQuality, OnImageQualityChanged));
            Panels.Add("setMovieFormat", CreateStatusPanel("MovieFormat", AppResources.MovieFormat, OnMovieFormatChanged));

            // other
            Panels.Add("setPostviewImageSize", CreateStatusPanel("PostviewSize", AppResources.Setting_PostViewImageSize, OnPostViewSizeChanged));
            Panels.Add("setViewAngle", CreateStatusPanel("ViewAngle", AppResources.ViewAngle, OnViewAngleChanged));
            Panels.Add("setBeepMode", CreateStatusPanel("BeepMode", AppResources.BeepMode, OnBeepModeChanged));
            Panels.Add("setFlipMode", CreateStatusPanel("FlipMode", AppResources.FlipMode, OnFlipModeChanged));
            Panels.Add("setIntervalTime", CreateStatusPanel("IntervalTime", AppResources.IntervalTime1, OnIntervalTimeChanged));
            Panels.Add("setColorSetting", CreateStatusPanel("ColorSetting", AppResources.ColorSetting, OnColorSettingChanged));
            Panels.Add("setIrRemoteControl", CreateStatusPanel("IrRemoteControl", AppResources.IrRemoteControl, OnIrRemoteControlChanged));
            Panels.Add("setTvColorSystem", CreateStatusPanel("TvColorSystem", AppResources.TvColorSystem, OnTvColorSystemChanged));
            Panels.Add("setAutoPowerOff", CreateStatusPanel("AutoPowerOff", AppResources.AutoPowerOff, OnAutoPowerOffChanged));

            // local interval.
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

            panel.Children.Add(new Border { Height = 32 });

            foreach (var key in Panels.Keys)
            {
                if (status.IsSupported(key) ||
                    (key == "ColorTemperture" && status.IsSupported("setWhiteBalance")))
                {
                    panel.Children.Add(Panels[key]);
                    if (status.IsRestrictedApi(key))
                    {
                        Panels[key].SetBinding(StackPanel.VisibilityProperty, visibility);
                    }
                }
                else if ((key == "IntervalSwitch" || key == "IntervalValue") && status.IsSupported("actTakePicture"))
                {
                    panel.Children.Add(Panels[key]);
                }
            }

            panel.Children.Add(new Border { Height = 96 });

            panel.Margin = new Thickness(8, 0, 4, 0);
            panel.MinWidth = 240;
            panel.Width = double.NaN;
        }

        private readonly StackPanel DummyPanel = new StackPanel
        {
            Height = 64
        };

        public void Hide()
        {
            panel.Visibility = Visibility.Collapsed;
        }

        private StackPanel CreateStatusPanel(string id, string title, SelectionChangedEventHandler handler)
        {
            var child = CreatePanel(title);

            var picker = CreatePicker();
            picker.SetBinding(ListPicker.IsEnabledProperty, new Binding()
            {
                Source = data,
                Path = new PropertyPath("CpIsAvailable" + id),
                Mode = BindingMode.OneWay
            });
            picker.SetBinding(ListPicker.ItemsSourceProperty, new Binding()
            {
                Source = data,
                Path = new PropertyPath("CpCandidates" + id),
                Mode = BindingMode.OneWay
            });
            picker.SetBinding(ListPicker.SelectedIndexProperty, new Binding()
            {
                Source = data,
                Path = new PropertyPath("CpSelectedIndex" + id),
                Mode = BindingMode.TwoWay
            });

            picker.SelectionChanged += handler;
            child.Children.Add(picker);
            return child;
        }

        private StackPanel CreateIntervalEnableSettingPanel()
        {
            var child = CreatePanel(AppResources.IntervalSetting);

            var toggle = CreateToggle();

            toggle.SetBinding(ToggleSwitch.IsCheckedProperty, new Binding()
            {
                Source = ApplicationSettings.GetInstance(),
                Path = new PropertyPath("IsIntervalShootingEnabled"),
                Mode = BindingMode.TwoWay
            });

            toggle.SetBinding(ToggleSwitch.IsEnabledProperty, new Binding()
            {
                Source = data,
                Path = new PropertyPath("CpIsAvailableStillImageFunctions"),
                Mode = BindingMode.OneWay
            });

            child.Children.Add(toggle);
            return child;
        }

        private StackPanel CreateIntervalTimeSliderPanel()
        {
            var child = CreatePanel(AppResources.IntervalTime);

            var slider = CreateSlider(5, 30);
            slider.Value = ApplicationSettings.GetInstance().IntervalTime;

            slider.ValueChanged += (sender, e) =>
            {
                ApplicationSettings.GetInstance().IntervalTime = (int)e.NewValue;
            };

            var indicator = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                Style = Application.Current.Resources["PhoneTextSmallStyle"] as Style,
                Margin = new Thickness(10, 22, 0, 0)
            };

            indicator.SetBinding(TextBlock.TextProperty, new Binding()
            {
                Source = ApplicationSettings.GetInstance(),
                Path = new PropertyPath("IntervalTime"),
                Mode = BindingMode.OneWay,
                StringFormat = "{0} sec."
            });
            (child.Children[0] as StackPanel).Children.Add(indicator);

            slider.SetBinding(Slider.ValueProperty, new Binding()
            {
                Source = ApplicationSettings.GetInstance(),
                Path = new PropertyPath("IntervalTime"),
                Mode = BindingMode.TwoWay
            });
            slider.SetBinding(Slider.IsEnabledProperty, new Binding()
            {
                Source = data,
                Path = new PropertyPath("CpIsAvailableStillImageFunctions"),
                Mode = BindingMode.OneWay
            });

            child.Children.Add(slider);

            child.SetBinding(StackPanel.VisibilityProperty, new Binding()
            {
                Source = ApplicationSettings.GetInstance(),
                Path = new PropertyPath("IntervalTimeVisibility"),
                Mode = BindingMode.OneWay,
                FallbackValue = Visibility.Collapsed
            });

            return child;
        }

        /// <summary>
        /// Convert to the nearest color temperture candidate value.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private int AsValidColorTemperture(int source)
        {
            var candidates = status.ColorTempertureCandidates[status.WhiteBalance.Current];
            if (candidates.Length < 2)
            {
                return -1;
            }
            var step = candidates[1] - candidates[0];

            var index_below = (source - candidates[0]) / step;
            if (index_below == candidates.Length - 1)
            {
                return candidates[index_below];
            }

            var diff_below = source - candidates[index_below];
            var diff_above = candidates[index_below + 1] - source;

            return diff_below < diff_above ? candidates[index_below] : candidates[index_below + 1];
        }

        private Slider ColorSlider;

        private StackPanel CreateColorTemperturePanel()
        {
            var child = CreatePanel(AppResources.WB_ColorTemperture);

            var slider = CreateSlider(null, null);
            slider.Value = 0;

            slider.ManipulationCompleted += async (sender, e) =>
            {
                var sld = sender as Slider;
                var target = AsValidColorTemperture((int)sld.Value);
                sld.Value = target;
                try
                {
#if !COLOR_TEMPERTURE_MOCK
                    await manager.CameraApi.SetWhiteBalanceAsync(new WhiteBalance { Mode = status.WhiteBalance.Current, ColorTemperature = target });
#endif
                }
                catch (RemoteApiException ex)
                {
                    DebugUtil.Log("Failed to set color temperture: " + ex.code);
                }
            };

            var indicator = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                Style = Application.Current.Resources["PhoneTextSmallStyle"] as Style,
                Margin = new Thickness(10, 22, 0, 0),
            };

            indicator.SetBinding(TextBlock.TextProperty, new Binding()
            {
                Source = status,
                Path = new PropertyPath("ColorTemperture"),
                Mode = BindingMode.OneWay,
                StringFormat = "{0}K"
            });

            (child.Children[0] as StackPanel).Children.Add(indicator);

            slider.SetBinding(Slider.ValueProperty, new Binding()
            {
                Source = status,
                Path = new PropertyPath("ColorTemperture"),
                Mode = BindingMode.TwoWay
            });
            slider.MinWidth = 320;

            ColorSlider = slider;

            child.Children.Add(slider);

            child.SetBinding(StackPanel.VisibilityProperty, new Binding()
            {
                Source = data,
                Path = new PropertyPath("CpIsVisibleColorTemperture"),
                Mode = BindingMode.OneWay
            });

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

        private static Slider CreateSlider(int? min, int? max)
        {
            return new Slider
            {
                Maximum = max != null ? max.Value : 1,
                Minimum = min != null ? min.Value : 0,
                Margin = new Thickness(5, 0, 10, -40),
                MinWidth = 185,
                Width = double.NaN,
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

            var titlePanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                Orientation = System.Windows.Controls.Orientation.Horizontal
            };

            titlePanel.Children.Add(new TextBlock
                {
                    Text = title,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Style = Application.Current.Resources["PhoneTextStyle"] as Style,
                    Margin = new Thickness(5, 20, 0, 0),
                });

            child.Children.Add(titlePanel);
            return child;
        }

        public void OnControlPanelPropertyChanged(string name)
        {
            data.OnControlPanelPropertyChanged(name);
        }

        private async void OnShootModeChanged(object sender, SelectionChangedEventArgs arg)
        {
            await OnPickerChanged<string>(sender, status.ShootModeInfo,
                async (selected) => { await manager.CameraApi.SetShootModeAsync(selected); });
        }

        private async void OnSelfTimerChanged(object sender, SelectionChangedEventArgs arg)
        {
            await OnPickerChanged<int>(sender, status.SelfTimerInfo,
                async (selected) => { await manager.CameraApi.SetSelfTimerAsync(selected); });
        }

        private async void OnPostViewSizeChanged(object sender, SelectionChangedEventArgs arg)
        {
            await OnPickerChanged<string>(sender, status.PostviewSizeInfo,
                async (selected) => { await manager.CameraApi.SetPostviewImageSizeAsync(selected); });
        }

        private async void OnExposureModeChanged(object sender, SelectionChangedEventArgs arg)
        {
            await OnPickerChanged<string>(sender, status.ExposureMode,
                async (selected) => { await manager.CameraApi.SetExposureModeAsync(selected); });
        }

        private async void OnBeepModeChanged(object sender, SelectionChangedEventArgs arg)
        {
            await OnPickerChanged<string>(sender, status.BeepMode,
                async (selected) => { await manager.CameraApi.SetBeepModeAsync(selected); });
        }

        private async void OnViewAngleChanged(object sender, SelectionChangedEventArgs arg)
        {
            await OnPickerChanged<int>(sender, status.ViewAngle,
                async (selected) => { await manager.CameraApi.SetViewAngleAsync(selected); });
        }

        private async void OnSteadyModeChanged(object sender, SelectionChangedEventArgs arg)
        {
            await OnPickerChanged<string>(sender, status.SteadyMode,
                async (selected) => { await manager.CameraApi.SetSteadyModeAsync(selected); });
        }

        private async void OnMovieQualityChanged(object sender, SelectionChangedEventArgs arg)
        {
            await OnPickerChanged<string>(sender, status.MovieQuality,
                async (selected) => { await manager.CameraApi.SetMovieQualityAsync(selected); });
        }

        private async void OnStillImageSizeChanged(object sender, SelectionChangedEventArgs arg)
        {
            await OnPickerChanged<StillImageSize>(sender, status.StillImageSize,
                async (selected) => { await manager.CameraApi.SetStillImageSizeAsync(selected); });
        }

        private async void OnFlashModeChanged(object sender, SelectionChangedEventArgs arg)
        {
            await OnPickerChanged<string>(sender, status.FlashMode,
                async (selected) => { await manager.CameraApi.SetFlashModeAsync(selected); });
        }

        private async void OnFocusModeChanged(object sender, SelectionChangedEventArgs arg)
        {
            await OnPickerChanged<string>(sender, status.FocusMode,
                async (selected) => { await manager.CameraApi.SetFocusModeAsync(selected); });
        }

        private async void OnZoomSettingChanged(object sender, SelectionChangedEventArgs arg)
        {
            await OnPickerChanged<string>(sender, status.ZoomSetting,
            async (selected) => { await manager.CameraApi.SetZoomSettingAsync(new ZoomSetting { Mode = selected }); });
        }
        private async void OnImageQualityChanged(object sender, SelectionChangedEventArgs arg)
        {
            await OnPickerChanged<string>(sender, status.ImageQuality,
            async (selected) => { await manager.CameraApi.SetStillQualityAsync(new ImageQualitySetting { Mode = selected }); });
        }
        private async void OnContShootingModeChanged(object sender, SelectionChangedEventArgs arg)
        {
            await OnPickerChanged<string>(sender, status.ContShootingMode,
            async (selected) => { await manager.CameraApi.SetContShootingModeAsync(new ContinuousShootSetting { Mode = selected }); });
        }
        private async void OnContShootingSpeedChanged(object sender, SelectionChangedEventArgs arg)
        {
            await OnPickerChanged<string>(sender, status.ContShootingSpeed,
            async (selected) => { await manager.CameraApi.SetContShootingSpeedAsync(new ContinuousShootSpeedSetting { Mode = selected }); });
        }
        private async void OnFlipModeChanged(object sender, SelectionChangedEventArgs arg)
        {
            await OnPickerChanged<string>(sender, status.FlipMode,
            async (selected) => { await manager.CameraApi.SetFlipSettingAsync(new FlipSetting { Mode = selected }); });
        }
        private async void OnSceneSelectionChanged(object sender, SelectionChangedEventArgs arg)
        {
            await OnPickerChanged<string>(sender, status.SceneSelection,
            async (selected) => { await manager.CameraApi.SetSceneSelectionAsync(new SceneSelectionSetting { Mode = selected }); });
        }
        private async void OnIntervalTimeChanged(object sender, SelectionChangedEventArgs arg)
        {
            await OnPickerChanged<string>(sender, status.IntervalTime,
            async (selected) => { await manager.CameraApi.SetIntervalTimeAsync(new IntervalTimeSetting { TimeInSeconds = selected }); });
        }
        private async void OnColorSettingChanged(object sender, SelectionChangedEventArgs arg)
        {
            await OnPickerChanged<string>(sender, status.ColorSetting,
            async (selected) => { await manager.CameraApi.SetColorSettingAsync(new ColorSetting { Mode = selected }); });
        }
        private async void OnMovieFormatChanged(object sender, SelectionChangedEventArgs arg)
        {
            await OnPickerChanged<string>(sender, status.MovieFormat,
            async (selected) => { await manager.CameraApi.SetMovieFileFormatAsync(new MovieFormat { Mode = selected }); });
        }
        private async void OnIrRemoteControlChanged(object sender, SelectionChangedEventArgs arg)
        {
            await OnPickerChanged<string>(sender, status.IrRemoteControl,
            async (selected) => { await manager.CameraApi.SetInfraredRemoteControlAsync(new InfraredRemoteControl { Mode = selected }); });
        }
        private async void OnTvColorSystemChanged(object sender, SelectionChangedEventArgs arg)
        {
            await OnPickerChanged<string>(sender, status.TvColorSystem,
            async (selected) => { await manager.CameraApi.SetTvColorSystemAsync(new TvColorSystem { Mode = selected }); });
        }
        private async void OnTrackingFocusModeChanged(object sender, SelectionChangedEventArgs arg)
        {
            await OnPickerChanged<string>(sender, status.TrackingFocusMode,
            async (selected) => { await manager.CameraApi.SetTrackingFocusAsync(new TrackingFocusSetting { Mode = selected }); });
        }
        private async void OnAutoPowerOffChanged(object sender, SelectionChangedEventArgs arg)
        {
            await OnPickerChanged<int>(sender, status.AutoPowerOff,
            async (selected) => { await manager.CameraApi.SetAutoPowerOffAsync(new AutoPowerOff { TimeInSeconds = selected }); });
        }

        private async void OnWhiteBalanceChanged(object sender, SelectionChangedEventArgs arg)
        {
            await OnPickerChanged<string>(sender, status.WhiteBalance,
                async (selected) =>
                {
                    if (selected != WhiteBalanceMode.Manual)
                    {
                        status.ColorTemperture = -1;
                        await manager.CameraApi.SetWhiteBalanceAsync(new WhiteBalance { Mode = selected });
                    }
                    else
                    {
                        var min = status.ColorTempertureCandidates[WhiteBalanceMode.Manual][0];
#if !COLOR_TEMPERTURE_MOCK
                        await manager.CameraApi.SetWhiteBalanceAsync(new WhiteBalance { Mode = WhiteBalanceMode.Manual, ColorTemperature = min });
#endif
                        status.ColorTemperture = min;

                        if (ColorSlider != null)
                        {
                            var val = status.ColorTempertureCandidates[selected];
                            ColorSlider.Maximum = val[val.Length - 1];
                            ColorSlider.Minimum = val[0];
                            ColorSlider.Value = status.ColorTemperture;
                        }
                    }
                });
        }

        private async Task OnPickerChanged<T>(object sender, Capability<T> param, AsyncAction<T> action)
        {
            if (param == null || param.Candidates == null || param.Candidates.Count == 0)
                return;
            var selected = (sender as ListPicker).SelectedIndex;
            if (SettingsValueConverter.GetSelectedIndex(param) != selected)
            {
                // This change is not from this application, maybe from the camera device.
                return;
            }
            try
            {
                await action.Invoke(param.Candidates[selected]);
            }
            catch (NullReferenceException)
            {
                DebugUtil.Log("Not ready to call Web API");
            }
            catch (RemoteApiException e)
            {
                DebugUtil.Log("Failed to set: " + e.code);
                manager.RefreshEventObserver();
            }
            catch (KeyNotFoundException e)
            {
                DebugUtil.Log("Key not found: " + e.Message);
                manager.RefreshEventObserver();
            }
        }

        private delegate Task AsyncAction<T>(T arg);
    }
}
