using LibDQB;
using LibDQB.DQB2Minimap;
using MinimapEditor.Viewmodels;

namespace MinimapEditorTests;

sealed record IslandFacade
{
    private readonly IslandViewmodel islandVM;
    private readonly StartupViewmodel startupVM;
    public required FakeDialogManager FakeDialogManager { get; init; }

    public IslandFacade(IslandViewmodel islandVM, StartupViewmodel startupVM)
    {
        this.islandVM = islandVM;
        this.startupVM = startupVM;
    }

    public bool IsTabOpened() => IsTabOpened(out _, out _);

    private bool IsTabOpened(out MapEditorViewmodel mapVM, out StartupViewmodel.TabItemViewmodel tabVM)
    {
        foreach (var tab in startupVM.Tabs4685)
        {
            if (tab.HoldsMapEditor(out var map) && map.IslandId == islandVM.IslandId2242)
            {
                mapVM = map;
                tabVM = tab;
                return true;
            }
        }

        mapVM = null!;
        tabVM = null!;
        return false;
    }

    private (MapEditorViewmodel MapVM, StartupViewmodel.TabItemViewmodel TabVM) EnsureOpened()
    {
        if (IsTabOpened(out var mapVM, out var tabVM))
        {
            return (mapVM, tabVM);
        }
        throw new Exception($"Tab is not open: {islandVM.IslandId2242}");
    }

    public void SetTile(int x, int z, MinimapTile tile)
    {
        var mapVM = EnsureOpened().MapVM;
        mapVM.Mode1336.IsModifyMode6812 = true;
        SetTile(mapVM.ModifyTileSpec5436, tile);
        mapVM.OnMousePositionChanged(new XZ(x, z));
        mapVM.OnMouseEvent(FakeMouseEvent.LeftDown());
        mapVM.OnMouseEvent(FakeMouseEvent.AllRelease());
    }

    private static void SetTile(TileSpecViewmodel vm, MinimapTile tile)
    {
        vm.SetBaseTile7123 = true;
        vm.SelectedBaseTile6495 = vm.BaseTileChoices2327.Single(x => x.BaseTileId == tile.BaseTileId);
        vm.SetOverlay1367 = true;
        vm.SelectedOverlay8725 = vm.OverlayChoices4299.Single(x => x.OverlayId == tile.ApparentOverlayId);
        if (tile.IsVisible)
        {
            vm.Visibility5366.IsTrue9880 = true;
        }
        else
        {
            vm.Visibility5366.IsFalse9122 = true;
        }
    }

    public int ChangedTileCount => islandVM.ChangedTileCount4506;

    public void CloseTab()
    {
        var tabVM = EnsureOpened().TabVM;
        Assert.IsTrue(tabVM.CanCloseTab4739);
        Assert.IsNotNull(tabVM.CommandCloseTab2176);
        Assert.IsTrue(tabVM.CommandCloseTab2176.CanExecute(null));
        tabVM.CommandCloseTab2176.Execute(null);
    }

    public void EnterWriteTextMode(XZ initialPosition)
    {
        var (mapVM, _) = EnsureOpened();
        mapVM.EnterWriteTextMode(initialPosition, out _);
        Assert.IsGreaterThan(1, mapVM.WriteText1898.Text1230.Length);
    }

    public void DiscardChanges()
    {
        Assert.IsTrue(islandVM.CommandDiscardChanges2227.CanExecute(null));
        FakeDialogManager.nextMessageBoxResult = new MessageBoxInterception()
        {
            AssertCaption = "Confirm Discard",
            Result = System.Windows.MessageBoxResult.OK,
        };
        islandVM.CommandDiscardChanges2227.Execute(null);
    }

    public void DoSnapshotTest(string snapshotName)
    {
        var (mapVM, _) = EnsureOpened();
        Util.DoSnapshotTest(snapshotName, mapVM.Grid());
    }

    /// <summary>
    /// Clone so that setting properties won't do anything
    /// (modification requests must come through the facade)
    /// </summary>
    public ModeModel CloneCurrentMode()
    {
        var (mapVM, _) = EnsureOpened();
        return mapVM.Mode1336.Clone();
    }
}
