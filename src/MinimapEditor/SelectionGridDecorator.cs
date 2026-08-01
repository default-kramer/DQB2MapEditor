using LibDQB;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Imaging;

namespace MinimapEditor;

/// <summary>
/// Decorates a selection grid and informs the <see cref="Repainter"/>
/// whenever the underlying grid is modified.
/// </summary>
sealed class SelectionGridDecorator : IGrid<bool>
{
    public interface IBitmapRepainter
    {
        void Repaint(IGrid<bool> selectionGrid, Rect dirty);
    }

    public required IGrid<bool> SelectionGrid { get; init; }
    public required IBitmapRepainter Repainter { get; init; }

    public Rect Bounds => SelectionGrid.Bounds;

    public bool Get(XZ xz) => SelectionGrid.Get(xz);

    public void Set(XZ xz, bool value)
    {
        SelectionGrid.Set(xz, value);
        Refresh(new Rect(xz, xz.Add(1, 1)));
    }

    public void Refresh(Rect dirty)
    {
        Repainter.Repaint(SelectionGrid, dirty);
    }
}
