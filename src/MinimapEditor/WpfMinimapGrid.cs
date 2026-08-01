using LibDQB;
using LibDQB.DQB2Minimap;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MinimapEditor;

/// <summary>
/// Decorates a grid of minimap tiles and informs the <see cref="Repainter"/>
/// whenever the underlying grid is modified.
/// </summary>
sealed class WpfMinimapGrid : BatchedUpdateMinimapGrid
{
    public interface IRepainter
    {
        void Repaint(IReadOnlyGrid<MinimapTile> grid, Rect dirty);
    }

    public required IRepainter Repainter { get; init; }
    public required Dispatcher Dispatcher { get; init; }

    protected override void EnqueueNotification()
    {
        Dispatcher.BeginInvoke(() => Refresh());
    }

    private void Refresh()
    {
        var dirtyRect = RecomputeShorelines();
        if (dirtyRect != null)
        {
            Refresh(dirtyRect);
        }
    }

    public void RefreshAll()
    {
        Refresh(this.Bounds);
    }

    private void Refresh(Rect dirtyRect)
    {
        Repainter.Repaint(this, dirtyRect);
    }
}
