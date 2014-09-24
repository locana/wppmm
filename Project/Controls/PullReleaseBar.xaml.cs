using Kazyx.WPPMM.Utils;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Kazyx.WPPMM.Controls
{
    public partial class PullReleaseBar : UserControl
    {
        public static readonly DependencyProperty MaxProperty = DependencyProperty.Register(
            "Max",
            typeof(int),
            typeof(PullReleaseBar),
            new PropertyMetadata(new PropertyChangedCallback(PullReleaseBar.OnMaxValueChanged)));

        public static readonly DependencyProperty MinProperty = DependencyProperty.Register(
            "Min",
            typeof(int),
            typeof(PullReleaseBar),
            new PropertyMetadata(new PropertyChangedCallback(PullReleaseBar.OnMinValueChanged)));

        public delegate void OnReleaseHandler(object sender, OnReleaseArgs e);
        public event OnReleaseHandler OnRelease;

        public int Max
        {
            get;
            set;
        }
        public int Min
        {
            get;
            set;
        }

        public string Unit
        {
            get;
            set;
        }

        private static void OnMaxValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // DebugUtil.Log("max updated: " + (int)e.NewValue);
            (d as PullReleaseBar).Max = (int)e.NewValue;
        }

        private static void OnMinValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // DebugUtil.Log("min updated: " + (int)e.NewValue);
            (d as PullReleaseBar).Min = (int)e.NewValue;
        }

        // public Action<int> OnRelease { get; set; }

        private double CurrentValue;
        private Thickness InitialCursorMargin;
        private Thickness InitialLabelMargin;



        public PullReleaseBar()
        {
            InitializeComponent();

            InitialCursorMargin = new Thickness(0, 0, 0, 0);
            InitialLabelMargin = new Thickness(0, 0, 0, 0);
            CurrentValue = 0.0;
            if (Unit == null)
            {
                Unit = "";
            }
        }

        private void TouchArea_ManipulationDelta(object sender, System.Windows.Input.ManipulationDeltaEventArgs e)
        {
            var accm = e.CumulativeManipulation;
            // DebugUtil.Log("accm: " + accm.Translation.X);
            var vel = e.Velocities;
            // DebugUtil.Log("v: " + vel.LinearVelocity.X);

            Cursor.Margin = new Thickness(InitialCursorMargin.Left + accm.Translation.X, 0, 0, 0); ;
            CurrentValueText.Margin = new Thickness(InitialLabelMargin.Left + accm.Translation.X, 0, 0, 0);
            DynamicBar.X2 = LayoutRoot.ActualWidth / 2 + accm.Translation.X;

            var length = Math.Abs(DynamicBar.X2 - DynamicBar.X1);
            DynamicBar.Opacity = length / (LayoutRoot.ActualWidth / 2);

            var value = (DynamicBar.X2 - DynamicBar.X1) / (LayoutRoot.ActualWidth / 2);
            if (value > 0)
            {
                CurrentValue = Math.Min(Math.Truncate((Max + 1) * value), Max);
            }
            else
            {
                CurrentValue = Math.Max(Math.Truncate((Min - 1) * Math.Abs(value)), Min);
            }
            CurrentValueText.Text = CurrentValue.ToString() + " " + Unit;

        }

        private void TouchArea_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            Cursor.Margin = InitialCursorMargin;
            CurrentValueText.Margin = InitialLabelMargin;
            DynamicBar.X2 = DynamicBar.X1;
            CurrentValueText.Text = "";

            if (OnRelease != null)
            {
                OnRelease(this, new OnReleaseArgs() { Value = (int)CurrentValue });
            }
        }

        private void LayoutRoot_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width != 0)
            {
                InitialCursorMargin = new Thickness(LayoutRoot.ActualWidth / 2 - Cursor.ActualWidth / 2, 0, 0, 0);
                InitialLabelMargin = new Thickness(LayoutRoot.ActualWidth / 2 - CurrentValueText.Width / 2, 0, 0, 0);
                DebugUtil.Log("initial X: " + (LayoutRoot.ActualWidth / 2 - Cursor.ActualWidth / 2));
                DebugUtil.Log("initial label X: " + (LayoutRoot.ActualWidth / 2 - CurrentValueText.ActualWidth / 2));
                Cursor.Margin = InitialCursorMargin;
                CurrentValueText.Margin = InitialLabelMargin;
                DynamicBar.Y2 = DynamicBar.Y1 = LayoutRoot.ActualHeight / 2;
                DynamicBar.X2 = DynamicBar.X1 = LayoutRoot.ActualWidth / 2;
                DebugUtil.Log("Max: " + Max + " Min: " + Min);
            }
        }
    }

    public class OnReleaseArgs : EventArgs
    {
        public int Value { get; set; }
    }
}
