using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Diagnostics;

namespace Kazyx.WPMMM.Controls
{
    public partial class Histogram : UserControl
    {
        public enum ColorType
        {
            Red,
            Green,
            Blue,
        }

        private List<Rectangle> bars;
        private int MaxFrequency;
        private int Resolution;
        private double ScaleFactor;

        public Histogram()
        {
            InitializeComponent();


        }

        public void Init(ColorType type, int resolution, int maxLevel)
        {
            InitColorBar(type);
            InitBars(resolution, maxLevel);
        }

        private void InitBars(int resolution, int maxFrequency)
        {
            Resolution = resolution;
            MaxFrequency = maxFrequency;
            ScaleFactor = BarsStackPanel.ActualHeight / (double)maxFrequency * 10;

            double barWidth = (double)LayoutRoot.ActualWidth / (double)resolution;
                        
            var barBrush = new SolidColorBrush();
            barBrush.Color = Color.FromArgb(255, 255, 255, 255);
            
            for (int i = 0; i < Resolution; i++)
            {
                var rect = new Rectangle()
                {
                    VerticalAlignment = System.Windows.VerticalAlignment.Bottom,
                    Margin = new Thickness(0),
                    Height = 0,
                    Width = barWidth,
                    Fill = barBrush,
                    StrokeThickness = 0,
                };
                BarsStackPanel.Children.Add(rect);
            }

            Debug.WriteLine("");
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
                default:
                    colorBarBrush.Color = Color.FromArgb(255, 0, 0, 0);
                    break;
            }
            ColorBar.Fill = colorBarBrush;
        }

        public void SetHistogramValue(int[] values)
        {
            for (int i = 0; i < BarsStackPanel.Children.Count; i++)
            {
                var rect = BarsStackPanel.Children.ElementAt(i) as Rectangle;
                if (i < values.Length)
                {
                    var barHeight = ScaleFactor * values[i];
                    if (barHeight > BarsStackPanel.ActualHeight)
                    {
                        barHeight = BarsStackPanel.ActualHeight;
                    }
                    else
                    {
                        rect.Height = ScaleFactor * values[i];
                    }
                }
            }
        }

    }
}
