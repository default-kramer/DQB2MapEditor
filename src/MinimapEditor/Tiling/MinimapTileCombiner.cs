using LibDQB.DQB2Minimap;

namespace MinimapEditor.Tiling;

/// <summary>
/// Combines visibility+overlay+baseTile into a single <see cref="TileBytes"/>.
/// Mostly done on-demand when <see cref="Get(MinimapTile)"/> is called
/// because there are many combinations that will never be used.
/// </summary>
/// <remarks>
/// Compared to having 3 separate WriteableBitmaps for each layer, this approach
/// should save about 256 MiB per island. Per WriteableBitmap, we have:
///    256 * 256  // number of tiles
///    * 16 * 16  // pixels per tile
///    * 4        // bytes per pixel
///    * 2        // front and back buffer
/// This multiplication gives us 128 MiB per WritableBitmap, and we
/// can multiply by 2 again because we're saving 2 WritableBitmaps per island.
///
/// Task Manager confirmed this -- with 8 islands opened I saw memory usage
/// of 4881 MB (this approach) vs 7006 MB (3-layer approach)
/// which is about 265MB per island and 133MB per WriteableBitmap.
/// And any speed difference is too small for me to measure.
/// </remarks>
public sealed class MinimapTileCombiner
{
    public TileSize TileSize { get; }
    private readonly IReadOnlyList<CacheEntry> cache;

    public MinimapTileCombiner(IMinimapTilesheet tilesheet)
    {
        TileSize = tilesheet.TileSize;

        // Prepopulate this first caching layer with all Overlay+Visibility combinations
        // because there are so few of them.
        this.cache = BuildCache(tilesheet);
    }

    private static IReadOnlyList<CacheEntry> BuildCache(IMinimapTilesheet tilesheet)
    {
        var overlayIds = Enumerable.Range(0, OverlayId.MaxValue + 1)
            .Select(i => new OverlayId(i))
            .ToList();

        var cache = new CacheEntry[overlayIds.Count * 2];
        foreach (var overlayId in overlayIds)
        {
            var oTile = tilesheet.GetOverlay(overlayId);
            foreach (bool visible in Util.Array(true, false))
            {
                var vTile = tilesheet.GetVisibility(visible);
                cache[GetIndex(visible, overlayId)] = new CacheEntry
                {
                    ovTile = Composite(oTile, vTile),
                    Tilesheet = tilesheet,
                };
            }
        }
        return cache;
    }

    private static int GetIndex(bool visible, OverlayId overlayId)
    {
        int adder = visible ? 0 : OverlayId.MaxValue + 1;
        return overlayId + adder;
    }

    public TileBytes Get(MinimapTile tile)
    {
        int index = GetIndex(tile.IsVisible, tile.ApparentOverlayId);
        return cache[index].Get(tile.BaseTileId);
    }

    sealed class CacheEntry
    {
        /// <summary>
        /// The combined overlay and visiblity
        /// </summary>
        public required TileBytes ovTile { get; init; }
        public required IMinimapTilesheet Tilesheet { get; init; }
        private readonly List<TileBytes> subcache = new();

        public TileBytes Get(BaseTileId baseId)
        {
            int index = baseId.Value;
            while (subcache.Count <= index)
            {
                subcache.Add(TileBytes.Empty);
            }

            var result = subcache[index];
            if (result.IsEmpty)
            {
                result = Composite(Tilesheet.GetBaseTile(baseId), ovTile);
                subcache[index] = result;
            }

            return result;
        }
    }

    internal static TileBytes Composite(TileBytes destination, TileBytes source)
    {
        var dst = destination.Memory.ToArray();
        StandardBitmapFormat.Composite(dst, source.Span);
        return new TileBytes(dst);
    }
}
