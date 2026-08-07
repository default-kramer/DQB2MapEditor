using LibDQB;
using LibDQB.B2;
using LibDQB.DQB2Minimap;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace MinimapEditor.Viewmodels;

sealed class MapEditorViewmodel : ViewmodelBase
{
    public interface IRepainter
    {
        IEnumerable<ImageSource> AllLayers();
    }

    private readonly IGrid<MinimapTile> grid;

    public required string CmndatPath3902 { get; init; }
    public required RawCommonData Cmndat { get; init; }
    public required IRepainter BitmapLayers { get; init; }

    public MapEditorViewmodel(IGrid<MinimapTile> grid, IGrid<bool> selectionGrid, DataDefinitions definitions)
    {
        this.grid = grid;
        this.selectionRectOrigState = new Array2D<bool>(selectionGrid.Bounds, false);

        SelectionGrid1346 = new SelectionGridModel(selectionGrid);

        BaseTileChoices8121 = definitions.BaseTiles;
        SelectedBaseTile7703 = BaseTileChoices8121.SingleOrDefault(b => b.BaseTileId == 7);
        SetBaseTile7860 = true;

        OverlayChoices5094 = definitions.Overlays;
        SelectedOverlay3158 = OverlayChoices5094.SingleOrDefault(o => o.OverlayId == 3);
        SetOverlay3252 = true;

        Visibility8138 = new();
        Visibility8138.IsTrue9880 = true;

        CommandApplyToSelection4785 = new RelayCommand(_ => SelectionGrid1346.SelectionCount9593 > 0, _ => ApplyToSelection());
        CommandClearSelection9567 = new RelayCommand(_ => SelectionGrid1346.SelectionCount9593 > 0, _ => SelectionGrid1346.ClearSelection());
        CommandSelectElevatedTiles2364 = new RelayCommand(_ => true, _ => SelectElevatedTiles());
        CommandSelectIllegalTiles8864 = new RelayCommand(_ => true, _ => SelectIllegalTiles());
        CommandSelectIncorrectShorelines7151 = new RelayCommand(_ => true, _ => SelectIncorrectShorelines());
        CommandFixShorelinesAllTiles8510 = new RelayCommand(_ => true, _ => FixShorelines(selectedTilesOnly: false));
        CommandFixShorelinesSelectedTiles3733 = new RelayCommand(_ => true, _ => FixShorelines(selectedTilesOnly: true));
        CommandRemoveElevationAllTiles2125 = new RelayCommand(_ => true, _ => RemoveElevation(selectedTilesOnly: false));
        CommandRemoveElevationSelectedTiles6487 = new RelayCommand(_ => true, _ => RemoveElevation(selectedTilesOnly: true));
        CommandResetMapAllTiles3843 = new RelayCommand(_ => true, _ => ResetMap(selectedTilesOnly: false));
        CommandResetMapSelectedTiles1852 = new RelayCommand(_ => true, _ => ResetMap(selectedTilesOnly: true));
    }

    public ICommand CommandApplyToSelection4785 { get; }
    public ICommand CommandClearSelection9567 { get; }
    public ICommand CommandSelectElevatedTiles2364 { get; }
    public ICommand CommandSelectIllegalTiles8864 { get; }
    public ICommand CommandSelectIncorrectShorelines7151 { get; }
    public ICommand CommandFixShorelinesAllTiles8510 { get; }
    public ICommand CommandFixShorelinesSelectedTiles3733 { get; }
    public ICommand CommandRemoveElevationAllTiles2125 { get; }
    public ICommand CommandRemoveElevationSelectedTiles6487 { get; }
    public ICommand CommandResetMapAllTiles3843 { get; }
    public ICommand CommandResetMapSelectedTiles1852 { get; }

    public SelectionGridModel SelectionGrid1346 { get; }
    public ModeModel Mode1336 { get; } = new();

    public IReadOnlyGrid<MinimapTile> Grid() => grid;

    public IReadOnlyList<BaseTileModel> BaseTileChoices8121 { get; }
    private BaseTileModel? _selectedBaseTile;
    public BaseTileModel? SelectedBaseTile7703
    {
        get => _selectedBaseTile;
        set => ChangeProperty(ref _selectedBaseTile, value);
    }

    public IReadOnlyList<OverlayModel> OverlayChoices5094 { get; }
    private OverlayModel? _selectedOverlay;
    public OverlayModel? SelectedOverlay3158
    {
        get => _selectedOverlay;
        set => ChangeProperty(ref _selectedOverlay, value);
    }

    private string _xDisplay = "";
    public string XDisplay2724
    {
        get => _xDisplay;
        set => ChangeProperty(ref _xDisplay, value);
    }
    private string _zDisplay = "";
    public string ZDisplay6617
    {
        get => _zDisplay;
        set => ChangeProperty(ref _zDisplay, value);
    }
    private string _fullHoverInfo = "";
    public string FullHoverInfo1657
    {
        get => _fullHoverInfo;
        set => ChangeProperty(ref _fullHoverInfo, value);
    }

    private BaseTileModel? _hoveredBaseTile = null;
    public BaseTileModel? HoveredBaseTile4659
    {
        get => _hoveredBaseTile;
        set => ChangeProperty(ref _hoveredBaseTile, value);
    }

    private OverlayModel? _hoveredOverlay = null;
    public OverlayModel? HoveredOverlay1634
    {
        get => _hoveredOverlay;
        set => ChangeProperty(ref _hoveredOverlay, value);
    }

    private bool _setBaseTile = false;
    public bool SetBaseTile7860
    {
        get => _setBaseTile;
        set => ChangeProperty(ref _setBaseTile, value);
    }

    private bool _setOverlay = false;
    public bool SetOverlay3252
    {
        get => _setOverlay;
        set => ChangeProperty(ref _setOverlay, value);
    }

    public NullableBooleanModel Visibility8138 { get; }

    // Ctrl and Alt keys are poor choices due to special handling.
    // The number keys seem like a good choice...
    public static bool IsSelectKey(Key key) => key == Key.D1 || key == Key.NumPad1;
    public static bool IsRectSelectKey(Key key) => key == Key.D2 || key == Key.NumPad2;
    private static bool IsModifyKey(Key key) => key == Key.D3 || key == Key.NumPad3;

    public void OnPreviewKeyDown(Key key)
    {
        if (IsModifyKey(key))
        {
            this.Mode1336.IsModifyMode6812 = true;
        }
        else if (IsSelectKey(key))
        {
            this.Mode1336.IsSelectMode5073 = true;
        }
        else if (IsRectSelectKey(key))
        {
            this.Mode1336.IsRectSelectMode2843 = true;
        }
    }

    public void OnPreviewKeyUp(Key key)
    {
        if (IsModifyKey(key))
        {
            this.Mode1336.IsModifyMode6812 = false;
        }
        else if (IsSelectKey(key))
        {
            this.Mode1336.IsSelectMode5073 = false;
        }
        else if (IsRectSelectKey(key))
        {
            this.Mode1336.IsRectSelectMode2843 = false;
        }
    }

    private bool isLeftMouseDown = false;
    private bool isRightMouseDown = false;

    public void OnMouseEvent(MouseEventArgs e)
    {
        bool oldLeft = isLeftMouseDown;
        isLeftMouseDown = e.LeftButton == MouseButtonState.Pressed;
        isRightMouseDown = e.RightButton == MouseButtonState.Pressed;

        if (!this.Mode1336.IsPanMode8931)
        {
            e.Handled = true; // disable panning
        }

        if (this.Mode1336.IsSelectMode5073)
        {
            if (SelectionGrid1346.Bounds.Contains(mouseXZ))
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    SelectionGrid1346.Set(mouseXZ, true);
                }
                else if (e.RightButton == MouseButtonState.Pressed)
                {
                    SelectionGrid1346.Set(mouseXZ, false);
                }
            }
        }
        else if (this.Mode1336.IsRectSelectMode2843)
        {
            if (selectionRectDragStart == null)
            {
                if (isLeftMouseDown)
                {
                    selectionRectOrigState.CopyFrom(SelectionGrid1346);
                    selectionRectDragStart = (mouseXZ, true);
                }
                else if (isRightMouseDown)
                {
                    selectionRectOrigState.CopyFrom(SelectionGrid1346);
                    selectionRectDragStart = (mouseXZ, false);
                }
            }
            UpdateRectSelection(mouseXZ, mouseXZ);
        }
        else if (this.Mode1336.IsModifyMode6812)
        {
            if (isLeftMouseDown && !oldLeft)
            {
                ApplyModification();
            }
        }
    }

    private void UpdateRectSelection(XZ newXZ, XZ prevXZ)
    {
        if (selectionRectDragStart.HasValue)
        {
            if (selectionRectDragStart.Value.isSelecting && !isLeftMouseDown)
            {
                selectionRectDragStart = null;
            }
            else if (!selectionRectDragStart.Value.isSelecting && !isRightMouseDown)
            {
                selectionRectDragStart = null;
            }
        }

        if (!selectionRectDragStart.HasValue)
        {
            return;
        }

        var (startXZ, isSelecting) = selectionRectDragStart.Value;
        var newRect = LibDQB.Rect.GetBounds([startXZ, newXZ]);
        var fullRect = LibDQB.Rect.GetBounds([startXZ, newXZ, prevXZ]);
        foreach (var xz in fullRect.Enumerate())
        {
            if (newRect.Contains(xz))
            {
                SelectionGrid1346.Set(xz, isSelecting);
            }
            else
            {
                // Selection Rect has shrunk, revert to whatever was there before
                SelectionGrid1346.Set(xz, selectionRectOrigState.Get(xz));
            }
        }
    }

    /// <summary>
    /// Clones the selection grid before starting a (de)selection rect so
    /// that we can revert to previous values if the user shrinks their rect.
    /// </summary>
    private readonly Array2D<bool> selectionRectOrigState;

    private (XZ loc, bool isSelecting)? selectionRectDragStart = null;

    private XZ mouseXZ = XZ.Zero.Add(-1, -1);
    public void OnMousePositionChanged(XZ xz)
    {
        var prevMouseXZ = mouseXZ;

        if (xz != mouseXZ)
        {
            mouseXZ = xz;

            XDisplay2724 = xz.X.ToString();
            ZDisplay6617 = xz.Z.ToString();

            if (grid.Bounds.Contains(xz))
            {
                var tile = grid.Get(xz);
                // Show the real tile in the Debug Info...
                FullHoverInfo1657 = $"Debug Info: 0x{tile.TileValue.ToString("x4")} / {tile.BaseTileId} / {tile.ApparentOverlayId}:{tile.FormulaicOverlayId} / {tile.IsVisible}";
                // ... but show the "No Shoreline" everywhere else:
                tile = tile.FixupShoreline(MinimapShorelineKey.NoShoreline);
                HoveredBaseTile4659 = BaseTileChoices8121.SingleOrDefault(t => t.BaseTileId == tile.BaseTileId);
                HoveredOverlay1634 = OverlayChoices5094.SingleOrDefault(o => o.OverlayId == tile.FormulaicOverlayId);
            }
            else
            {
                FullHoverInfo1657 = "";
                HoveredBaseTile4659 = null;
                HoveredOverlay1634 = null;
            }

            ApplyModification();

            if (this.Mode1336.IsRectSelectMode2843)
            {
                UpdateRectSelection(xz, prevMouseXZ);
            }

            if (this.Mode1336.IsSelectMode5073)
            {
                if (isLeftMouseDown)
                {
                    SelectionGrid1346.Set(xz, true);
                }
                else if (isRightMouseDown)
                {
                    SelectionGrid1346.Set(xz, false);
                }
            }
        }
    }

    private void ApplyModification()
    {
        if (!this.Mode1336.IsModifyMode6812 || !isLeftMouseDown)
        {
            return;
        }

        ApplyModificationTo([mouseXZ]);
    }

    private void ApplyToSelection()
    {
        ApplyModificationTo(SelectionGrid1346.Selection());
    }

    private void ApplyModificationTo(IEnumerable<XZ> xzs)
    {
        var baseTile = SetBaseTile7860 ? SelectedBaseTile7703 : null;
        var overlay = SetOverlay3252 ? SelectedOverlay3158 : null;
        var visibility = Visibility8138.Value();

        if (baseTile == null && overlay == null && !visibility.HasValue)
        {
            return;
        }

        foreach (var xz in xzs)
        {
            var tile = grid.Get(xz);
            if (baseTile != null)
            {
                tile = tile.ReplaceBaseTile(baseTile.BaseTileId);
            }
            if (overlay != null)
            {
                tile = tile.ReplaceOverlay(overlay.OverlayId);
            }
            if (visibility.HasValue)
            {
                tile = tile.ReplaceVisibility(visibility.Value);
            }

            grid.Set(xz, tile);
        }
    }

    public void SaveCmndat()
    {
        const string message = "This app does not create backups yet."
            + " Are you absolutely sure you want to overwrite your CMNDAT?";

        var result = MessageBox.Show(message, "WARNING!!", MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.No);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        result = MessageBox.Show("Really?", "WARNING!!", MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.No);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        Cmndat.LastSaveTime = DateTime.UtcNow.AddYears(1000);
        Cmndat.Save(CmndatPath3902);
        MessageBox.Show("Saved!");
    }

    private void SelectElevatedTiles()
    {
        foreach (var xz in grid.Bounds.Enumerate())
        {
            if (grid.Get(xz).IsQuirky)
            {
                SelectionGrid1346.Set(xz, true);
            }
        }
    }

    private void SelectIllegalTiles()
    {
        foreach (var xz in grid.Bounds.Enumerate())
        {
            if (!grid.Get(xz).BaseTileId.IsLegal)
            {
                SelectionGrid1346.Set(xz, true);
            }
        }
    }

    private bool IsShorelineIncorrect(XZ xz, out MinimapTile correction)
    {
        var tile = grid.Get(xz).RemoveQuirkiness();
        if (tile.CanHaveShoreline())
        {
            var key = MinimapShorelineKey.Compute(xz, grid);
            correction = tile.FixupShoreline(key);
            return tile != correction;
        }
        else
        {
            correction = tile;
            return false;
        }
    }

    private void SelectIncorrectShorelines()
    {
        foreach (var xz in grid.Bounds.Enumerate())
        {
            if (IsShorelineIncorrect(xz, out _))
            {
                SelectionGrid1346.Set(xz, true);
            }
        }
    }

    private void FixShorelines(bool selectedTilesOnly)
    {
        int changeCount = 0;
        var xzs = selectedTilesOnly ? SelectionGrid1346.Selection() : grid.Bounds.Enumerate();
        foreach (var xz in xzs)
        {
            if (IsShorelineIncorrect(xz, out var correction))
            {
                grid.Set(xz, correction);
                changeCount++;
            }
        }

        MessageBox.Show($"{changeCount} tiles updated.");
    }

    private void RemoveElevation(bool selectedTilesOnly)
    {
        int changeCount = 0;
        var xzs = selectedTilesOnly ? SelectionGrid1346.Selection() : grid.Bounds.Enumerate();
        foreach (var xz in xzs)
        {
            var tile = grid.Get(xz);
            if (tile.IsQuirky)
            {
                grid.Set(xz, tile.RemoveQuirkiness());
                changeCount++;
            }
        }

        MessageBox.Show($"{changeCount} tiles updated.");
    }

    private void ResetMap(bool selectedTilesOnly)
    {
        // FUTURE WORK - We should probably only set this zeroTile when a chunk is present.
        // When a chunk isn't present, we should reset to 1 instead...
        var zeroTile = MinimapTile.FromRawValue(0);

        int changeCount = 0;
        var xzs = selectedTilesOnly ? SelectionGrid1346.Selection() : grid.Bounds.Enumerate();
        foreach (var xz in xzs)
        {
            grid.Set(xz, zeroTile);
            changeCount++;
        }

        MessageBox.Show("The reset tiles will be updated by DQB2 when the Builder comes near enough. Door overlays may take longer to update.", $"{changeCount} tiles reset.");
    }
}
