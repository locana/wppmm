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

        private const double GoldenRatio = 0.382;

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
                case FramingGridTypes.Crosshairs:
                    DrawLine(w / 2, w / 2, 0, h);
                    DrawLine(0, w, h / 2, h / 2);
                    break;
                case FramingGridTypes.Diagonal:
                    DrawLine(0, w, 0, h);
                    DrawLine(0, w, h, 0);
                    break;
                case FramingGridTypes.Fibonacci:
                    if (w > h)
                    {
                        DrawFibonacciSpiral(w, h);
                    }
                    break;
                case FramingGridTypes.GoldenRatio:
                    DrawLine(w * GoldenRatio, w * GoldenRatio, 0, h);
                    DrawLine(w * (1 - GoldenRatio), w * (1 - GoldenRatio), 0, h);
                    DrawLine(0, w, h * GoldenRatio, h * GoldenRatio);
                    DrawLine(0, w, h * (1 - GoldenRatio), h * (1 - GoldenRatio));    
                    break;
                case FramingGridTypes.Square:
                    if (w > h)
                    {
                        // only vertical lines.
                        DrawLine((w - h) / 2, (w - h) / 2, 0, h);
                        DrawLine(w - ((w - h) / 2), w - ((w - h) / 2), 0, h);
                    }
                    else if (h > w)
                    {
                        // horizontal lines
                        DrawLine((h - w) / 2, (h - w) / 2, 0, w);
                        DrawLine(h - ((h - w) / 2), h - ((h - w) / 2), 0, w);
                    }
                    break;
                case FramingGridTypes.Off:
                default:
                    break;
            }
        }

        private void DrawFibonacciSpiral(double w, double h)
        {
            Debug.WriteLine("draw fibonaci: " + w + " " + h);

            PathFigure figure = new PathFigure();
            figure.StartPoint = new Point(0, 0);

            var x1 = 0.0;
            var y1 = 0.0;

            var x2 = 0.0;
            var y2 = h;

            var x3 = w * (1 - GoldenRatio);
            var y3 = h;

            for (int i = 0; i < 10; i++)
            {
                Debug.WriteLine("Bezier: " + x1 + " " + y1 + " / " + x2 + " " + y2 + " / " + x3 + " " + y3);
                var seg = new BezierSegment();
                seg.Point1 = new Point(x1, y1);
                seg.Point2 = new Point(x2, y2);
                seg.Point3 = new Point(x3, y3);
                figure.Segments.Add(seg);

                x1 = x3;
                y1 = y3;

                switch (i % 4)
                {
                    case 0: // lower right
                        w = w * GoldenRatio;
                        x2 = x1 + w;
                        y2 = y1;
                        x3 = x1 + w;
                        y3 = y1 - h * (1 - GoldenRatio);
                        break;
                    case 1:
                        h = h * GoldenRatio;
                        x2 = x1;
                        y2 = y1 - h;
                        x3 = x1 - w * (1 - GoldenRatio);
                        y3 = y1 - h;
                        break;
                    case 2:
                        w = w * GoldenRatio;
                        x2 = x1 - w;
                        y2 = y1;
                        x3 = x1 - w;
                        y3 = y1 + h * (1 - GoldenRatio);
                        break;
                    case 3:
                        h = h * GoldenRatio;
                        x2 = x1;
                        y2 = y1 + h;
                        x3 = x1 + w * (1 - GoldenRatio);
                        y3 = y1 + h;
                        break;
                }

            }
            
            PathFigureCollection pthFigureCollection = new PathFigureCollection();
            pthFigureCollection.Add(figure);

            PathGeometry pthGeometry = new PathGeometry();
            pthGeometry.Figures = pthFigureCollection;

            Path Fibonacci = new Path();
            Fibonacci.Stroke = Stroke = this.Stroke;
            Fibonacci.StrokeThickness = this.StrokeThickness;
            Fibonacci.Data = pthGeometry;
            Lines.Children.Add(Fibonacci);
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

        private void DrawArcSegment(Point start, Point end, SweepDirection dir)
        {
            Debug.WriteLine("draw arc: " + start.X + " " + start.Y + " " + end.X + " " + end.Y + " " + dir);

            PathFigure pthFigure = new PathFigure();
            pthFigure.StartPoint = start;

            ArcSegment arcSeg = new ArcSegment();
            arcSeg.Point = end;
            arcSeg.Size = new Size(Math.Abs(start.X - end.X), Math.Abs(start.Y - end.Y));
            arcSeg.IsLargeArc = false;
            arcSeg.SweepDirection = dir;
            arcSeg.RotationAngle = 90;

            PathSegmentCollection myPathSegmentCollection = new PathSegmentCollection();
            myPathSegmentCollection.Add(arcSeg);

            pthFigure.Segments = myPathSegmentCollection;

            PathFigureCollection pthFigureCollection = new PathFigureCollection();
            pthFigureCollection.Add(pthFigure);

            PathGeometry pthGeometry = new PathGeometry();
            pthGeometry.Figures = pthFigureCollection;

            Path arcPath = new Path();
            arcPath.Stroke = this.Stroke;
            arcPath.StrokeThickness = this.StrokeThickness;
            arcPath.Data = pthGeometry;
            Lines.Children.Add(arcPath);
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
