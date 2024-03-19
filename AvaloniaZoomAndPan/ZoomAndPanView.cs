using Avalonia;
using Avalonia.Controls;
using Avalonia.Dialogs.Internal;
using Avalonia.Input;
using Avalonia.Media;
using System.Linq;

namespace AvaloniaZoomAndPan
{
    /// <summary>
    /// View for enabling zooming and panning for the child of this control
    /// Controls:
    /// -   MouseWheel: Zoom
    /// -   MouseLeftButtonPressed i.c.w. MouseMoving: Panning
    /// -   MiddleMouseButton: Reset the View
    /// Properties:
    /// -   MaxZoom: value between 0.0 and 1.0 default: 0.2
    /// -   ZoomSpeed: value between 0.0 and 1.0 default: 0.05
    /// -   ZoomEnabled: bool  
    /// -   PanEnabled: bool
    /// </summary>
    public class ZoomAndPanView : Border
    {
        Control child = null;
        Point origin;
        Point start;
        bool isPanning = false;

        public override void ApplyTemplate()
        {
            base.ApplyTemplate();


            var c = this.VisualChildren.FirstOrDefault();
            if (c != null)
            {
                child = c as Control;
                TransformGroup group = new TransformGroup();
                ScaleTransform st = new ScaleTransform();
                group.Children.Add(st);
                TranslateTransform tt = new TranslateTransform();
                group.Children.Add(tt);
                child.RenderTransform = group;
                child.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Relative);
                child.PointerWheelChanged += OnPointerWheelChanged;
                child.PointerPressed += OnPointerPressed;
                child.PointerReleased += OnPointerReleased;
                child.PointerMoved += OnPointerMoved;
            }
        }

        private TranslateTransform GetTranslateTransform(Visual element)
        {
            return (TranslateTransform)((TransformGroup)element.RenderTransform)
              .Children.First(tr => tr is TranslateTransform);
        }
        private ScaleTransform GetScaleTransform(Visual element)
        {
            return (ScaleTransform)((TransformGroup)element.RenderTransform)
              .Children.First(tr => tr is ScaleTransform);
        }


        #region Dependency Properties

        /// <summary>
        /// Value between 0.0 and 1.0
        /// </summary>
        public double MaxZoom
        {
            get { return GetValue(MaxZoomProperty); }
            set { SetValue(MaxZoomProperty, value); }
        }

        public static readonly StyledProperty<double> MaxZoomProperty =
            AvaloniaProperty.Register<ZoomAndPanView, double>("MaxZoom", 0.2);


        public double ZoomSpeed
        {
            get { return GetValue(ZoomSpeedProperty); }
            set { SetValue(ZoomSpeedProperty, value); }
        }

        public static readonly StyledProperty<double> ZoomSpeedProperty =
            AvaloniaProperty.Register<ZoomAndPanView, double>("ZoomSpeed", 0.05);
        public bool ZoomEnabled
        {
            get { return GetValue(ZoomEnabledProperty); }
            set { SetValue(ZoomEnabledProperty, value); }
        }

        public static readonly StyledProperty<bool> ZoomEnabledProperty =
            AvaloniaProperty.Register<ZoomAndPanView, bool>("ZoomEnabled", true);

        public bool PanEnabled
        {
            get { return GetValue(PanEnabledProperty); }
            set { SetValue(PanEnabledProperty, value); }
        }

        public static readonly StyledProperty<bool> PanEnabledProperty =
            AvaloniaProperty.Register<ZoomAndPanView, bool>("PanEnabled", true);


        #endregion



        private void OnPointerWheelChanged(object sender, PointerWheelEventArgs e)
        {
            if (child != null && ZoomEnabled)
            {
                var st = GetScaleTransform(child);
                var tt = GetTranslateTransform(child);

                double zoom = e.Delta.Y > 0 ? ZoomSpeed : -ZoomSpeed;
                if (!(e.Delta.Y > 0) && (st.ScaleX < MaxZoom || st.ScaleY < MaxZoom))
                    return;

                Point relative = e.GetPosition(child);
                double absoluteX;
                double absoluteY;

                absoluteX = relative.X * st.ScaleX + tt.X;
                absoluteY = relative.Y * st.ScaleY + tt.Y;

                st.ScaleX += zoom;
                st.ScaleY += zoom;

                tt.X = absoluteX - relative.X * st.ScaleX;
                tt.Y = absoluteY - relative.Y * st.ScaleY;
            }
        }

        private void OnPointerPressed(object sender, PointerPressedEventArgs e)
        {
            isPanning = true;
            if (child != null && PanEnabled)
            {
                var tt = GetTranslateTransform(child);
                start = e.GetPosition(this);
                origin = new Point(tt.X, tt.Y);
                Cursor = new Cursor(StandardCursorType.Hand);
            }
        }

        private void OnPointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (child != null)
            {
                Cursor = new Cursor(StandardCursorType.Arrow);
            }
            isPanning = false;


            // View can be reset by pressing the middle mouse button
            if (e.InitialPressMouseButton == MouseButton.Middle)
                ResetView();
        }


        private void OnPointerMoved(object sender, PointerEventArgs e)
        {
            if (child != null && isPanning && child.IsPointerOver)
            {
                var tt = GetTranslateTransform(child);
                Vector v = start - e.GetPosition(this);
                tt.X = origin.X - v.X;
                tt.Y = origin.Y - v.Y;
            }
        }
        private void ResetView()
        {
            if (child == null || Parent == null)
                return;

            var parent = this;

            // Trigger a Measure for the controls dimensions
            child.InvalidateMeasure();
            parent.InvalidateMeasure();


            // Reset zoom
            double scale = child.Width / (parent.Width);
            var st = GetScaleTransform(child);
            st.ScaleX = scale;
            st.ScaleY = scale;

            // Reset pan
            double transX = (child.Width - parent.Width * scale) / 2;
            double transY = (child.Height - parent.Height * scale) / 2;
            var tt = GetTranslateTransform(child);
            tt.X = transX;
            tt.Y = transY;

        }

    }
}
