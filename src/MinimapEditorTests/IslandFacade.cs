using LibDQB;
using LibDQB.DQB2Minimap;
using MinimapEditor.Viewmodels;

namespace MinimapEditorTests;

sealed record IslandFacade
{
    private readonly IslandViewmodel islandVM;
    private readonly StartupViewmodel startupVM;

    public IslandFacade(IslandViewmodel islandVM, StartupViewmodel startupVM)
    {
        this.islandVM = islandVM;
        this.startupVM = startupVM;
    }

    private (MapEditorViewmodel MapVM, StartupViewmodel.TabItemViewmodel TabVM) EnsureOpened()
    {
        foreach (var tab in startupVM.Tabs4685)
        {
            if (tab.HoldsMapEditor(out var mapVM) && mapVM.IslandId == islandVM.IslandId2242)
            {
                return (mapVM, tab);
            }
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
}
