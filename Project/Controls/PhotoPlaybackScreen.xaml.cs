using Kazyx.WPPMM.Utils;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Kazyx.WPPMM.Controls
{
    public partial class PhotoPlaybackScreen : UserControl
    {

        public PhotoPlaybackScreen()
        {
            InitializeComponent();
        }

        public Visibility DetailInfoVisibility
        {
            get { return DetailInfoPanel.Visibility; }
            set { DetailInfoPanel.Visibility = value; }
        }

        public static readonly DependencyProperty MinProperty = DependencyProperty.Register(
            "SourceBitmap",
            typeof(BitmapImage),
            typeof(PhotoPlaybackScreen),
            new PropertyMetadata(new PropertyChangedCallback(PhotoPlaybackScreen.OnSourceBitmapUpdated)));

        private static void OnSourceBitmapUpdated(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as PhotoPlaybackScreen).SourceBitmap = (BitmapImage)e.NewValue;
        }

        const double MaxScale = 1.0;

        double _scale = 1.0;
        double _minScale;
        double _coercedScale;
        double _originalScale;

        Size _viewportSize;
        bool _pinching;
        Point _screenMidpoint;
        Point _relativeMidpoint;

        BitmapImage _SourceBitmap;
        public BitmapImage SourceBitmap
        {
            get { return _SourceBitmap; }
            set
            {
                _SourceBitmap = value;
                Image.Source = value;
            }
        }

        public void viewport_ManipulationStarted(object sender, System.Windows.Input.ManipulationStartedEventArgs e)
        {
            _pinching = false;
            _originalScale = _scale;
        }

        public void Init()
        {
            _scale = 0;
            CoerceScale(true);
            _scale = _coercedScale;
            _SourceBitmap = null;
            ResizeImage(true);
        }

        public void viewport_ManipulationDelta(object sender, System.Windows.Input.ManipulationDeltaEventArgs e)
        {
            if (e.PinchManipulation != null)
            {
                e.Handled = true;

                if (!_pinching)
                {
                    _pinching = true;
                    var center = e.PinchManipulation.Original.Center;
                    _relativeMidpoint = new Point(center.X / Image.ActualWidth, center.Y / Image.ActualHeight);

                    var xform = Image.TransformToVisual(viewport);
                    _screenMidpoint = xform.Transform(center);
                }

                _scale = _originalScale * e.PinchManipulation.CumulativeScale;

                CoerceScale(false);
                ResizeImage(false);
            }
            else if (_pinching)
            {
                _pinching = false;
                _originalScale = _scale = _coercedScale;
            }
        }

        public void viewport_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            _pinching = false;
            _scale = _coercedScale;
        }

        public void viewport_ViewportChanged(object sender, System.Windows.Controls.Primitives.ViewportChangedEventArgs e)
        {
            var newSize = new Size(viewport.Viewport.Width, viewport.Viewport.Height);
            if (newSize != _viewportSize)
            {
                _viewportSize = newSize;
                CoerceScale(true);
                ResizeImage(false);
            }
        }

        void ResizeImage(bool center)
        {
            if (_coercedScale != 0 && _SourceBitmap != null)
            {
                double newWidth = canvas.Width = Math.Round(_SourceBitmap.PixelWidth * _coercedScale);
                double newHeight = canvas.Height = Math.Round(_SourceBitmap.PixelHeight * _coercedScale);

                xform.ScaleX = xform.ScaleY = _coercedScale;

                viewport.Bounds = new Rect(0, 0, newWidth, newHeight);

                if (center)
                {
                    viewport.SetViewportOrigin(
                        new Point(
                            Math.Round((newWidth - viewport.ActualWidth) / 2),
                            Math.Round((newHeight - viewport.ActualHeight) / 2)
                            ));
                }
                else
                {
                    var newImgMid = new Point(newWidth * _relativeMidpoint.X, newHeight * _relativeMidpoint.Y);
                    var origin = new Point(newImgMid.X - _screenMidpoint.X, newImgMid.Y - _screenMidpoint.Y);
                    viewport.SetViewportOrigin(origin);
                }
            }
        }

        void CoerceScale(bool recompute)
        {
            if (recompute && _SourceBitmap != null && viewport != null)
            {
                // Calculate the minimum scale to fit the viewport 
                var minX = viewport.ActualWidth / _SourceBitmap.PixelWidth;
                var minY = viewport.ActualHeight / _SourceBitmap.PixelHeight;

                _minScale = Math.Min(minX, minY);
                DebugUtil.Log("Minimum scale: " + _minScale);
            }

            _coercedScale = Math.Min(MaxScale, Math.Max(_scale, _minScale));
        }

        internal void ReleaseImage()
        {
            this.Image.Source = null;
            _SourceBitmap = null;
        }
    }
}
