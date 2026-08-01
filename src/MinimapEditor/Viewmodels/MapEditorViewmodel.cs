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
    }

    public ICommand CommandApplyToSelection4785 { get; }
    public ICommand CommandClearSelection9567 { get; }
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
    private static bool IsModifyKey(Key key) => key == Key.D2 || key == Key.NumPad2;

    public static bool IsSelectKey(Key key) => key == Key.D1 || key == Key.NumPad1;

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
        else if (this.Mode1336.IsModifyMode6812)
        {
            if (isLeftMouseDown && !oldLeft)
            {
                ApplyModification();
            }
        }
    }

    private XZ mouseXZ = XZ.Zero.Add(-1, -1);
    public void OnMousePositionChanged(XZ xz)
    {
        if (xz != mouseXZ)
        {
            mouseXZ = xz;

            XDisplay2724 = xz.X.ToString();
            ZDisplay6617 = xz.Z.ToString();

            if (grid.Bounds.Contains(xz))
            {
                var tile = grid.Get(xz);
                // Show the real tile in the Debug Info...
                FullHoverInfo1657 = $"Debug Info: {tile.TileValue} / {tile.BaseTileId} / {tile.OverlayId} / {tile.IsVisible}";
                // ... but show the "No Shoreline" everywhere else:
                tile = tile.FixupShoreline(MinimapShorelineKey.NoShoreline);
                HoveredBaseTile4659 = BaseTileChoices8121.SingleOrDefault(t => t.BaseTileId == tile.BaseTileId);
                HoveredOverlay1634 = OverlayChoices5094.SingleOrDefault(o => o.OverlayId == tile.OverlayId);
            }
            else
            {
                FullHoverInfo1657 = "";
                HoveredBaseTile4659 = null;
                HoveredOverlay1634 = null;
            }

            ApplyModification();

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
}
