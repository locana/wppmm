using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Diagnostics;

namespace Kazyx.WPMMM.Controls
{
    public partial class PullReleaseBar : UserControl
    {
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

        private double CurrentValue;
        private Thickness InitialCursorMargin;
        private Thickness InitialLabelMargin;

        public Action<double> ManipulationCompleted { get; set; }
        
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
            // Debug.WriteLine("accm: " + accm.Translation.X);
            var vel = e.Velocities;
            // Debug.WriteLine("v: " + vel.LinearVelocity.X);

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

            if (ManipulationCompleted != null)
            {
                ManipulationCompleted(CurrentValue);
            }
        }

        private void LayoutRoot_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width != 0)
            {
                InitialCursorMargin = new Thickness(LayoutRoot.ActualWidth / 2 - Cursor.ActualWidth / 2, 0, 0, 0);
                InitialLabelMargin = new Thickness(LayoutRoot.ActualWidth / 2 - CurrentValueText.Width / 2, 0, 0, 0);
                Debug.WriteLine("initial X: " + (LayoutRoot.ActualWidth / 2 - Cursor.ActualWidth / 2));
                Debug.WriteLine("initial label X: " + (LayoutRoot.ActualWidth / 2 - CurrentValueText.ActualWidth / 2));
                Cursor.Margin = InitialCursorMargin;
                CurrentValueText.Margin = InitialLabelMargin;
                DynamicBar.Y2 = DynamicBar.Y1 = LayoutRoot.ActualHeight / 2;
                DynamicBar.X2 = DynamicBar.X1 = LayoutRoot.ActualWidth / 2;
                Debug.WriteLine(Max + " " + Min);
            }
        }
    }
}
