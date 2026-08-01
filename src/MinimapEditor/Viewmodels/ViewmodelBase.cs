using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace MinimapEditor.Viewmodels;

class ViewmodelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// For clarity. Raising <see cref="PropertyChanged"/> with this value indicates
    /// that all properties of the object have changed.
    /// </summary>
    protected const string AllProperties = null;

    protected bool ChangeProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null, params string[] moreProperties)
    {
        if (object.Equals(field, value)) { return false; }

        field = value;
        OnPropertyChanged(propertyName);
        foreach (var prop in moreProperties)
        {
            OnPropertyChanged(prop);
        }
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        var args = new PropertyChangedEventArgs(propertyName);
        PropertyChanged?.Invoke(this, args);
    }
}
