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
using Kazyx.WPPMM.CameraManager;

namespace Kazyx.WPMMM.Controls
{
    public partial class SettingDial : UserControl
    {
        public const int THRESHOLD = 7;
        public const int GEAR_SPEED = 10;

        private double GearAngle;
        private int OperationCount;

        private CameraManager manager;

        public SettingDial()
        {
            InitializeComponent();

            GearAngle = 0;
            OperationCount = 0;

            manager = CameraManager.GetInstance();
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
                OperationCount--;
            }
            else if (delta.Translation.Y < (-THRESHOLD))
            {
                Debug.WriteLine("Operation: Up");
                RotateGear(GearAngle, GearAngle + THRESHOLD);
                GearAngle += GEAR_SPEED;
                OperationCount++;
            }
        }

        private void Gear_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            if (manager == null || manager.cameraStatus == null)
            {
                OperationCount = 0;
                return;
            }

            // Ev
            if (manager.cameraStatus.IsAvailable("setExposureCompensation") && manager.cameraStatus.EvInfo != null)
            {
                var target = manager.cameraStatus.EvInfo.CurrentIndex + OperationCount;
                if (target >= manager.cameraStatus.EvInfo.Candidate.MinIndex && target <= manager.cameraStatus.EvInfo.Candidate.MaxIndex)
                {
                    manager.SetExposureCompensation(target);
                }
            }


            OperationCount = 0;
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
