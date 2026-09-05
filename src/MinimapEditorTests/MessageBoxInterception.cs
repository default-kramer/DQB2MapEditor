using System.Windows;

namespace MinimapEditorTests;

sealed record MessageBoxInterception
{
    public required MessageBoxResult Result { get; init; }
    public string? AssertText { get; init; }
    public string? AssertCaption { get; init; }
}
