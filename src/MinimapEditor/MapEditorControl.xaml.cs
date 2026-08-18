using LibDQB;
using MinimapEditor.Viewmodels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MinimapEditor;

/// <summary>
/// Interaction logic for MapEditorControl.xaml
/// </summary>
public partial class MapEditorControl : UserControl
{
    private MapEditorViewmodel? viewmodel => DataContext as MapEditorViewmodel;
    private Grid? bitmapGrid;

    public MapEditorControl()
    {
        InitializeComponent();

        DataContextChanged += MapEditorControl_DataContextChanged;

        var handler = new EventHandler(OnMousePositionChanged);
        var px = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(ZoomAndPanControl.MousePositionXProperty, typeof(ZoomAndPanControl));
        px.AddValueChanged(Zoomer, handler);
        var py = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(ZoomAndPanControl.MousePositionYProperty, typeof(ZoomAndPanControl));
        py.AddValueChanged(Zoomer, handler);
    }

    private XZ MouseoverTile()
    {
        int x = (int)(Zoomer.MousePositionX * 256);
        int z = (int)(Zoomer.MousePositionY * 256);
        return new XZ(x, z);
    }

    private void OnMousePositionChanged(object? sender, EventArgs e)
    {
        var xz = MouseoverTile();
        viewmodel?.OnMousePositionChanged(xz);
    }

    private void MapEditorControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (bitmapGrid != null)
        {
            bitmapGrid.Children.Clear();
            ReloadBitmaps();
        }
    }

    private void Grid_Loaded(object sender, RoutedEventArgs e)
    {
        this.bitmapGrid = sender as Grid;
        ReloadBitmaps();
    }

    private void ReloadBitmaps()
    {
        var grid = bitmapGrid;
        if (grid == null || viewmodel == null)
        {
            return;
        }

        if (grid.Children.Count == 0)
        {
            foreach (var source in viewmodel.BitmapLayers.AllLayers())
            {
                var image = new Image();
                image.Source = source;
                image.Stretch = Stretch.None;
                grid.Children.Add(image);
            }

            // Assume last layer is selection layer which should blink
            MakeBlinking(grid.Children.OfType<Image>().Last());
        }
    }

    private static void MakeBlinking(Image image)
    {
        Duration blinkDuration = new Duration(TimeSpan.FromMilliseconds(200));

        var sb = new Storyboard();
        TimeSpan beginTime = TimeSpan.Zero;
        void Add(DoubleAnimation da)
        {
            da.BeginTime = beginTime;
            beginTime += da.Duration.TimeSpan;
            sb.Children.Add(da);
            Storyboard.SetTargetProperty(da, new PropertyPath(nameof(image.Opacity)));
            Storyboard.SetTarget(da, image);
        }

        Add(new DoubleAnimation()
        {
            From = 0.0,
            To = 0.0,
            Duration = blinkDuration,
        });
        Add(new DoubleAnimation()
        {
            From = 1.0,
            To = 1.0,
            Duration = blinkDuration,
        });

        sb.RepeatBehavior = RepeatBehavior.Forever;
        sb.Begin();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        viewmodel?.OnPreviewKeyDown(e.Key);
    }

    private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
    {
        viewmodel?.OnPreviewKeyUp(e.Key);
    }

    private void Zoomer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        viewmodel?.OnMouseEvent(e);
    }

    private void Zoomer_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        viewmodel?.OnMouseEvent(e);
    }

    private void Zoomer_MouseEnter(object sender, MouseEventArgs e)
    {
        viewmodel?.OnMouseEvent(e);
    }

    private void ResetZoom(object sender, RoutedEventArgs e)
    {
        if (viewmodel != null)
        {
            viewmodel.ResetZoom();
            Zoomer.SetZoom(viewmodel);
        }
    }

    private void NotYetImplemented(object sender, EventArgs e)
    {
        MessageBox.Show("Not yet implemented");
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        viewmodel?.CopySelectionToClipboard();
    }
}
