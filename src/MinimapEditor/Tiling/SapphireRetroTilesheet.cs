using LibDQB;
using LibDQB.DQB2Minimap;
using System.Windows.Media;

namespace MinimapEditor.Tiling;

public sealed class SapphireRetroTilesheet : Tilesheet
    , DataDefinitions.ITilesheet
    , IMinimapTilesheet
    , ISelectionTilesheet
{
    public static readonly SapphireRetroTilesheet Instance = new();

    private const int HiddenTileIndex = 32 * 31; // first tile of final row
    private const int OverlayStartIndex = HiddenTileIndex + 1;
    private const int TransparentTileIndex = OverlayStartIndex; // overlay 0 is transparent
    private const int SelectionTileIndex = 32 * 32 - 1; // last tile of last row
    private readonly ImageSource?[] baseTileCache;
    private readonly ImageSource?[] overlayCache;
    public readonly Lazy<MinimapTileCombiner> Combiner;

    private SapphireRetroTilesheet() : base("SheetRetro.png", new XZ(32, 32))
    {
        Combiner = new Lazy<MinimapTileCombiner>(() => new MinimapTileCombiner(this));

        baseTileCache = new ImageSource[BaseTileId.MaxLegalValue + 1];
        baseTileCache.AsSpan().Fill(null);

        overlayCache = new ImageSource[OverlayId.MaxValue + 1];
        overlayCache.AsSpan().Fill(null);
    }

    public TileBytes GetBaseTile(BaseTileId baseId)
    {
        int baseIndex = baseId.IsLegal ? baseId.Value : (BaseTileId.MaxLegalValue + 1);
        return GetTileBytes(baseIndex);
    }

    public TileBytes GetOverlay(OverlayId overlayId)
    {
        return GetTileBytes(OverlayStartIndex + overlayId);
    }

    public TileBytes GetVisibility(bool visible)
    {
        int index = visible ? TransparentTileIndex : HiddenTileIndex;
        return GetTileBytes(index);
    }

    public TileBytes SelectionTileA() => GetTileBytes(SelectionTileIndex);
    public TileBytes SelectionTileB() => GetTileBytes(SelectionTileIndex - 1);
    public TileBytes TransparentTile() => GetTileBytes(TransparentTileIndex);

    public ImageSource GetBaseTileImage(BaseTileId baseTileId)
    {
        int index = baseTileId.Value;
        if (index < 0 || index >= baseTileCache.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(baseTileId));
        }
        var img = baseTileCache[index];
        if (img == null)
        {
            img = GetBaseTile(baseTileId).CreateBitmap(this.TileSize);
            img.Freeze();
            baseTileCache[index] = img;
        }
        return img;
    }

    public ImageSource GetOverlayImage(OverlayId overlayId)
    {
        var img = overlayCache[overlayId];
        if (img == null)
        {
            img = GetOverlay(overlayId).CreateBitmap(this.TileSize);
            img.Freeze();
            overlayCache[overlayId] = img;
        }
        return img;
    }
}
