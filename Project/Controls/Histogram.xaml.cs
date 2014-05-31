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
            White,
        }

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
            ScaleFactor = BarsGrid.ActualHeight / (double)maxFrequency * 5;

            double barWidth = (double)LayoutRoot.ActualWidth / (double)resolution;
            Debug.WriteLine("width " + LayoutRoot.ActualWidth + " " + barWidth);

            var barBrush = new SolidColorBrush();
            barBrush.Color = Color.FromArgb(255, 255, 255, 255);


            for (int i = 0; i < Resolution; i++)
            {
                BarsGrid.ColumnDefinitions.Add(new ColumnDefinition()
                {
                    Width = new GridLength(1, GridUnitType.Star),
                });

                var rect = new Rectangle()
                {
                    VerticalAlignment = System.Windows.VerticalAlignment.Bottom,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                    Margin = new Thickness(0),
                    Height = 0,
                    Fill = barBrush,
                    StrokeThickness = 0,
                };
                BarsGrid.Children.Add(rect);
                Grid.SetColumn(rect, i);
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
                case ColorType.White:
                    colorBarBrush.Color = Color.FromArgb(255, 255, 255, 255);
                    break;
                default:
                    colorBarBrush.Color = Color.FromArgb(255, 0, 0, 0);
                    break;
            }
            ColorBar.Fill = colorBarBrush;
        }

        public void SetHistogramValue(int[] values)
        {
            for (int i = 0; i < BarsGrid.Children.Count; i++)
            {
                var rect = BarsGrid.Children.ElementAt(i) as Rectangle;
                if (i < values.Length)
                {
                    var barHeight = ScaleFactor * values[i];
                    if (barHeight > BarsGrid.ActualHeight)
                    {
                        barHeight = BarsGrid.ActualHeight;
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
