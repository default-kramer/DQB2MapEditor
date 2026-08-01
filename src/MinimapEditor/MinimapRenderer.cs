using LibDQB;
using LibDQB.DQB2Minimap;
using MinimapEditor.Viewmodels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MinimapEditor;

static class MinimapRenderer
{
    private static IReadOnlyList<TileImage> tiles;
    private static readonly BitmapImage tilesetImage;
    private static readonly byte[] sheetPixels;
    private static readonly int sheetStride;

    public static IReadOnlyList<BaseTileModel> BaseTiles;
    public static IReadOnlyList<OverlayModel> OverlayTiles;

    private static BitmapImage Load(string filename)
    {
        var path = System.IO.Path.Combine(AppContext.BaseDirectory, "SheetRetro.png");
        return new BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute));
    }

    static MinimapRenderer()
    {
        tilesetImage = Load("SheetRetro.png");

        sheetStride = tilesetImage.PixelWidth * 4;
        sheetPixels = new byte[sheetStride * tilesetImage.PixelHeight];
        tilesetImage.CopyPixels(sheetPixels, sheetStride, 0);

        tiles = ExtractTiles(tilesetImage, TileSize, TileSize);

        BaseTiles = tiles.Index().Take(26).Select(x => new BaseTileModel
        {
            ImageSource = x.Item.Bitmap,
            TileId = x.Index,
            Name = x.Index.ToString(),
        }).ToList();

        OverlayTiles = tiles.Index().Where(x => IsValidOverlay(x.Index)).Select(x => new OverlayModel
        {
            ImageSource = x.Item.Bitmap,
            OverlayIndex = x.Index - 993,
            Name = (x.Index - 993).ToString(),
        }).ToList();
    }

    private static bool IsValidOverlay(int index)
    {
        return index >= 993 && index < 993 + 11 && index != 993 + 4 && index != 993 + 5;
    }

    public static WriteableBitmap TODO()
    {
        return new WriteableBitmap(256 * TileSize, 256 * TileSize, 96, 96, tilesetImage.Format, tilesetImage.Palette);
    }

    private const int TileSize = 16;
    const int BytesPerPixel = 4;
    private const int HiddenTileIndex = 992;
    private const int OverlayStartIndex = 993;

    public static BitmapSource? RenderMinimap(IReadOnlyGrid<MinimapTile> map)
    {
        var bounds = map.Bounds;
        var size = bounds.Size;

        // Create a drawing visual to render onto
        int imageWidth = size.X * TileSize;
        int imageHeight = size.Z * TileSize;
        var drawingVisual = new DrawingVisual();

        using (var dc = drawingVisual.RenderOpen())
        {
            // Iterate through every tile in the map and draw it
            foreach (var mapXZ in bounds.Enumerate())
            {
                var tile = map.Get(mapXZ);

                var imageXZ = mapXZ.Subtract(bounds.Start);
                var positionRect = new System.Windows.Rect(imageXZ.X * TileSize, imageXZ.Z * TileSize, TileSize, TileSize);

                foreach (var img in GetImages(tile).FOO())
                {
                    dc.DrawImage(img.Bitmap, positionRect);
                }
            }
        }

        // Render the visual to a bitmap
        var finalBitmap = new RenderTargetBitmap(imageWidth, imageHeight, 96, 96, PixelFormats.Pbgra32);
        finalBitmap.Render(drawingVisual);
        finalBitmap.Freeze();

        return finalBitmap;
    }

    public enum TileLayer
    {
        Base = 0,
        Overlay = 1,
        Shroud = 2,
    };

    public sealed class TileImages
    {
        private readonly TileImage?[] layers = new TileImage?[4];

        private TileImage? this[TileLayer layer]
        {
            get => layers[(int)layer];
            set => layers[(int)layer] = value;
        }

        public TileImage? GetImage(TileLayer layer) => this[layer];

        public required TileImage? BaseImage
        {
            get => this[TileLayer.Base];
            init => this[TileLayer.Base] = value;
        }

        public required TileImage? OverlayImage
        {
            get => this[TileLayer.Overlay];
            init => this[TileLayer.Overlay] = value;
        }

        public required TileImage? ShroudImage
        {
            get => this[TileLayer.Shroud];
            init => this[TileLayer.Shroud] = value;
        }

        public IEnumerable<TileImage> FOO()
        {
            if (BaseImage != null) yield return BaseImage;
            if (OverlayImage != null) yield return OverlayImage;
            if (ShroudImage != null) yield return ShroudImage;
        }
    }

    public static TileImages GetImages(MinimapTile tile)
    {
        if (tile.TileValue == -1)
        {
            return new TileImages { BaseImage = null, OverlayImage = null, ShroudImage = null };
        }

        int baseIndex = tile.IsLegal ? tile.TileId : (MinimapTile.MaxLegalTileId + 1);
        var baseTile = tiles[baseIndex];

        // --- Draw Overlays ---
        // Draw TileType overlay (trees, rooms, etc.)
        TileImage? overlayTile = null;
        int overlayIndex = OverlayStartIndex + (tile.QuirkyOverlay ?? tile.TileType);
        if (overlayIndex < tiles.Count)
        {
            overlayTile = tiles[overlayIndex];
        }

        // Handle visibility: If we are not showing all tiles and the tile is not visible, draw the hidden tile on top.
        TileImage? shroudTile = null;
        if (!tile.IsVisible)
        {
            shroudTile = tiles[HiddenTileIndex];
        }

        return new TileImages
        {
            BaseImage = baseTile,
            OverlayImage = overlayTile,
            ShroudImage = shroudTile
        };
    }

    public sealed record TileImage(BitmapSource Bitmap, XZ PositionInTilesheet);

    /// <summary>
    /// Extracts tiles from a tileset image into a list of Bitmaps.
    /// </summary>
    private static List<TileImage> ExtractTiles(BitmapSource tileset, int tileWidth, int tileHeight)
    {
        var extractedTiles = new List<TileImage>();
        for (int y = 0; y < tileset.PixelHeight; y += tileHeight)
        {
            for (int x = 0; x < tileset.PixelWidth; x += tileWidth)
            {
                var tileRect = new Int32Rect(x, y, tileWidth, tileHeight);
                var croppedBitmap = new CroppedBitmap(tileset, tileRect);
                croppedBitmap.Freeze();
                extractedTiles.Add(new TileImage(croppedBitmap, new XZ(x / tileWidth, y / tileHeight)));
            }
        }
        return extractedTiles;
    }

    public static void UpdateSelection(WriteableBitmap bitmap, IReadOnlyGrid<bool> selection, LibDQB.Rect dirty)
    {
        var empty = tiles[HiddenTileIndex + 1]; // transparent (but is this guaranteed?)
        var selectionOverlay = tiles[tiles.Count - 1];

        using var writer = new BitmapWriter(bitmap, sheetPixels, sheetStride);

        foreach (var xz in dirty.Enumerate())
        {
            var tile = selection.Get(xz) ? selectionOverlay : empty;
            writer.DrawTile(tile.PositionInTilesheet, xz);
        }
    }

    public static void Update(WriteableBitmap bitmap, TileLayer layer, IReadOnlyGrid<MinimapTile> map, LibDQB.Rect dirty)
    {
        var empty = tiles[HiddenTileIndex + 1]; // transparent (but is this guaranteed?)

        using var writer = new BitmapWriter(bitmap, sheetPixels, sheetStride);

        foreach (var xz in dirty.Enumerate())
        {
            var tile = map.Get(xz);
            var images = GetImages(tile);
            var image = images.GetImage(layer) ?? empty;
            writer.DrawTile(image.PositionInTilesheet, xz);
        }
    }

    unsafe ref struct BitmapWriter : IDisposable
    {
        private readonly WriteableBitmap map;
        private readonly byte* destBase;
        private readonly byte[] tileSheetPixels;
        private readonly int tileSheetStride;

        public BitmapWriter(WriteableBitmap map, byte[] tileSheetPixels, int sheetStride)
        {
            this.map = map;
            this.tileSheetPixels = tileSheetPixels;
            this.tileSheetStride = sheetStride;

            map.Lock();
            destBase = (byte*)map.BackBuffer;
        }

        public void DrawTile(XZ tilePosition, XZ mapPosition)
        {
            int srcX = tilePosition.X * TileSize;
            int srcZ = tilePosition.Z * TileSize;

            int dstX = mapPosition.X * TileSize;
            int dstZ = mapPosition.Z * TileSize;

            fixed (byte* srcBase = tileSheetPixels)
            {
                for (int row = 0; row < TileSize; row++)
                {
                    byte* src =
                        srcBase +
                        (srcZ + row) * tileSheetStride +
                        srcX * BytesPerPixel;

                    byte* dst =
                        destBase +
                        (dstZ + row) * map.BackBufferStride +
                        dstX * BytesPerPixel;

                    Buffer.MemoryCopy(
                        src,
                        dst,
                        TileSize * BytesPerPixel,
                        TileSize * BytesPerPixel);
                }
            }

            map.AddDirtyRect(new Int32Rect(
                dstX,
                dstZ,
                TileSize,
                TileSize));
        }

        public void Dispose()
        {
            map.Unlock();
        }
    }
}
