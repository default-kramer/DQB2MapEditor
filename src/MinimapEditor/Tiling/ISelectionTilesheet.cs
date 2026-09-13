
namespace MinimapEditor.Tiling;

public interface ISelectionTilesheet
{
    TileSize TileSize { get; }
    TileBytes SelectionTileA();
    TileBytes SelectionTileB();
    TileBytes TransparentTile();
}
