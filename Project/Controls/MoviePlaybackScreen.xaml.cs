using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Kazyx.WPPMM.Utils;

namespace Kazyx.WPPMM.Controls
{
    public partial class MoviePlaybackScreen : UserControl
    {
        public double PlaybackPosition { get; set; }
        public static readonly DependencyProperty PlaybackPositionProperty = DependencyProperty.Register(
            "Type",
            typeof(double),
            typeof(MoviePlaybackScreen),
            new PropertyMetadata(new PropertyChangedCallback(MoviePlaybackScreen.OnPlaybackPositionChanged)));

        public TimeSpan CurrentPosition { get; set; }
        public static readonly DependencyProperty CurrentPositionProperty = DependencyProperty.Register(
            "Type",
            typeof(TimeSpan),
            typeof(MoviePlaybackScreen),
            new PropertyMetadata(new PropertyChangedCallback(MoviePlaybackScreen.OnCurrentPositionChanged)));

        private static void OnCurrentPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DebugUtil.Log("Position updated: " + ((TimeSpan)e.NewValue).TotalSeconds);
        }

        public TimeSpan Duration { get; set; }
        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
            "Type",
            typeof(TimeSpan),
            typeof(MoviePlaybackScreen),
            new PropertyMetadata(new PropertyChangedCallback(MoviePlaybackScreen.OnDurationChanged)));

        private static void OnDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DebugUtil.Log("Duration updated: " + ((TimeSpan)e.NewValue).TotalSeconds);
        }


        private bool _SeekAvailable = false;
        public bool SeekAvailable
        {
            get { return _SeekAvailable; }
            set
            {
                if (_SeekAvailable != value)
                {
                    _SeekAvailable = value;
                    if (value)
                    {
                        this.ProgressBar.Visibility = System.Windows.Visibility.Collapsed;
                        this.SeekBar.Visibility = System.Windows.Visibility.Visible;
                    }
                    else
                    {
                        this.ProgressBar.Visibility = System.Windows.Visibility.Visible;
                        this.SeekBar.Visibility = System.Windows.Visibility.Collapsed;
                    }
                }
            }
        }

        public Action<double> SeekOperated;

        private static void OnPlaybackPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var value = (double)e.NewValue;
            DebugUtil.Log("Position Changed: " + (double)e.NewValue);
            if (value < 0 || value > 100)
            {
                return;
            }
            if ((d as MoviePlaybackScreen).SeekAvailable)
            {
                (d as MoviePlaybackScreen).SeekBar.Value = value;
            }
            else
            {
                (d as MoviePlaybackScreen).ProgressBar.Value = value;
            }
        }

        public void Reset()
        {
            if (SeekAvailable)
            {
                this.SeekBar.Value = 0;
            }
            else
            {
                this.ProgressBar.Value = 0;
            }
        }

        public MoviePlaybackScreen()
        {
            InitializeComponent();
        }

        private void SeekBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SeekOperated != null)
            {
                SeekOperated(e.NewValue);
            }
        }
    }
}
