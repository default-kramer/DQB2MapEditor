using System;
using System.Collections.Generic;
using System.Text;

namespace MinimapEditor.Viewmodels;

public sealed class ModeModel : ViewmodelBase
{
    enum Mode
    {
        Pan,
        Select,
        RectSelect,
        Modify,
        Paste,
        WriteText,
    };

    private Mode _mode = Mode.Pan;

    private void Set(bool value, Mode mode)
    {
        if (value)
        {
            ChangeProperty(ref _mode, mode, AllProperties);
        }
        else if (mode == _mode)
        {
            ChangeProperty(ref _mode, Mode.Pan, AllProperties);
        }
    }

    public bool IsPanMode8931
    {
        get => _mode == Mode.Pan;
        set => Set(value, Mode.Pan);
    }

    public bool IsSelectMode5073
    {
        get => _mode == Mode.Select;
        set => Set(value, Mode.Select);
    }

    public bool IsRectSelectMode2843
    {
        get => _mode == Mode.RectSelect;
        set => Set(value, Mode.RectSelect);
    }

    public bool IsModifyMode6812
    {
        get => _mode == Mode.Modify;
        set => Set(value, Mode.Modify);
    }

    public bool IsPasteMode4735
    {
        get => _mode == Mode.Paste;
        set => Set(value, Mode.Paste);
    }

    public bool IsWriteTextMode2099
    {
        get => _mode == Mode.WriteText;
        set => Set(value, Mode.WriteText);
    }

    public bool IsAnySelectMode4440 => IsSelectMode5073 || IsRectSelectMode2843;

    public bool IsSpecialMode8897 => IsWriteTextMode2099;
}
