using LibDQB;
using System.Windows.Media.Imaging;

namespace MinimapEditor.Tiling;

/// <summary>
/// Just the bytes (no metadata) of a bitmap in the <see cref="StandardBitmapFormat"/>.
/// </summary>
public readonly struct TileBytes
{
    public readonly ReadOnlyMemory<byte> Memory;
    public TileBytes(ReadOnlyMemory<byte> memory)
    {
        this.Memory = memory;
    }

    public ReadOnlySpan<byte> Span => Memory.Span;
    public bool IsEmpty => Memory.IsEmpty;
    public static readonly TileBytes Empty = new(ReadOnlyMemory<byte>.Empty);

    public WriteableBitmap CreateBitmap(TileSize tileSize)
    {
        var dest = TileCanvas.Create(tileSize, new XZ(1, 1));
        using (var writer = dest.MakeWriter())
        {
            writer.DrawTile(this, XZ.Zero);
        }
        return dest.Bitmap;
    }
}
