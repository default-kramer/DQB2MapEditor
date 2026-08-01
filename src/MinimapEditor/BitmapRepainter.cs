using LibDQB;
using LibDQB.DQB2Minimap;
using MinimapEditor.Viewmodels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MinimapEditor;

/// <summary>
/// Creates and manages the WriteableBitmaps having the same size which will
/// be stacked on top of each other to produce the main UI.
/// </summary>
sealed class BitmapRepainter : MapEditorViewmodel.IRepainter
    , WpfMinimapGrid.IRepainter
    , SelectionGridDecorator.IRepainter
{
    private readonly WriteableBitmap bitmapBase = MinimapRenderer.TODO();
    private readonly WriteableBitmap bitmapOverlay = MinimapRenderer.TODO();
    private readonly WriteableBitmap bitmapShroud = MinimapRenderer.TODO();
    private readonly WriteableBitmap bitmapSelection = MinimapRenderer.TODO();

    IEnumerable<ImageSource> MapEditorViewmodel.IRepainter.AllLayers()
    {
        yield return bitmapBase;
        yield return bitmapOverlay;
        yield return bitmapShroud;
        yield return bitmapSelection;
    }

    void WpfMinimapGrid.IRepainter.Repaint(IReadOnlyGrid<MinimapTile> grid, Rect dirty)
    {
        MinimapRenderer.Update(bitmapBase, MinimapRenderer.TileLayer.Base, grid, dirty);
        MinimapRenderer.Update(bitmapOverlay, MinimapRenderer.TileLayer.Overlay, grid, dirty);
        MinimapRenderer.Update(bitmapShroud, MinimapRenderer.TileLayer.Shroud, grid, dirty);
    }

    void SelectionGridDecorator.IRepainter.Repaint(IReadOnlyGrid<bool> selectionGrid, Rect dirty)
    {
        MinimapRenderer.UpdateSelection(bitmapSelection, selectionGrid, dirty);
    }
}
