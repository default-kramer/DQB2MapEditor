using LibDQB;
using LibDQB.DQB2Minimap;
using MinimapEditor.Tiling;
using MinimapEditor.Viewmodels;
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
    , MapEditorViewmodel.IImageExporter
{
    private readonly MinimapTileCombiner tileCombinator;
    private readonly TileCanvas minimapLayer;
    private readonly TileCanvas layerSelectionA;
    private readonly TileCanvas layerSelectionB;
    private readonly ISelectionTilesheet selectionTilesheet;

    public BitmapRepainter(MinimapTileCombiner combinator, ISelectionTilesheet selectionTilesheet)
    {
        this.tileCombinator = combinator;
        this.selectionTilesheet = selectionTilesheet;

        var numTiles = new XZ(256, 256);
        minimapLayer = TileCanvas.Create(tileCombinator.TileSize, numTiles);
        layerSelectionA = TileCanvas.Create(selectionTilesheet.TileSize, numTiles);
        layerSelectionB = TileCanvas.Create(selectionTilesheet.TileSize, numTiles);
    }

    IEnumerable<ImageSource> MapEditorViewmodel.IRepainter.AllLayers()
    {
        yield return minimapLayer.Bitmap;
        yield return layerSelectionA.Bitmap;
        yield return layerSelectionB.Bitmap;
    }

    void WpfMinimapGrid.IRepainter.Repaint(IReadOnlyGrid<MinimapTile> grid, Rect dirty)
    {
        using var writer = minimapLayer.MakeWriter();
        foreach (var xz in dirty.Enumerate())
        {
            var tile = grid.Get(xz);
            var bytes = this.tileCombinator.Get(tile);
            writer.DrawTile(bytes, xz);
        }
    }

    void SelectionGridDecorator.IRepainter.Repaint(IReadOnlyGrid<bool> selectionGrid, Rect dirty)
    {
        var selectionTileA = selectionTilesheet.SelectionTileA();
        var selectionTileB = selectionTilesheet.SelectionTileB();
        var transparentTile = selectionTilesheet.TransparentTile();

        using var writerA = layerSelectionA.MakeWriter();
        using var writerB = layerSelectionB.MakeWriter();

        foreach (var xz in dirty.Enumerate())
        {
            bool isSelected = selectionGrid.Get(xz);
            var tileA = isSelected ? selectionTileA : transparentTile;
            var tileB = isSelected ? selectionTileB : transparentTile;
            if ((xz.X + xz.Z) % 2 == 0)
            {
                (tileA, tileB) = (tileB, tileA);
            }
            writerA.DrawTile(tileA, xz);
            writerB.DrawTile(tileB, xz);
        }
    }

    BitmapFrame MapEditorViewmodel.IImageExporter.ExportFullImage()
    {
        return BitmapFrame.Create(minimapLayer.Bitmap);
    }

    BitmapFrame MapEditorViewmodel.IImageExporter.ExportCroppedImage(IReadOnlyGrid<MinimapTile> grid)
    {
        grid = grid.TranslateTo(new XZ(0, 0));
        int width = grid.Bounds.Size.X;
        int height = grid.Bounds.Size.Z;

        var canvas = TileCanvas.Create(tileCombinator.TileSize, grid.Bounds.Size);
        using (var writer = canvas.MakeWriter())
        {
            foreach (var xz in grid.Bounds.Enumerate())
            {
                var tile = tileCombinator.Get(grid.Get(xz));
                writer.DrawTile(tile, xz);
            }
        }

        return BitmapFrame.Create(canvas.Bitmap);
    }
}
