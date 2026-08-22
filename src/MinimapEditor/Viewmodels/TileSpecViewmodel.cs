using LibDQB.DQB2Minimap;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace MinimapEditor.Viewmodels;

sealed class TileSpecViewmodel : ViewmodelBase
{
    private bool _setBaseTile = true;
    public bool SetBaseTile7123
    {
        get => _setBaseTile;
        set => ChangeProperty(ref _setBaseTile, value);
    }

    private BaseTileModel? _selectedBaseTile;
    public BaseTileModel? SelectedBaseTile6495
    {
        get => _selectedBaseTile;
        set => ChangeProperty(ref _selectedBaseTile, value);
    }

    public required IReadOnlyList<BaseTileModel> BaseTileChoices2327 { get; init; }

    private bool _setOverlay = true;
    public bool SetOverlay1367
    {
        get => _setOverlay;
        set => ChangeProperty(ref _setOverlay, value);
    }

    private OverlayModel? _selectedOverlay;
    public OverlayModel? SelectedOverlay8725
    {
        get => _selectedOverlay;
        set => ChangeProperty(ref _selectedOverlay, value);
    }

    public required IReadOnlyList<OverlayModel> OverlayChoices4299 { get; init; }

    public NullableBooleanModel Visibility5366 { get; } = new();
}
