using LibDQB;
using LibDQB.DQB2Minimap;
using MinimapEditor.Viewmodels;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MinimapEditor;

/// <summary>
/// Creates and manages the WriteableBitmaps having the same size which will
/// be stacked on top of each other to produce the main UI.
/// </summary>
sealed class BitmapRepainter<TLayer> : MapEditorViewmodel.IRepainter
    , WpfMinimapGrid.IRepainter
    , SelectionGridDecorator.IRepainter
    , MapEditorViewmodel.IImageExporter
    where TLayer : BitmapSource
{
    public interface ITilesheet
    {
        /// <summary>
        /// Create a new image that can hold exactly the number of tiles specified
        /// by <paramref name="width"/> and <paramref name="height"/>.
        /// </summary>
        TLayer CreateLayer(int width, int height);

        void UpdateBaseTileLayer(TLayer layer, IReadOnlyGrid<MinimapTile> map, Rect dirty);
        void UpdateOverlayLayer(TLayer layer, IReadOnlyGrid<MinimapTile> map, Rect dirty);
        void UpdateVisibilityLayer(TLayer layer, IReadOnlyGrid<MinimapTile> map, Rect dirty);
        void UpdateSelectionLayer(TLayer layerA, TLayer layerB, IReadOnlyGrid<bool> selectionGrid, Rect dirty);
    }

    private readonly ITilesheet tilesheet;
    private readonly TLayer layerBase;
    private readonly TLayer layerOverlay;
    private readonly TLayer layerVisibility;
    // Selection will be drawn on 2 layers. The second (layer B) will blink so that it appears
    // to be toggling between layer A and layer B.
    private readonly TLayer layerSelectionA;
    private readonly TLayer layerSelectionB;

    public BitmapRepainter(ITilesheet tilesheet)
    {
        this.tilesheet = tilesheet;
        const int size = 256;
        layerBase = tilesheet.CreateLayer(size, size);
        layerOverlay = tilesheet.CreateLayer(size, size);
        layerVisibility = tilesheet.CreateLayer(size, size);
        layerSelectionA = tilesheet.CreateLayer(size, size);
        layerSelectionB = tilesheet.CreateLayer(size, size);
    }

    IEnumerable<ImageSource> MapEditorViewmodel.IRepainter.AllLayers()
    {
        yield return layerBase;
        yield return layerOverlay;
        yield return layerVisibility;
        yield return layerSelectionA;
        yield return layerSelectionB;
    }

    void WpfMinimapGrid.IRepainter.Repaint(IReadOnlyGrid<MinimapTile> grid, Rect dirty)
    {
        tilesheet.UpdateBaseTileLayer(layerBase, grid, dirty);
        tilesheet.UpdateOverlayLayer(layerOverlay, grid, dirty);
        tilesheet.UpdateVisibilityLayer(layerVisibility, grid, dirty);
    }

    void SelectionGridDecorator.IRepainter.Repaint(IReadOnlyGrid<bool> selectionGrid, Rect dirty)
    {
        tilesheet.UpdateSelectionLayer(layerSelectionA, layerSelectionB, selectionGrid, dirty);
    }

    BitmapFrame MapEditorViewmodel.IImageExporter.ExportFullImage()
    {
        return Composite(layerBase, layerOverlay, layerVisibility);
    }

    BitmapFrame MapEditorViewmodel.IImageExporter.ExportCroppedImage(IReadOnlyGrid<MinimapTile> grid)
    {
        grid = grid.TranslateTo(new XZ(0, 0));
        int width = grid.Bounds.Size.X;
        int height = grid.Bounds.Size.Z;

        var layerBase = tilesheet.CreateLayer(width, height);
        tilesheet.UpdateBaseTileLayer(layerBase, grid, grid.Bounds);

        var layerOverlay = tilesheet.CreateLayer(width, height);
        tilesheet.UpdateOverlayLayer(layerOverlay, grid, grid.Bounds);

        var layerVisibility = tilesheet.CreateLayer(width, height);
        tilesheet.UpdateVisibilityLayer(layerVisibility, grid, grid.Bounds);

        return Composite(layerBase, layerOverlay, layerVisibility);
    }

    private static BitmapFrame Composite(params TLayer[] layers)
    {
        int width = layers[0].PixelWidth;
        int height = layers[0].PixelHeight;

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            foreach (var bitmap in layers)
            {
                dc.DrawImage(bitmap, new System.Windows.Rect(0, 0, width, height));
            }
        }

        var composite = new RenderTargetBitmap(width, height, layers[0].DpiX, layers[0].DpiY, PixelFormats.Pbgra32);
        composite.Render(visual);
        return BitmapFrame.Create(composite);
    }
}
