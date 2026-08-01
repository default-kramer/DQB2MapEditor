using LibDQB;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MinimapEditor;

/// <summary>
/// Decorates a grid of minimap tiles and refreshes the <see cref="Layers"/>
/// whenever the underlying grid is modified.
/// </summary>
sealed class WpfMinimapGrid : BatchedUpdateMinimapGrid
{
    public interface IBitmapLayers
    {
        IEnumerable<(MinimapRenderer.TileLayer LayerId, WriteableBitmap Bitmap)> Layers();
    }

    public required IBitmapLayers Layers { get; init; }
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
        foreach (var item in Layers.Layers())
        {
            MinimapRenderer.Update(item.Bitmap, item.LayerId, base.Grid, dirtyRect);
        }
    }
}
