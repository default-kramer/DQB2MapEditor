using LibDQB;
using System.Windows.Media.Imaging;

namespace MinimapEditor.Tiling;

readonly struct TileCanvas
{
    public required WriteableBitmap Bitmap { get; init; }
    public required TileSize TileSize { get; init; }

    public static TileCanvas Create(TileSize tileSize, XZ numberOfTiles)
    {
        var pixelSize = numberOfTiles.Scale(tileSize.AsXZ);
        var bitmap = StandardBitmapFormat.CreateWriteableBitmap(pixelSize.X, pixelSize.Z);
        return new TileCanvas { Bitmap = bitmap, TileSize = tileSize };
    }

    public BitmapWriter MakeWriter() => new BitmapWriter(this.Bitmap, this.TileSize);
}
