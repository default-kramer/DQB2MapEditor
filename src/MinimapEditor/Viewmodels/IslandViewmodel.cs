using LibDQB;
using LibDQB.B2;
using LibDQB.B2.Records;
using LibDQB.DQB2Minimap;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MinimapEditor.Viewmodels;

public sealed class IslandViewmodel : ViewmodelBase
{
    public interface ICallback
    {
        void OpenMinimap(IslandViewmodel islandVM);
    }

    public sealed record Dependencies
    {
        public required RawCommonData Cmndat { get; init; }
        public required SapphireRetroTilesheet Tilesheet { get; init; }
        public required DataDefinitions DataDefinitions { get; init; }
        public required ICallback Callback { get; init; }
    }

    private readonly Dependencies deps;
    private readonly Lazy<MapEditor> mapEditor;

    public IslandViewmodel(Dependencies deps, IslandId islandId)
    {
        this.deps = deps;
        IslandId2242 = islandId;
        mapEditor = new Lazy<MapEditor>(_rebuildMapEditor);
        CommandOpenMinimap5775 = new RelayCommand(_ => true, _ => OpenMinimap());
        CommandDiscardChanges2227 = new RelayCommand(_ => ChangedTileCount4506 > 0, _ => DiscardChanges());
    }

    public required DialogManager DialogManager { get; init; }
    public IslandId IslandId2242 { get; }
    public required string IslandName3332 { get; init; }
    public ICommand CommandOpenMinimap5775 { get; }
    public ICommand CommandDiscardChanges2227 { get; }

    private int _changedTileCount = 0;
    public int ChangedTileCount4506
    {
        get => _changedTileCount;
        private set => ChangeProperty(ref _changedTileCount, value);
    }

    public static IEnumerable<(string, IslandId)> Islands()
    {
        yield return ("Isle of Awakening", IslandId.IoA);
        yield return ("Furrowfield", IslandId.Furrowfield);
        yield return ("Khrumbul-Dun", IslandId.KhrumbulDun);
        yield return ("Moonbrooke", IslandId.Moonbrooke);
        yield return ("Malhalla", IslandId.Malhalla);
        yield return ("Buildertopia 1", IslandId.Buildertopia1);
        yield return ("Buildertopia 2 (Beta)", IslandId.Buildertopia2);
        yield return ("Buildertopia 3 (Gamma)", IslandId.Buildertopia3);
        yield return ("Skelkatraz", IslandId.Skelkatraz);
        yield return ("Angler's Isle", IslandId.AnglersIsle);
        yield return ("??? 5", new IslandId(5));
        yield return ("??? 6", new IslandId(6));
        yield return ("??? 9", new IslandId(9));
        yield return ("??? 12", new IslandId(12));
    }

    public MapEditorViewmodel GetMapEditorVM() => mapEditor.Value.VM;

    sealed class MapEditor
    {
        public MapEditorViewmodel VM { get; }
        private readonly CountChangedTiles countChangedTiles;
        private readonly WpfMinimapGrid wpfMinimapGrid;

        public MapEditor(MapEditorViewmodel vm, CountChangedTiles countChangedTiles, WpfMinimapGrid wpfMinimapGrid)
        {
            this.VM = vm;
            this.countChangedTiles = countChangedTiles;
            this.wpfMinimapGrid = wpfMinimapGrid;
        }

        public void DiscardChanges()
        {
            countChangedTiles.DiscardChanges();
            wpfMinimapGrid.RefreshAll();
        }

        public void OnCmndatSaved()
        {
            countChangedTiles.OnCmndatSaved();
        }
    }

    private MapEditor _rebuildMapEditor()
    {
        var Tilesheet = deps.Tilesheet;
        var DataDefinitions = deps.DataDefinitions;
        var islandId = this.IslandId2242;
        var minimap = deps.Cmndat.GetMinimap(islandId);

        var changeCountingGrid = new CountChangedTiles(minimap)
        {
            IslandVM = this,
        };

        var repainter = new BitmapRepainter<WriteableBitmap>(Tilesheet);

        var tileDecorator = new WpfMinimapGrid
        {
            Grid = changeCountingGrid,
            Repainter = repainter,
            Dispatcher = Dispatcher.CurrentDispatcher,
        };

        tileDecorator.RefreshAll();

        var selectionGrid = new Array2D<bool>(minimap.Bounds, false);

        var selectionDecorator = new SelectionGridDecorator
        {
            SelectionGrid = selectionGrid,
            Repainter = repainter,
        };
        selectionDecorator.Refresh(selectionGrid.Bounds);

        var viewmodel = new MapEditorViewmodel(tileDecorator, selectionDecorator, DataDefinitions)
        {
            IslandId = islandId,
            BitmapLayers = repainter,
        };
        return new MapEditor(viewmodel, changeCountingGrid, tileDecorator);
    }

    private void OpenMinimap()
    {
        deps.Callback.OpenMinimap(this);
    }

    private void DiscardChanges()
    {
        if (!mapEditor.IsValueCreated)
        {
            return;
        }

        var result = DialogManager.ShowMessageBox($"Discard changes to {IslandName3332}?", "Confirm Discard",
            System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxImage.Warning);
        if (result != System.Windows.MessageBoxResult.OK)
        {
            return;
        }

        mapEditor.Value.DiscardChanges();
    }

    public void OnCmndatSaved()
    {
        if (mapEditor.IsValueCreated)
        {
            mapEditor.Value.OnCmndatSaved();
        }
    }

    /// <summary>
    /// Intercepts writes to the minimap and keeps track of how many tiles
    /// have changed relative to the <see cref="UnmodifiedGrid"/>.
    /// </summary>
    sealed class CountChangedTiles : IGrid<MinimapTile>
    {
        public required IslandViewmodel IslandVM { get; init; }
        public IGrid<MinimapTile> Grid { get; }
        private IReadOnlyGrid<MinimapTile> unmodifiedGrid;
        public CountChangedTiles(IGrid<MinimapTile> grid)
        {
            this.Grid = grid;
            this.unmodifiedGrid = Array2D<MinimapTile>.CopyFrom(grid);
        }


        Rect IReadOnlyGrid<MinimapTile>.Bounds => Grid.Bounds;

        MinimapTile IReadOnlyGrid<MinimapTile>.Get(XZ xz) => Grid.Get(xz);

        void IGrid<MinimapTile>.Set(XZ xz, MinimapTile value)
        {
            var oldVal = Grid.Get(xz);
            if (oldVal == value)
            {
                return;
            }

            Grid.Set(xz, value);

            var origVal = unmodifiedGrid.Get(xz);
            if (origVal == value)
            {
                IslandVM.ChangedTileCount4506--;
            }
            else if (oldVal == origVal)
            {
                IslandVM.ChangedTileCount4506++;
            }
        }

        public void OnCmndatSaved()
        {
            this.unmodifiedGrid = Array2D<MinimapTile>.CopyFrom(this);
            IslandVM.ChangedTileCount4506 = 0;
        }

        public void DiscardChanges()
        {
            this.CopyFrom(unmodifiedGrid);

            if (IslandVM.ChangedTileCount4506 != 0)
            {
                IslandVM.ChangedTileCount4506 = 0;
                Util.SoftAssertFail();
            }
        }
    }
}
