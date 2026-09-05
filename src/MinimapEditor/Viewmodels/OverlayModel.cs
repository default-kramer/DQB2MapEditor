using LibDQB.DQB2Minimap;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace MinimapEditor.Viewmodels;

public sealed class OverlayModel
{
    public required ImageSource ImageSource { get; init; }
    public required string Name { get; init; }
    public required OverlayId OverlayId { get; init; }
}
