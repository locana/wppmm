using Microsoft.Phone.Controls;
using System;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Kazyx.WPPMM.Controls
{
    public class ScrollDetectableLongListSelector : LongListSelector
    {
        private ViewportControl _ViewportControl;

        public event EventHandler<ViewportChangedEventArgs> InnerViewportChanged;

        public event EventHandler<ManipulationDeltaEventArgs> InnerManipulationDelta;

        public event EventHandler<ManipulationStateChangedEventArgs> InnerManipulationStateChanged;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _ViewportControl = GetTemplateChild("ViewportControl") as ViewportControl;
            _ViewportControl.ManipulationLockMode = ManipulationLockMode.HorizontalOrVertical;
            _ViewportControl.ViewportChanged += OnViewportChanged;
            _ViewportControl.ManipulationDelta += OnManipulationDelta;
            _ViewportControl.ManipulationStateChanged += OnManipulationStateChanged;
        }

        private void OnViewportChanged(object sender, ViewportChangedEventArgs e)
        {
            if (InnerViewportChanged != null)
            {
                InnerViewportChanged(sender, e);
            }
        }

        private void OnManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            if (InnerManipulationDelta != null)
            {
                InnerManipulationDelta(sender, e);
            }
        }

        private void OnManipulationStateChanged(object sender, ManipulationStateChangedEventArgs e)
        {
            if (InnerManipulationStateChanged != null)
            {
                InnerManipulationStateChanged(sender, e);
            }
        }
    }
}
