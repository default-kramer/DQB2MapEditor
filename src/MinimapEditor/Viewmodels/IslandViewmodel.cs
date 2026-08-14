using LibDQB;
using LibDQB.B2;
using LibDQB.B2.Records;
using LibDQB.DQB2Minimap;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MinimapEditor.Viewmodels;

sealed class IslandViewmodel : ViewmodelBase
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
    private readonly IReadOnlyGrid<MinimapTile> unmodifiedMinimap;
    private readonly Lazy<MapEditorViewmodel> mapEditorVM;

    public IslandViewmodel(Dependencies deps, IslandId islandId)
    {
        this.deps = deps;
        IslandId2242 = islandId;
        unmodifiedMinimap = Array2D<MinimapTile>.CopyFrom(deps.Cmndat.GetMinimap(islandId));
        mapEditorVM = new Lazy<MapEditorViewmodel>(_rebuildMapEditorVM);
        CommandOpenMinimap5775 = new RelayCommand(_ => true, _ => OpenMinimap());
    }

    public IslandId IslandId2242 { get; }
    public required string IslandName3332 { get; init; }
    public ICommand CommandOpenMinimap5775 { get; }

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

    public MapEditorViewmodel GetMapEditorVM() => mapEditorVM.Value;

    private MapEditorViewmodel _rebuildMapEditorVM()
    {
        var Tilesheet = deps.Tilesheet;
        var DataDefinitions = deps.DataDefinitions;
        var islandId = this.IslandId2242;
        var minimap = deps.Cmndat.GetMinimap(islandId);

        var repainter = new BitmapRepainter<WriteableBitmap>(Tilesheet);

        var tileDecorator = new WpfMinimapGrid
        {
            Grid = new CountChangedTiles
            {
                Grid = minimap,
                IslandVM = this,
                UnmodifiedGrid = unmodifiedMinimap,
            },
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
        return viewmodel;
    }

    private void OpenMinimap()
    {
        deps.Callback.OpenMinimap(this);
    }

    /// <summary>
    /// Intercepts writes to the minimap and keeps track of how many tiles
    /// have changed relative to the <see cref="UnmodifiedGrid"/>.
    /// </summary>
    sealed class CountChangedTiles : IGrid<MinimapTile>
    {
        public required IslandViewmodel IslandVM { get; init; }
        public required IReadOnlyGrid<MinimapTile> UnmodifiedGrid { get; init; }
        public required IGrid<MinimapTile> Grid { get; init; }

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

            var origVal = UnmodifiedGrid.Get(xz);
            if (origVal == value)
            {
                IslandVM.ChangedTileCount4506--;
            }
            else if (oldVal == origVal)
            {
                IslandVM.ChangedTileCount4506++;
            }
        }
    }
}
