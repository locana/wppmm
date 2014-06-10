using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Kazyx.WPMMM.Controls
{
    public partial class Histogram : UserControl
    {
        public enum ColorType
        {
            Red,
            Green,
            Blue,
            White,
        }

        private int MaxFrequency;
        private double ScaleFactor;

        public Histogram()
        {
            InitializeComponent();
        }

        public void Init(ColorType type, int maxLevel)
        {
            InitColorBar(type);
            InitBars(maxLevel);
        }

        private void InitBars(int maxFrequency)
        {
            MaxFrequency = maxFrequency;
            ScaleFactor = BarsGrid.ActualHeight / (double)maxFrequency * 6;
        }

        private void InitColorBar(ColorType type)
        {
            var colorBarBrush = new SolidColorBrush();

            switch (type)
            {
                case ColorType.Red:
                    colorBarBrush.Color = Color.FromArgb(255, 255, 0, 0);
                    break;
                case ColorType.Green:
                    colorBarBrush.Color = Color.FromArgb(255, 0, 255, 0);
                    break;
                case ColorType.Blue:
                    colorBarBrush.Color = Color.FromArgb(255, 0, 0, 255);
                    break;
                case ColorType.White:
                    colorBarBrush.Color = Color.FromArgb(255, 160, 160, 160);
                    break;
                default:
                    colorBarBrush.Color = Color.FromArgb(255, 0, 0, 0);
                    break;
            }
            ColorBar.Fill = colorBarBrush;
        }

        private const int X_SKIP_ORDER = 2;
        private const int HISTOGRAM_PADDING_TOP = 2;

        public void SetHistogramValue(int[] values)
        {
            if (values == null)
            {
                return;
            }

            var rate = (int)(values.Length / BarsGrid.ActualWidth * X_SKIP_ORDER);

            var points = new PointCollection();

            // Left corner
            points.Add(new Point(0.0, BarsGrid.ActualHeight));

            for (int i = 0; i < BarsGrid.ActualWidth / X_SKIP_ORDER; i++)
            {
                var index = rate * i;
                if (index > values.Length - 1)
                {
                    index = values.Length - 1;
                }
                var barHeight = ScaleFactor * values[index];
                points.Add(new Point(i * X_SKIP_ORDER, BarsGrid.ActualHeight - Math.Min(BarsGrid.ActualHeight, barHeight)));
            }

            // Right corner
            points.Add(new Point(BarsGrid.ActualWidth, BarsGrid.ActualHeight));

            HistogramPolygon.Points = points;
        }

        public void SetHistogramValue(int[] valuesR, int[] valuesG, int[] valuesB)
        {
            if (valuesR == null || valuesG == null || valuesB == null)
            {
                return;
            }

            if (!(valuesR.Length == valuesG.Length && valuesG.Length == valuesB.Length))
            {
                return;
            }

            var rate = (int)(valuesR.Length / BarsGrid.ActualWidth * X_SKIP_ORDER);

            var pointsR = new PointCollection();
            var pointsG = new PointCollection();
            var pointsB = new PointCollection();

            var verticalResolution = BarsGrid.ActualWidth / X_SKIP_ORDER;
            var maxHistogramLevel = BarsGrid.ActualHeight - HISTOGRAM_PADDING_TOP;

            // pointsBG.Add(new Point(0.0, BarsGrid.ActualHeight));

            for (int i = 0; i < verticalResolution; i++)
            {
                var index = rate * i;

                if (index > valuesR.Length - 1)
                {
                    index = valuesR.Length - 1;
                }
                var barHeightR = ScaleFactor * valuesR[index];
                pointsR.Add(new Point(i * X_SKIP_ORDER, BarsGrid.ActualHeight - Math.Min(maxHistogramLevel, barHeightR)));

                var barHeightG = ScaleFactor * valuesG[index];
                pointsG.Add(new Point(i * X_SKIP_ORDER, BarsGrid.ActualHeight - Math.Min(maxHistogramLevel, barHeightG)));

                var barHeightB = ScaleFactor * valuesB[index];
                pointsB.Add(new Point(i * X_SKIP_ORDER, BarsGrid.ActualHeight - Math.Min(maxHistogramLevel, barHeightB)));
            }

            // Right corner
            /*
            pointsR.Add(new Point(BarsGrid.ActualWidth, BarsGrid.ActualHeight));
            pointsG.Add(new Point(BarsGrid.ActualWidth, BarsGrid.ActualHeight));
            pointsB.Add(new Point(BarsGrid.ActualWidth, BarsGrid.ActualHeight));
            */

            HistogramPolylineR.Points = pointsR;
            HistogramPolylineG.Points = pointsG;
            HistogramPolylineB.Points = pointsB;
            
        }
    }
}
