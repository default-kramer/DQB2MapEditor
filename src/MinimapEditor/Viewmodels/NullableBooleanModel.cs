using System;
using System.Collections.Generic;
using System.Text;

namespace MinimapEditor.Viewmodels;

public sealed class NullableBooleanModel : ViewmodelBase
{
    private bool? val = null;

    public bool IsNull3392
    {
        get => val == null;
        set { if (value) ChangeProperty(ref val, null, AllProperties); }
    }

    public bool IsTrue9880
    {
        get => val == true;
        set { if (value) ChangeProperty(ref val, true, AllProperties); }
    }

    public bool IsFalse9122
    {
        get => val == false;
        set { if (value) ChangeProperty(ref val, false, AllProperties); }
    }

    public bool? Value() => val;
}
