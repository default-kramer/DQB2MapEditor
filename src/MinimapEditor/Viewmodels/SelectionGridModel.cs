using LibDQB;
using LibDQB.B2;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinimapEditor.Viewmodels;

sealed class SelectionGridModel : ViewmodelBase, IGrid<bool>
{
    private readonly IGrid<bool> selectionGrid;

    // Keep separate counts per 16x16 sector:
    private readonly IGrid<int> countsPerSector;
    private const int scale = 16;

    public Rect Bounds => selectionGrid.Bounds;

    public SelectionGridModel(IGrid<bool> selectionGrid)
    {
        this.selectionGrid = selectionGrid;
        var bounds = selectionGrid.Bounds;
        if (bounds.Start.X % scale != 0 || bounds.Start.Z % scale != 0 || bounds.End.X % scale != 0 || bounds.End.Z % scale != 0)
        {
            throw new Exception($"Assert fail: expected a grid with dimensions divisible by {scale}");
        }
        var scaledBounds = new Rect(bounds.Start.Unscale(scale), bounds.End.Unscale(scale));
        countsPerSector = new Array2D<int>(scaledBounds, 0);
    }

    public void Set(XZ xz, bool value)
    {
        if (value == selectionGrid.Get(xz))
        {
            return;
        }

        selectionGrid.Set(xz, value);

        int delta = value ? 1 : -1;
        var smallXZ = xz.Unscale(scale);
        countsPerSector[smallXZ] += delta;
        SelectionCount9593 += delta;

        if (SelectionCount9593 != 1)
        {
            SingleSelection9916 = null;
        }
        else if (value)
        {
            SingleSelection9916 = xz;
        }
        else
        {
            SingleSelection9916 = FindSingleSelection();
        }
    }

    private XZ FindSingleSelection() => Selection().First();

    public IEnumerable<XZ> Selection()
    {
        foreach (var smallXZ in countsPerSector.Bounds.Enumerate())
        {
            if (countsPerSector.Get(smallXZ) > 0)
            {
                var bounds = new Rect(smallXZ.Scale(scale), smallXZ.Add(1, 1).Scale(scale));
                foreach (var xz in bounds.Enumerate())
                {
                    if (selectionGrid.Get(xz))
                    {
                        yield return xz;
                    }
                }
            }
        }
    }

    public bool Get(XZ xz)
    {
        return selectionGrid.Get(xz);
    }

    private int _selectionCount = 0;
    public int SelectionCount9593
    {
        get => _selectionCount;
        set => ChangeProperty(ref _selectionCount, value);
    }

    private XZ? _singleSelection = null;
    public XZ? SingleSelection9916
    {
        get => _singleSelection;
        set => ChangeProperty(ref _singleSelection, value, nameof(SingleSelection9916), nameof(HasSingleSelection5888));
    }

    public bool HasSingleSelection5888 => _singleSelection != null;

    public void ClearSelection()
    {
        foreach (var xz in Selection())
        {
            Set(xz, false);
        }
    }
}
