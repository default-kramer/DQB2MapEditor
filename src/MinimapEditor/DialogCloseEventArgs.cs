
namespace MinimapEditor;

public sealed record DialogCloseEventArgs
{
    public required bool? DialogResult { get; init; }
}
