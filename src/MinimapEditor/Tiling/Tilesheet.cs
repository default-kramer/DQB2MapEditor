using LibDQB;
using System.Windows;
using System.Windows.Media.Imaging;

namespace MinimapEditor.Tiling;

public abstract class Tilesheet
{
    protected readonly ReadOnlyMemory<byte> tileMemory;
    private readonly XZ tilesPerSheet;
    public TileSize TileSize { get; }

    public Tilesheet(string filename, XZ tilesPerSheet)
    {
        if (tilesPerSheet.X < 1 || tilesPerSheet.Z < 1)
        {
            throw new ArgumentException(nameof(tilesPerSheet));
        }

        this.tileMemory = Load(filename, tilesPerSheet, out var tileSize);
        this.tilesPerSheet = tilesPerSheet;
        this.TileSize = tileSize;
    }

    private static ReadOnlyMemory<byte> Load(string filename, XZ tilesPerSheet, out TileSize tileSize)
    {
        var path = System.IO.Path.Combine(AppContext.BaseDirectory, filename);
        var raw = new BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute));

        var rawSize = new XZ(raw.PixelWidth, raw.PixelHeight);
        var tileSizeXZ = rawSize.Unscale(tilesPerSheet);

        if (tileSizeXZ.X < 1 || tileSizeXZ.Z < 1)
        {
            throw new Exception("Invalid combination of tilesheet bitmap and declared size");
        }

        if (tileSizeXZ.Scale(tilesPerSheet) != rawSize)
        {
            // Tilesheet bitmap was not divisible by size, but we can cope
            Util.SoftAssertFail();
        }

        tileSize = new TileSize
        {
            PixelHeight = tileSizeXZ.X,
            PixelWidth = tileSizeXZ.Z,
        };

        var converted = StandardBitmapFormat.ConvertIfNecessary(raw);
        return RearrangeMemory(converted, tileSize, tilesPerSheet);
    }

    /// <summary>
    /// Rearranges the memory of the tilesheet such that we can get all the bytes of
    /// a single tile using a single Slice()
    /// </summary>
    private static ReadOnlyMemory<byte> RearrangeMemory(BitmapSource tilesheet, TileSize tileSize, XZ tilesPerSheet)
    {
        var dstBuffer = new byte[tilesPerSheet.X * tilesPerSheet.Z * tileSize.BytesPerTile];
        int dstOffset = 0;
        var tileSizeXZ = tileSize.AsXZ;

        foreach (var xz in new LibDQB.Rect(XZ.Zero, tilesPerSheet).Enumerate())
        {
            var srcRect = new Int32Rect(xz.Scale(tileSizeXZ).X, xz.Scale(tileSizeXZ).Z, tileSizeXZ.X, tileSizeXZ.Z);
            tilesheet.CopyPixels(srcRect, dstBuffer, tileSize.Stride, dstOffset);
            dstOffset += tileSize.BytesPerTile;
        }

        if (dstOffset != dstBuffer.Length)
        {
            throw new Exception("Assert fail");
        }

        return dstBuffer;
    }

    protected TileBytes GetTileBytes(XZ positionInTilesheet)
    {
        int index = new LibDQB.Rect(XZ.Zero, tilesPerSheet).GetIndex(positionInTilesheet)
            ?? throw new ArgumentException(nameof(positionInTilesheet));
        return GetTileBytes(index);
    }

    protected TileBytes GetTileBytes(int index)
    {
        var memory = tileMemory.Slice(index * TileSize.BytesPerTile, TileSize.BytesPerTile);
        return new TileBytes(memory);
    }
}
