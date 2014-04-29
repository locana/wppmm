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
using System.Windows.Media.Animation;
using System.Windows.Media;

namespace Kazyx.WPMMM.Controls
{
    public partial class SettingDial : UserControl
    {
        public const int THRESHOLD = 20;
        public const int GEAR_SPEED = 10;

        private double GearAngle;

        public SettingDial()
        {
            InitializeComponent();

            GearAngle = 0;
        }

        private void Gear_ManipulationDelta(object sender, System.Windows.Input.ManipulationDeltaEventArgs e)
        {
            var delta = e.DeltaManipulation;
            // Debug.WriteLine("delta: " + delta.Translation.Y);
            var accm = e.CumulativeManipulation;
            // Debug.WriteLine("accm: " + accm.Translation.Y);
            var vel = e.Velocities;
            // Debug.WriteLine("v: " + vel.LinearVelocity.Y);
            if (delta.Translation.Y > THRESHOLD)
            {
                Debug.WriteLine("Operation: Down");
                RotateGear(GearAngle, GearAngle - GEAR_SPEED);
                GearAngle -= GEAR_SPEED;
            }
            else if (delta.Translation.Y < (-THRESHOLD))
            {
                Debug.WriteLine("Operation: Up");
                RotateGear(GearAngle, GearAngle + THRESHOLD);
                GearAngle += GEAR_SPEED;
            }
        }

        private void Gear_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {

        }

        private void RotateGear(double from, double to)
        {
            var story = new Storyboard();
            var duration= new TimeSpan(0, 0, 0, 0, 100);
            var animation = new DoubleAnimation();
            var rt = new RotateTransform();

            story.Duration = duration;
            animation.Duration = duration;
            story.Children.Add(animation);
            Storyboard.SetTarget(animation, rt);
            Storyboard.SetTargetProperty(animation, new PropertyPath("Angle"));
            animation.From = from;
            animation.To = to;

            Gear.RenderTransform = rt;
            Gear.RenderTransformOrigin = new Point(0.5, 0.5);

            story.Begin();

            




        }
    }
}
