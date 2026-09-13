using LibDQB.DQB2Minimap;

namespace MinimapEditor.Tiling;

public interface IMinimapTilesheet
{
    TileSize TileSize { get; }
    TileBytes GetBaseTile(BaseTileId baseTileId);
    TileBytes GetOverlay(OverlayId overlayId);
    TileBytes GetVisibility(bool visible);
}
