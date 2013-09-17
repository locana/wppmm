using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using WPPMMComp;

namespace WPPMM
{
    public partial class SamplePage : PhoneApplicationPage
    {
        private Direct3DInterop m_d3dInterop = null;

        // コンストラクター
        public SamplePage()
        {
            InitializeComponent();
        }

        private void DrawingSurface_Loaded(object sender, RoutedEventArgs e)
        {
            if (m_d3dInterop == null)
            {
                m_d3dInterop = new Direct3DInterop();

                // ウィンドウの境界をピクセル単位で設定
                m_d3dInterop.WindowBounds = new Windows.Foundation.Size(
                    (float)DrawingSurface.ActualWidth,
                    (float)DrawingSurface.ActualHeight
                    );

                // ネイティブの解像度をピクセル単位で設定
                m_d3dInterop.NativeResolution = new Windows.Foundation.Size(
                    (float)Math.Floor(DrawingSurface.ActualWidth * Application.Current.Host.Content.ScaleFactor / 100.0f + 0.5f),
                    (float)Math.Floor(DrawingSurface.ActualHeight * Application.Current.Host.Content.ScaleFactor / 100.0f + 0.5f)
                    );

                // レンダリング解像度を完全なネイティブな解像度に設定
                m_d3dInterop.RenderResolution = m_d3dInterop.NativeResolution;

                // ネイティブ コンポーネントを DrawingSurface に接続
                DrawingSurface.SetContentProvider(m_d3dInterop.CreateContentProvider());
                DrawingSurface.SetManipulationHandler(m_d3dInterop);
            }
        }
    }
}