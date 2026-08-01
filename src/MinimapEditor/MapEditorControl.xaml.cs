using LibDQB;
using LibDQB.B2;
using LibDQB.B2.Records;
using LibDQB.DQB2Minimap;
using MinimapEditor.Viewmodels;
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
using System.Windows.Threading;

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

    private static System.Windows.Rect? GetInitialZoom(IReadOnlyGrid<MinimapTile> grid)
    {
        var xzs = grid.Bounds.Enumerate().Where(xz => grid.Get(xz).IsVisible).ToList();
        if (xzs.Count == 0)
        {
            return null;
        }

        var rect = LibDQB.Rect.GetBounds(xzs);
        double x0 = (0.0 + rect.Start.X) / grid.Bounds.Size.X;
        double x1 = (0.0 + rect.End.X) / grid.Bounds.Size.X;
        double y0 = (0.0 + rect.Start.Z) / grid.Bounds.Size.Z;
        double y1 = (0.0 + rect.End.Z) / grid.Bounds.Size.Z;
        double w = x1 - x0;
        double h = y1 - y0;
        double size = Math.Max(w, h);
        double dx = Math.Min(0, w - size) / 2;
        double dy = Math.Min(0, h - size) / 2;
        x0 = Math.Clamp(x0 + dx, 0, 1.0 - size);
        y0 = Math.Clamp(y0 + dy, 0, 1.0 - size);
        return new System.Windows.Rect(x0, y0, size, size);
    }

    private void MapEditorControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (bitmapGrid != null)
        {
            bitmapGrid.Children.Clear();
            ReloadBitmaps();
        }
        ResetZoom(viewmodel?.Grid());
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
            foreach (var bitmap in viewmodel.BitmapLayers.Bitmaps())
            {
                var image = new Image();
                image.Source = bitmap;
                image.Stretch = Stretch.None;
                grid.Children.Add(image);
            }
        }
    }

    private void Button_Click_1(object sender, RoutedEventArgs e) => viewmodel?.SaveCmndat();

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

    private void ResetZoom(object sender, RoutedEventArgs e) => ResetZoom(viewmodel?.Grid());

    private void ResetZoom(IReadOnlyGrid<MinimapTile>? grid)
    {
        if (grid != null)
        {
            var zoom = GetInitialZoom(grid);
            if (zoom.HasValue)
            {
                Zoomer.SetZoom(zoom.Value);
            }
        }
    }
}
