using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace MinimapEditor.Viewmodels;

sealed class OverlayModel
{
    public required ImageSource ImageSource { get; init; }
    public required string Name { get; init; }
    internal required int OverlayIndex { get; init; }
}
