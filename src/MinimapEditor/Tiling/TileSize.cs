using LibDQB;

namespace MinimapEditor.Tiling;

public sealed record TileSize
{
    public required int PixelWidth { get; init; }
    public required int PixelHeight { get; init; }
    public int Stride => PixelWidth * StandardBitmapFormat.BytesPerPixel;
    public int BytesPerTile => PixelHeight * Stride;
    public XZ AsXZ => new(PixelWidth, PixelHeight);
}
