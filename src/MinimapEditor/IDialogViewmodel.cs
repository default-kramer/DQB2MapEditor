using System.Windows;

namespace MinimapEditor;

public interface IDialogViewmodel
{
    Window CreateWindow();

    EventHandler<DialogCloseEventArgs>? CloseRequested { get; set; }
}
