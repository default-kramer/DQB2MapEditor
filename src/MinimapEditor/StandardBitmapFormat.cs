using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MinimapEditor;

/// <summary>
/// Defines the bitmap format to be used inside this application.
/// External images (like tilesheets) should be immediately converted to this format
/// on load; all other code can assume we are using this format.
/// </summary>
public static class StandardBitmapFormat
{
    public static readonly PixelFormat PixelFormat = PixelFormats.Pbgra32;
    public const int BytesPerPixel = 4;
    public const int DpiX = 96;
    public const int DpiY = 96;

    public static WriteableBitmap CreateWriteableBitmap(int pixelWidth, int pixelHeight)
    {
        return new WriteableBitmap(pixelWidth, pixelHeight, DpiX, DpiY, PixelFormat, null);
    }

    public static BitmapSource ConvertIfNecessary(BitmapSource source)
    {
        if (source.Format == PixelFormat)
        {
            return source;
        }

        var converted = new FormatConvertedBitmap();
        converted.BeginInit();
        converted.Source = source;
        converted.DestinationFormat = PixelFormat;
        converted.EndInit();
        return converted;
    }

    /// <remarks>
    /// There's a known bug where instantiating too many RenderTargetBitmap instances
    /// too quickly causes an exception (something about GDI handles not being freed, I think).
    /// Operating directly on the bytes avoids this problem and is probably faster.
    /// </remarks>
    public static void Composite(Span<byte> destination, ReadOnlySpan<byte> source)
    {
        for (int i = 0; i < destination.Length; i += 4)
        {
            byte sb = source[i];
            byte sg = source[i + 1];
            byte sr = source[i + 2];
            byte sa = source[i + 3];

            if (sa == 0)
                continue;

            if (sa == 255)
            {
                destination[i] = sb;
                destination[i + 1] = sg;
                destination[i + 2] = sr;
                destination[i + 3] = 255;
                continue;
            }

            byte db = destination[i];
            byte dg = destination[i + 1];
            byte dr = destination[i + 2];
            byte da = destination[i + 3];

            int invA = 255 - sa;

            // Pbgra32 is premultiplied, so RGB values are already multiplied by their alpha.
            destination[i] = (byte)(sb + db * invA / 255);
            destination[i + 1] = (byte)(sg + dg * invA / 255);
            destination[i + 2] = (byte)(sr + dr * invA / 255);
            destination[i + 3] = (byte)(sa + da * invA / 255);
        }
    }
}
