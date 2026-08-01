using LibDQB;
using MinimapEditor.Viewmodels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Imaging;

namespace MinimapEditor;

/// <summary>
/// Holds WriteableBitmaps having the same size which will be stacked on top of each
/// other to produce the main UI.
/// </summary>
sealed class BitmapLayers : MapEditorViewmodel.IBitmapLayers
    , WpfMinimapGrid.IBitmapLayers
    , SelectionGridDecorator.IBitmapRepainter
{
    private readonly WriteableBitmap bitmapBase = MinimapRenderer.TODO();
    private readonly WriteableBitmap bitmapOverlay = MinimapRenderer.TODO();
    private readonly WriteableBitmap bitmapShroud = MinimapRenderer.TODO();
    private readonly WriteableBitmap bitmapSelection = MinimapRenderer.TODO();

    IEnumerable<WriteableBitmap> MapEditorViewmodel.IBitmapLayers.Bitmaps()
    {
        yield return bitmapBase;
        yield return bitmapOverlay;
        yield return bitmapShroud;
        yield return bitmapSelection;
    }

    IEnumerable<(MinimapRenderer.TileLayer LayerId, WriteableBitmap Bitmap)> WpfMinimapGrid.IBitmapLayers.Layers()
    {
        yield return (MinimapRenderer.TileLayer.Base, bitmapBase);
        yield return (MinimapRenderer.TileLayer.Overlay, bitmapOverlay);
        yield return (MinimapRenderer.TileLayer.Shroud, bitmapShroud);
    }

    void SelectionGridDecorator.IBitmapRepainter.Repaint(IGrid<bool> selectionGrid, Rect dirty)
    {
        MinimapRenderer.UpdateSelection(bitmapSelection, selectionGrid, dirty);
    }
}
