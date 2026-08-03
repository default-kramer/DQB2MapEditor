using LibDQB;
using LibDQB.DQB2Minimap;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MinimapEditor;

sealed class SapphireRetroTilesheet : BitmapRepainter<WriteableBitmap>.ITilesheet, DataDefinitions.ITilesheet
{
    sealed record TilesheetItem(BitmapSource Bitmap, XZ PositionInTilesheet);

    private readonly IReadOnlyList<TilesheetItem> tiles;
    private readonly TilesheetItem shroudTile;
    private readonly TilesheetItem transparentTile;
    private readonly TilesheetItem selectionTileA;
    private readonly TilesheetItem selectionTileB;
    private readonly BitmapImage tilesetImage;
    private readonly byte[] sheetPixels;
    private readonly int sheetStride;
    private const int TileSize = 16;
    private const int BytesPerPixel = 4;
    private const int HiddenTileIndex = 32 * 31; // first tile of final row
    private const int OverlayStartIndex = HiddenTileIndex + 1;
    private const int SelectionTileIndex = 32 * 32 - 1; // last tile of last row

    private static BitmapImage Load(string filename)
    {
        var path = System.IO.Path.Combine(AppContext.BaseDirectory, filename);
        return new BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute));
    }

    public static readonly SapphireRetroTilesheet Instance = new();

    private SapphireRetroTilesheet()
    {
        tilesetImage = Load("SheetRetro.png");

        sheetStride = tilesetImage.PixelWidth * 4;
        sheetPixels = new byte[sheetStride * tilesetImage.PixelHeight];
        tilesetImage.CopyPixels(sheetPixels, sheetStride, 0);

        tiles = ExtractTiles(tilesetImage, TileSize, TileSize);

        shroudTile = tiles[HiddenTileIndex];
        transparentTile = tiles[OverlayStartIndex]; // Overlay=0 must be transparent
        selectionTileA = tiles[SelectionTileIndex];
        selectionTileB = tiles[SelectionTileIndex - 1];
    }

    /// <summary>
    /// Extracts tiles from a tileset image into a list of Bitmaps.
    /// </summary>
    private static List<TilesheetItem> ExtractTiles(BitmapSource tileset, int tileWidth, int tileHeight)
    {
        var extractedTiles = new List<TilesheetItem>();
        for (int y = 0; y < tileset.PixelHeight; y += tileHeight)
        {
            for (int x = 0; x < tileset.PixelWidth; x += tileWidth)
            {
                var tileRect = new Int32Rect(x, y, tileWidth, tileHeight);
                var croppedBitmap = new CroppedBitmap(tileset, tileRect);
                croppedBitmap.Freeze();
                extractedTiles.Add(new TilesheetItem(croppedBitmap, new XZ(x / tileWidth, y / tileHeight)));
            }
        }
        return extractedTiles;
    }

    public WriteableBitmap CreateLayer(int width, int height)
    {
        return new WriteableBitmap(width * TileSize, height * TileSize, 96, 96, tilesetImage.Format, tilesetImage.Palette);
    }

    private BitmapWriter MakeWriter(WriteableBitmap layer) => new(layer, sheetPixels, sheetStride);

    private TilesheetItem GetItem(BaseTileId baseId)
    {
        int baseIndex = baseId.IsLegal ? baseId.Value : (BaseTileId.MaxLegalValue + 1);
        return tiles[baseIndex];
    }

    private TilesheetItem GetItem(OverlayId overlayId)
    {
        return tiles[OverlayStartIndex + overlayId];
    }

    public ImageSource GetBaseTileImage(BaseTileId id) => GetItem(id).Bitmap;
    public ImageSource GetOverlayImage(OverlayId id) => GetItem(id).Bitmap;

    public void UpdateBaseTileLayer(WriteableBitmap layer, IReadOnlyGrid<MinimapTile> map, LibDQB.Rect dirty)
    {
        using var writer = MakeWriter(layer);
        foreach (var xz in dirty.Enumerate())
        {
            var tile = map.Get(xz);
            var item = GetItem(tile.BaseTileId);
            writer.DrawTile(item, xz);
        }
    }

    public void UpdateOverlayLayer(WriteableBitmap layer, IReadOnlyGrid<MinimapTile> map, LibDQB.Rect dirty)
    {
        using var writer = MakeWriter(layer);
        foreach (var xz in dirty.Enumerate())
        {
            var tile = map.Get(xz);
            var item = GetItem(tile.ApparentOverlayId);
            writer.DrawTile(item, xz);
        }
    }

    public void UpdateVisibilityLayer(WriteableBitmap layer, IReadOnlyGrid<MinimapTile> map, LibDQB.Rect dirty)
    {
        using var writer = MakeWriter(layer);
        foreach (var xz in dirty.Enumerate())
        {
            var tile = map.Get(xz);
            var item = tile.IsVisible ? transparentTile : shroudTile;
            writer.DrawTile(item, xz);
        }
    }

    public void UpdateSelectionLayer(WriteableBitmap layerA, WriteableBitmap layerB, IReadOnlyGrid<bool> selectionGrid, LibDQB.Rect dirty)
    {
        using var writerA = MakeWriter(layerA);
        using var writerB = MakeWriter(layerB);

        foreach (var xz in dirty.Enumerate())
        {
            bool isSelected = selectionGrid.Get(xz);
            var tileA = isSelected ? selectionTileA : transparentTile;
            var tileB = isSelected ? selectionTileB : transparentTile;
            if ((xz.X + xz.Z) % 2 == 0)
            {
                (tileA, tileB) = (tileB, tileA);
            }
            writerA.DrawTile(tileA, xz);
            writerB.DrawTile(tileB, xz);
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

        public void DrawTile(TilesheetItem tile, XZ mapPosition) => DrawTile(tile.PositionInTilesheet, mapPosition);

        private void DrawTile(XZ tilePosition, XZ mapPosition)
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
