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
                    colorBarBrush.Color = Color.FromArgb(255, 255, 255, 255);
                    break;
                default:
                    colorBarBrush.Color = Color.FromArgb(255, 0, 0, 0);
                    break;
            }
            ColorBar.Fill = colorBarBrush;
        }

        private const int X_SKIP_ORDER = 4;

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
    }
}
