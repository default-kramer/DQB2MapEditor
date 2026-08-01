using LibDQB;
using LibDQB.B2;
using LibDQB.B2.Records;
using LibDQB.DQB2Minimap;
using MinimapEditor.Viewmodels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MinimapEditor;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, StartupViewmodel.ICallback
{
    public MainWindow()
    {
        InitializeComponent();

        var control = new StartupControl();
        var vm = new StartupViewmodel(this);
        control.DataContext = vm;
        SetContent(control);
    }

    private void SetContent(UIElement element)
    {
        mainGrid.Children.Clear();
        mainGrid.Children.Add(element);
        element.Focusable = true;
        element.Focus();
    }

    public void OpenMap(string cmndatPath, RawCommonData cmndat, IMinimap minimap)
    {
        if (1.ToString() == "nope")
        {
            PutTestPattern(minimap);
        }

        var layers = new Dictionary<MinimapRenderer.TileLayer, WriteableBitmap>();
        layers[MinimapRenderer.TileLayer.Base] = MinimapRenderer.TODO();
        layers[MinimapRenderer.TileLayer.Overlay] = MinimapRenderer.TODO();
        layers[MinimapRenderer.TileLayer.Shroud] = MinimapRenderer.TODO();

        var tileDecorator = new WpfMinimapGrid
        {
            Grid = minimap,
            Layers = layers,
            Dispatcher = this.Dispatcher,
        };

        tileDecorator.RefreshAll();

        var selectionGrid = new Array2D<bool>(minimap.Bounds, false);

        var selectionLayer = MinimapRenderer.TODO();

        var selectionDecorator = new SelectionGridDecorator
        {
            SelectionGrid = selectionGrid,
            SelectionBitmap = selectionLayer,
        };
        selectionDecorator.Refresh(selectionGrid.Bounds);

        var viewmodel = new MapEditorViewmodel(tileDecorator, selectionDecorator)
        {
            Cmndat = cmndat,
            CmndatPath3902 = cmndatPath,
            Layers = layers,
            SelectionLayer = selectionLayer,
        };

        var control = new MapEditorControl();
        control.DataContext = viewmodel;
        SetContent(control);

        if (1.ToString() == "nope")
        {
            for (byte i = IslandId.IoA.Value; i <= IslandId.Buildertopia3.Value; i++)
            {
                ValidateShores(cmndat.GetMinimap(new IslandId(i)));
            }
        }
    }

    /// <summary>
    /// Decorates a selection grid and refreshes the <see cref="SelectionBitmap"/>
    /// whenever the underlying grid is modified.
    /// </summary>
    sealed class SelectionGridDecorator : IGrid<bool>
    {
        public required IGrid<bool> SelectionGrid { get; init; }
        public required WriteableBitmap SelectionBitmap { get; init; }

        public LibDQB.Rect Bounds => SelectionGrid.Bounds;

        public bool Get(XZ xz) => SelectionGrid.Get(xz);

        public void Set(XZ xz, bool value)
        {
            SelectionGrid.Set(xz, value);
            Refresh(new LibDQB.Rect(xz, xz.Add(1, 1)));
        }

        public void Refresh(LibDQB.Rect dirty)
        {
            MinimapRenderer.UpdateSelection(SelectionBitmap, SelectionGrid, dirty);
        }
    }

    /// <summary>
    /// Decorates a grid of minimap tiles and refreshes the <see cref="Layers"/>
    /// whenever the underlying grid is modified.
    /// </summary>
    sealed class WpfMinimapGrid : BatchedUpdateMinimapGrid
    {
        public required Dictionary<MinimapRenderer.TileLayer, WriteableBitmap> Layers { get; init; }
        public required Dispatcher Dispatcher { get; init; }

        protected override void EnqueueNotification()
        {
            Dispatcher.BeginInvoke(() => Refresh());
        }

        private void Refresh()
        {
            var dirtyRect = RecomputeShorelines();
            if (dirtyRect != null)
            {
                Refresh(dirtyRect);
            }
        }

        public void RefreshAll()
        {
            Refresh(this.Bounds);
        }

        private void Refresh(LibDQB.Rect dirtyRect)
        {
            foreach (var kvp in Layers)
            {
                MinimapRenderer.Update(kvp.Value, kvp.Key, base.Grid, dirtyRect);
            }
        }
    }

    /// <summary>
    /// !! NOT THREAD SAFE !!
    /// The intended usage pattern is, for example, a WPF application where you might update
    /// many grid spaces in response to a single key press (on the UI thread).
    /// The implementation of <see cref="EnqueueNotification"/> would use Dispatcher.BeginInvoke so that work
    /// like <see cref="RecomputeShorelines"/> and refreshing the UI would also happen on the UI thread.
    /// </summary>
    abstract class BatchedUpdateMinimapGrid : IGrid<MinimapTile>
    {
        private readonly HashSet<XZ> pendingPoints = new();
        public required IGrid<MinimapTile> Grid { get; init; }
        public LibDQB.Rect Bounds => Grid.Bounds;

        protected abstract void EnqueueNotification();

        public void Set(XZ xz, MinimapTile value)
        {
            if (pendingPoints.Count == 0)
            {
                EnqueueNotification();
            }

            Grid.Set(xz, value);

            pendingPoints.Add(xz);
            foreach (var neighbor in xz.AllNeighbors().Where(Bounds.Contains))
            {
                pendingPoints.Add(neighbor);
            }
        }

        public MinimapTile Get(XZ xz) => Grid.Get(xz);

        /// <summary>
        /// Returns the Rect containing all XZs which may have changed,
        /// or null if there are no changes since last time.
        /// </summary>
        protected LibDQB.Rect? RecomputeShorelines()
        {
            if (pendingPoints.Count == 0)
            {
                return null;
            }

            foreach (var xz in pendingPoints)
            {
                RecomputeShoreline(xz);
            }
            var dirtyRect = LibDQB.Rect.GetBounds(pendingPoints);
            pendingPoints.Clear();
            return dirtyRect;
        }

        private void RecomputeShoreline(XZ xz)
        {
            var tile = Grid.Get(xz);
            if (tile.CanHaveShoreline())
            {
                var key = MinimapShorelineKey.Compute(xz, Grid);
                Grid.Set(xz, tile.FixupShoreline(key));
            }
        }
    }

    private static void PutTestPattern(IGrid<MinimapTile> minimap)
    {
        for (int i = 0; i < 2048 * 2 / 32; i++)
        {
            int val;
            if (i % 16 == 0)
            {
                val = 1 + 3 * 11;
                val |= 0x8000;
            }
            else if (i % 4 == 0)
            {
                val = 1 + 5 * 11;
                val |= 0x8000;
            }
            else
            {
                val = 1;
            }
            minimap.Set(new XZ(2, i + 3), new MinimapTile { TileValue = val });
        }

        int wantTileId = 0;
        for (int i = 0; i < 2048 * 2; i++)
        {
            int x = i % 32 + 3;
            int z = i / 32 + 3;
            int val = wantTileId * 11 + 1;
            if (val >= 0x4000)
            {
                int baseVal = val / 0x4000 * 0x4000;
                int blah = 1 + baseVal / 11;
                val = baseVal + (wantTileId - blah) * 11 + 1;
            }
            wantTileId++;

            var tile = new MinimapTile { TileValue = val };
            int overlay = tile.SeaTypeIndex switch
            {
                SeaTypeIndex.DeepSea => 7, // mountain
                SeaTypeIndex.ShallowSea => 1, // tree 1
                SeaTypeIndex.ClearWater => 3, // tree 2
                _ => 0,
            };
            overlay = 3;
            val += overlay;
            val |= 0x8000 * 3;
            tile = new MinimapTile { TileValue = val };
            if (tile.BaseTileId < 0)
            {
                tile = new MinimapTile { TileValue = 1 };
            }
            minimap.Set(new XZ(x, z), tile);
        }
    }

    private static void ValidateShores(IReadOnlyGrid<MinimapTile> grid)
    {
        if (grid.Get(new XZ(0, 0)).TileValue == -1)
        {
            return;
        }

        bool hasAlerted = false;

        foreach (var xz in grid.Bounds.Enumerate())
        {
            var tile = grid.Get(xz);
            if (tile.IsVisible && tile.CanHaveShoreline())
            {
                var key = MinimapShorelineKey.Compute(xz, grid);
                if (tile.BaseTileId == key.DeepSeaBaseTileId)
                {
                    // okay
                }
                else if (tile.BaseTileId == key.ShallowSeaBaseTileId)
                {
                    // okay
                }
                else if (tile.BaseTileId == key.ClearWaterBaseTileId)
                {
                    // okay
                }
                else if (!hasAlerted)
                {
                    hasAlerted = true;
                    // Alert! If this map hasn't been hacked, it means that I haven't learned
                    // everything about how Shorelines tiles are chosen.
                    // (But if this is a hacked map, it probably means nothing. Carry on.)
                    System.Diagnostics.Debugger.Break();
                }
            }
        }
    }
}
