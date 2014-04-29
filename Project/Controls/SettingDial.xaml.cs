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
        private const int TH_OPERATION = 2;
        private const int GEAR_SPEED = 18;
        private const int TH_SETTING = 40;

        private double GearAngle;
        private double OperationCount;
        private int SettingCount;

        private CameraManager manager;

        public SettingDial()
        {
            InitializeComponent();

            GearAngle = 0;
            OperationCount = 0;
            SettingCount = 0;

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
            var deltaY = delta.Translation.Y;

            // if each manipuration is too small, it's may be noise
            if (Math.Abs(deltaY) > TH_OPERATION)
            {
                OperationCount += -(deltaY);
            }

            if (OperationCount > TH_SETTING)
            {
                Debug.WriteLine("Going UP");
                // do animation: up
                RotateGear(GearAngle, GearAngle + GEAR_SPEED);
                GearAngle += GEAR_SPEED;

                // increment setting value
                SettingCount++;

                OperationCount = 0;
                return;
            }

            if (OperationCount < (-TH_SETTING))
            {
                Debug.WriteLine("Going DOWN");
                RotateGear(GearAngle, GearAngle -= GEAR_SPEED);
                GearAngle -= GEAR_SPEED;

                SettingCount--;

                OperationCount = 0;
                return;
            }

        }

        private void Gear_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            if (manager == null || manager.cameraStatus == null)
            {
                OperationCount = 0;
                SettingCount = 0;
                return;
            }

            // Ev
            if (manager.cameraStatus.IsAvailable("setExposureCompensation") && manager.cameraStatus.EvInfo != null)
            {
                var target = manager.cameraStatus.EvInfo.CurrentIndex + SettingCount;
                if (target < manager.cameraStatus.EvInfo.Candidate.MinIndex )
                {
                    manager.SetExposureCompensation(manager.cameraStatus.EvInfo.Candidate.MinIndex);
                }
                else if (target > manager.cameraStatus.EvInfo.Candidate.MaxIndex)
                {
                    manager.SetExposureCompensation(manager.cameraStatus.EvInfo.Candidate.MaxIndex);
                }
                else
                {
                    manager.SetExposureCompensation(target);
                }
            }


            OperationCount = 0;
            SettingCount = 0;
        }

        private void RotateGear(double from, double to)
        {
            var story = new Storyboard();
            var duration= new TimeSpan(0, 0, 0, 0, 150);
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
