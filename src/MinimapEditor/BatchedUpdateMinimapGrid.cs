using LibDQB;
using LibDQB.DQB2Minimap;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinimapEditor;

/// <summary>
/// !! NOT THREAD SAFE !!
/// The intended usage pattern is, for example, a WPF application where you might update
/// many grid spaces in response to a single key press (on the UI thread).
/// The implementation of <see cref="EnqueueNotification"/> would use Dispatcher.BeginInvoke so that work
/// like <see cref="RecomputeShorelines"/> and refreshing the UI would also happen on the UI thread.
/// </summary>
/// <remarks>
/// Candidate LibDQB code?
/// I don't think so, this is tricky/confusing.
/// And if you really want it you can easily re-invent it.
/// </remarks>
abstract class BatchedUpdateMinimapGrid : IGrid<MinimapTile>
{
    private readonly HashSet<XZ> pendingPoints = new();
    public required IGrid<MinimapTile> Grid { get; init; }
    public Rect Bounds => Grid.Bounds;

    protected abstract void EnqueueNotification();

    public void Set(XZ xz, MinimapTile value)
    {
        if (pendingPoints.Count == 0)
        {
            EnqueueNotification();
        }

        Grid.Set(xz, value);

        pendingPoints.Add(xz);
        foreach (var neighbor in xz.AllNeighbors().Where(Bounds.Contains))
        {
            pendingPoints.Add(neighbor);
        }
    }

    public MinimapTile Get(XZ xz) => Grid.Get(xz);

    /// <summary>
    /// Returns the Rect containing all XZs which may have changed,
    /// or null if there are no changes since last time.
    /// </summary>
    protected Rect? RecomputeShorelines()
    {
        if (pendingPoints.Count == 0)
        {
            return null;
        }

        foreach (var xz in pendingPoints)
        {
            RecomputeShoreline(xz);
        }
        var dirtyRect = Rect.GetBounds(pendingPoints);
        pendingPoints.Clear();
        return dirtyRect;
    }

    private void RecomputeShoreline(XZ xz)
    {
        var tile = Grid.Get(xz);
        if (tile.CanHaveShoreline())
        {
            var key = MinimapShorelineKey.Compute(xz, Grid);
            Grid.Set(xz, tile.FixupShoreline(key));
        }
    }
}
