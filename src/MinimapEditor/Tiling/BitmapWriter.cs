using LibDQB;
using System.Windows;
using System.Windows.Media.Imaging;

namespace MinimapEditor.Tiling;

unsafe readonly ref struct BitmapWriter : IDisposable
{
    private readonly TileSize tileSize;
    private readonly WriteableBitmap map;
    private readonly byte* destBase;

    private int TileWidth => tileSize.PixelWidth;
    private int TileHeight => tileSize.PixelHeight;
    private int TileStride => tileSize.Stride;

    public BitmapWriter(WriteableBitmap map, TileSize tileSize)
    {
        this.tileSize = tileSize;
        this.map = map;
        map.Lock();
        destBase = (byte*)map.BackBuffer;
    }

    public void DrawTile(TileBytes tile, XZ mapPosition)
    {
        const int BytesPerPixel = StandardBitmapFormat.BytesPerPixel;
        const int srcX = 0;
        const int srcZ = 0;
        int dstX = mapPosition.X * TileWidth;
        int dstZ = mapPosition.Z * TileHeight;

        fixed (byte* srcBase = tile.Span)
        {
            for (int row = 0; row < TileHeight; row++)
            {
                byte* src = srcBase + (srcZ + row) * TileStride + srcX * BytesPerPixel;
                byte* dst = destBase + (dstZ + row) * map.BackBufferStride + dstX * BytesPerPixel;
                Buffer.MemoryCopy(src, dst, TileStride, TileStride);
            }
        }

        map.AddDirtyRect(new Int32Rect(dstX, dstZ, TileWidth, TileHeight));
    }

    public void Dispose()
    {
        map.Unlock();
    }
}
