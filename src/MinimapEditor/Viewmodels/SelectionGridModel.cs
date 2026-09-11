using LibDQB;

namespace MinimapEditor.Viewmodels;

public sealed class SelectionGridModel : ViewmodelBase, IReadOnlyGrid<bool>
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

    public void SetAndImmediatelyNotify(XZ xz, bool value)
    {
        SelectionCount9593 = __Set(xz, value);
    }

    private void SetDeferred(XZ xz, bool value, PropertyChangeDeferral deferral)
    {
        // Set the field directly; the deferral will notify
        _selectionCount = __Set(xz, value);
    }

    private int __Set(XZ xz, bool value)
    {
        if (value == selectionGrid.Get(xz))
        {
            return _selectionCount;
        }

        selectionGrid.Set(xz, value);

        int delta = value ? 1 : -1;
        var smallXZ = xz.Unscale(scale);
        countsPerSector[smallXZ] += delta;
        return _selectionCount + delta;
    }

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

    public void ClearSelection()
    {
        using var selector = DeferPropertyChanged();
        foreach (var xz in Selection())
        {
            selector.Set(xz, false);
        }
    }

    public readonly ref struct PropertyChangeDeferral : IDisposable
    {
        private readonly SelectionGridModel parent;
        public readonly int OriginalSelectionCount;

        public PropertyChangeDeferral(SelectionGridModel parent)
        {
            this.parent = parent;
            this.OriginalSelectionCount = parent.SelectionCount9593;
        }

        public void Set(XZ xz, bool value) => parent.SetDeferred(xz, value, this);

        public void Dispose() => parent.Notify(this);
    }

    private bool isDeferring = false;

    /// <summary>
    /// Provides a big speedup when making mass edits (such as Invert Selection)
    /// by delaying the PropertyChanged notification until finished.
    /// </summary>
    public PropertyChangeDeferral DeferPropertyChanged()
    {
        if (isDeferring)
        {
            throw new InvalidOperationException("Nested deferral is not supported");
        }
        isDeferring = true;
        return new PropertyChangeDeferral(this);
    }

    private void Notify(PropertyChangeDeferral defer)
    {
        if (!isDeferring)
        {
            throw new InvalidOperationException("Assert fail - was the deferral disposed twice?");
        }

        isDeferring = false;
        if (_selectionCount != defer.OriginalSelectionCount)
        {
            OnPropertyChanged(nameof(SelectionCount9593));
        }
    }
}
