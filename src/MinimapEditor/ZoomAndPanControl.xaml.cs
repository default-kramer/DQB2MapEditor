using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MinimapEditor;

/// <summary>
/// Interaction logic for ZoomAndPanControl.xaml
/// </summary>
public partial class ZoomAndPanControl : UserControl
{
    public static readonly DependencyProperty ZoomableContentProperty =
        DependencyProperty.Register("ZoomableContent", typeof(object), typeof(ZoomAndPanControl), new PropertyMetadata(null));

    // MousePosition[X/Y] properties should only be settable from inside this control.
    // The pattern is that RegisterReadOnly returns a "key" which is needed to set the property's value.
    private static readonly DependencyPropertyKey MousePositionXPropertyKey
        = DependencyProperty.RegisterReadOnly("MousePositionX", typeof(double), typeof(ZoomAndPanControl), new PropertyMetadata(-1.0));
    public static readonly DependencyPropertyKey MousePositionYPropertyKey =
        DependencyProperty.RegisterReadOnly("MousePositionY", typeof(double), typeof(ZoomAndPanControl), new PropertyMetadata(-1.0));

    /// <summary>
    /// Ranges from 0.0 to 1.0 when the mouse is over the zoomable content.
    /// Value is relative to the entire content; that is, it adjusts for any zooming/panning.
    /// </summary>
    public static readonly DependencyProperty MousePositionXProperty = MousePositionXPropertyKey.DependencyProperty;

    /// <summary>
    /// Same as <see cref="MousePositionXProperty"/>.
    /// </summary>
    public static readonly DependencyProperty MousePositionYProperty = MousePositionYPropertyKey.DependencyProperty;

    public object ZoomableContent
    {
        get { return GetValue(ZoomableContentProperty); }
        set { SetValue(ZoomableContentProperty, value); }
    }

    public double MousePositionX { get => (double)GetValue(MousePositionXProperty); }
    public double MousePositionY { get => (double)GetValue(MousePositionYProperty); }

    private double contentWidth;
    private double contentHeight;

    public ZoomAndPanControl()
    {
        InitializeComponent();
        this.Loaded += ZoomAndPanControl_Loaded;
        this.LayoutUpdated += ZoomAndPanControl_LayoutUpdated;
    }

    private void ZoomAndPanControl_LayoutUpdated(object? sender, EventArgs e)
    {
        if (contentWidth != measurementViewer.ExtentWidth || contentHeight != measurementViewer.ExtentHeight)
        {
            TrySetContentSize();
        }
    }

    private bool hasLoaded = false;
    private Rect? requestedInitialZoom;

    private void ZoomAndPanControl_Loaded(object sender, RoutedEventArgs e)
    {
        theBrush.Visual = contentHost;

        Dispatcher.BeginInvoke(new Action(() =>
        {
            TrySetContentSize();

            // Nasty timing issue, but this works (for now, at least):
            hasLoaded = true;
            if (requestedInitialZoom.HasValue)
            {
                ZoomTo(requestedInitialZoom.Value);
                requestedInitialZoom = null;
            }
        }), System.Windows.Threading.DispatcherPriority.ContextIdle);
    }

    private void TrySetContentSize()
    {
        contentWidth = measurementViewer.ExtentWidth;
        contentHeight = measurementViewer.ExtentHeight;

        if (contentWidth > 0 && contentHeight > 0)
        {
            ResetZoom();
        }
    }

    private static readonly Rect NoZoom = new(0, 0, 1, 1);

    public bool IsZoomed() => theBrush.Viewbox != NoZoom;

    private void ResetZoom()
    {
        if (contentWidth > 0 && contentHeight > 0)
        {
            theBrush.Viewbox = NoZoom;
            viewboxContentGrid.Width = contentWidth;
            viewboxContentGrid.Height = contentHeight;
        }
    }

    private void ResetZoom_Click(object sender, RoutedEventArgs e)
    {
        ResetZoom();
    }

    public void SetZoom(Rect zoom)
    {
        if (!hasLoaded)
        {
            requestedInitialZoom = zoom;
        }
        else
        {
            ZoomTo(zoom);
        }
    }

    private void ZoomTo(Rect zoom)
    {
        theBrush.Viewbox = zoom;
        viewboxContentGrid.Width = contentWidth * zoom.Width;
        viewboxContentGrid.Height = contentHeight * zoom.Height;
    }

    private void viewboxContentGrid_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Released)
        {
            StopDragging();
        }

        Point currentPoint = e.GetPosition(viewboxContentGrid);

        if (draggingFrom.HasValue)
        {
            var box = theBrush.Viewbox;
            var dx = currentPoint.X - draggingFrom.Value.X;
            var dy = currentPoint.Y - draggingFrom.Value.Y;
            dx /= -viewboxContentGrid.Width;
            dy /= -viewboxContentGrid.Height;
            dx *= box.Width;
            dy *= box.Height;
            var newX = Math.Clamp(box.X + dx, 0, 1.0 - box.Width);
            var newY = Math.Clamp(box.Y + dy, 0, 1.0 - box.Height);
            ZoomTo(new Rect(newX, newY, box.Width, box.Height));
            draggingFrom = currentPoint;
        }
        else
        {
            var x = currentPoint.X / viewboxContentGrid.ActualWidth;
            var y = currentPoint.Y / viewboxContentGrid.ActualHeight;
            x = theBrush.Viewbox.Left + x * theBrush.Viewbox.Width;
            y = theBrush.Viewbox.Top + y * theBrush.Viewbox.Height;
            SetValue(MousePositionXPropertyKey, x);
            SetValue(MousePositionYPropertyKey, y);
        }
    }

    private void MouseWheelZoom(object sender, MouseWheelEventArgs e)
    {
        var zoom = DoMouseWheelZoom(e, theBrush.Viewbox, theViewbox);
        if (zoom.HasValue)
        {
            ZoomTo(zoom.Value);
        }
    }

    private static Rect? DoMouseWheelZoom(MouseWheelEventArgs e, Rect currentZoom, FrameworkElement element)
    {
        const double zoomStep = 0.02;
        const double zoomLimitX = 0.05;
        const double zoomLimitY = 0.05;

        (double, double) GetPos()
        {
            var pos = e.MouseDevice.GetPosition(element);
            var x = Math.Clamp(pos.X / element.ActualWidth, 0, 1);
            var y = Math.Clamp(pos.Y / element.ActualHeight, 0, 1);
            return (x, y);
        }

        if (e.Delta > 0 && (currentZoom.Size.Width > zoomLimitX || currentZoom.Size.Height > zoomLimitY))
        {
            var (scaleX, scaleY) = GetPos();

            // zooming in cannot cause out-of-bounds
            var w = Math.Max(zoomLimitX, currentZoom.Width - zoomStep);
            var h = Math.Max(zoomLimitY, currentZoom.Height - zoomStep);
            var x = currentZoom.X + zoomStep * scaleX;
            var y = currentZoom.Y + zoomStep * scaleY;
            return new Rect(x, y, w, h);
        }
        if (e.Delta < 0 && (currentZoom.Size.Width < 1 || currentZoom.Size.Height < 1))
        {
            var (scaleX, scaleY) = GetPos();

            // zooming out must check for out-of-bounds
            var w = Math.Min(1, currentZoom.Width + zoomStep);
            var h = Math.Min(1, currentZoom.Height + zoomStep);
            var x = Math.Max(0, currentZoom.X - zoomStep * scaleX);
            var y = Math.Max(0, currentZoom.Y - zoomStep * scaleY);
            if (x + w > 1)
            {
                x = 1 - w;
            }
            if (y + h > 1)
            {
                y = 1 - h;
            }
            return new Rect(x, y, w, h);
        }

        return null;
    }

    private Point? draggingFrom = null;

    private void viewboxContentGrid_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (theBrush.Viewbox != NoZoom)
        {
            draggingFrom = e.GetPosition(viewboxContentGrid);
        }
    }

    private void viewboxContentGrid_MouseUp(object sender, MouseButtonEventArgs e) => StopDragging();
    private void viewboxContentGrid_MouseLeave(object sender, MouseEventArgs e) => StopDragging();

    private void StopDragging()
    {
        draggingFrom = null;
    }
}
