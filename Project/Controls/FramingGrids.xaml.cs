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
using System.Diagnostics;
using Kazyx.WPPMM.DataModel;

namespace Kazyx.WPMMM.Controls
{
    public partial class FramingGrids : UserControl
    {
        public Brush Stroke { get; set; }
        public double StrokeThickness { get; set; }

        private string _Type = FramingGridTypes.Off;
        public string Type
        {
            get { return _Type; }
            set
            {
                if (value != _Type)
                {
                    _Type = value;
                    this.DrawGridLines(value);
                }
            }
        }

        public static readonly DependencyProperty GridTypeProperty = DependencyProperty.Register(
            "Type",
            typeof(string),
            typeof(FramingGrids),
            new PropertyMetadata(new PropertyChangedCallback(FramingGrids.OnGridTypeChanged)));

        public static void OnGridTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Debug.WriteLine("[FramingGrids]Type changed: " + (string)e.NewValue);
            (d as FramingGrids).Type = (string)e.NewValue;
        }

        public FramingGrids()
        {
            InitializeComponent();
            if (this.Stroke == null)
            {
                Stroke = new SolidColorBrush() { Color = Color.FromArgb(200, 200, 200, 200) };
            }
            StrokeThickness = 1.0;


        }

        private void Clear()
        {
            Lines.Children.Clear();
        }

        private void DrawGridLines(string t)
        {
            double w = LayoutRoot.ActualWidth;
            double h = LayoutRoot.ActualHeight;

            this.Clear();

            switch (t)
            {
                case FramingGridTypes.RuleOfThirds:
                    DrawLine(w / 3, w / 3, 0, h);
                    DrawLine(2 * w / 3, 2 * w / 3, 0, h);
                    DrawLine(0, w, h / 3, h / 3);
                    DrawLine(0, w, 2 * h / 3, 2 * h / 3);
                    break;

                default:
                    break;
            }
        }

        private void DrawLine(double x1, double x2, double y1, double y2)
        {
            Debug.WriteLine("draw line: " + x1 + " " + x2 + " " + y1 + " " + y2);

            double minX = StrokeThickness / 2;
            double maxX = LayoutRoot.ActualWidth - minX;
            double minY = StrokeThickness / 2;
            double maxY = LayoutRoot.ActualHeight - minY;

            x1 = RoundToRange(x1, minX, maxX);
            x2 = RoundToRange(x2, minX, maxX);
            y1 = RoundToRange(y1, minY, maxY);
            y2 = RoundToRange(y2, minY, maxY);

            var line = new Line()
            {
                Stroke = this.Stroke,
                StrokeThickness = this.StrokeThickness,
                X1 = x1,
                X2 = x2,
                Y1 = y1,
                Y2 = y2,

            };
            Lines.Children.Add(line);
        }

        private static double RoundToRange(double value, double min, double max)
        {
            if (value < min) { return min; }
            if (value > max) { return max; }
            return value;
        }

        private void Lines_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.Clear();
            DrawGridLines(_Type);
        }
    }
}
