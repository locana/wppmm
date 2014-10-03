using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Shapes;
using System.Windows.Media;
using Kazyx.ImageStream.FocusInfo;
using Kazyx.WPPMM.Utils;

namespace Kazyx.WPPMM.Controls
{
    public partial class FocusFrames : UserControl
    {
        public FocusFrames()
        {
            InitializeComponent();
        }

        public void ClearFrames()
        {
            this.LayoutRoot.Children.Clear();
        }

        private static readonly Brush FocusedBrush = (Brush)Application.Current.Resources["PhoneAccentBrush"];
        private static readonly Brush NormalBrush = (Brush)Application.Current.Resources["PhoneBackgroundBrush"];
        private static readonly Brush MainBrush = (Brush)Application.Current.Resources["PhoneForegroundBrush"];
        private static readonly Brush SubBrush = (Brush)Application.Current.Resources["PhoneBackgroundBrush"];
        private static readonly double FaceFrameStrokeThickness = 1.5;
        private static readonly double FrameStrokeThickness = 2.5;
        private static readonly double FrameOpacity = 0.7;

        void AddFrame(double X1, double X2, double Y1, double Y2, Brush StrokeBrush, double StrokeThickness, bool DottedBorder = false)
        {
            DebugUtil.Log("[FocusFrames] " + X1 + " " + X2 + " " + Y1 + " " + Y2);
            var r = new Rectangle()
            {
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                Width = X2 - X1,
                Height = Y2 - Y1,
                Margin = new Thickness(X1, Y1, 0, 0),
                Stroke = StrokeBrush,
                Opacity = FrameOpacity,
                StrokeThickness = StrokeThickness,
            };

            if (DottedBorder)
            {
                var d = new DoubleCollection();
                d.Add(4);
                r.StrokeDashArray = d;
            }

            this.LayoutRoot.Children.Add(r);
        }

        public void SetFocusFrames(List<FocusFrameInfo> frames)
        {
            this.ClearFrames();

            foreach (FocusFrameInfo info in frames)
            {
                double x1 = (double)info.TopLeft_X / 10000 * LayoutRoot.ActualWidth;
                double x2 = (double)info.BottomRight_X / 10000 * LayoutRoot.ActualWidth;
                double y1 = (double)info.TopLeft_Y / 10000 * LayoutRoot.ActualHeight;
                double y2 = (double)info.BottomRight_Y / 10000 * LayoutRoot.ActualHeight;

                var brush = NormalBrush;
                switch (info.Category)
                {
                    case Category.ContrastAF:
                    case Category.Tracking:
                    case Category.PhaseDetectionAF:
                        
                        switch (info.Status)
                        {
                            case Status.Focused:
                                brush = FocusedBrush;
                                break;
                            case Status.Normal:
                                brush = NormalBrush;
                                break;
                        }
                        this.AddFrame(x1, x2, y1, y2, brush, FrameStrokeThickness, info.AdditionalStatus == AdditionalStatus.LargeFrame);
                        break;
                    case Category.Face:
                        switch (info.Status)
                        {
                            case Status.Focused:
                                brush = FocusedBrush;
                                break;
                            case Status.Main:
                                brush = MainBrush;
                                break;
                            case Status.Sub:
                                brush = SubBrush;
                                break;
                        }
                        this.AddFrame(x1, x2, y1, y2, brush, FaceFrameStrokeThickness);
                        break;
                }
            }
        }
    }
}
