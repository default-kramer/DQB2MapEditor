using Microsoft.Win32;
using System.Windows;

namespace MinimapEditor;

/// <summary>
/// Hook for testability
/// </summary>
public class DialogManager
{
    private readonly Stack<Window> dialogStack = new();
    private readonly Lazy<Window> mainWindow;
    public DialogManager(Window mainWindow)
    {
        this.mainWindow = new Lazy<Window>(mainWindow);
    }

    protected DialogManager()
    {
        mainWindow = new Lazy<Window>(() => throw new NotImplementedException("no main window available"));
    }

    public virtual bool? ShowDialog(FileDialog fd) => fd.ShowDialog();

    public virtual MessageBoxResult ShowMessageBox(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
    {
        return MessageBox.Show(messageBoxText, caption, button, icon);
    }

    public virtual void ShowError(string message, string caption = "Error")
    {
        MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public virtual bool? ShowDialog(IDialogViewmodel vm)
    {
        var window = vm.CreateWindow();
        if (dialogStack.TryPeek(out var owner))
        {
            window.Owner = owner;
        }
        else
        {
            window.Owner = mainWindow.Value;
        }
        dialogStack.Push(window);

        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        window.DataContext = vm;

        // The window might be closed without the VM being involved
        window.Closed += (_, _) =>
        {
            if (dialogStack.TryPeek(out var top) && top == window)
            {
                dialogStack.Pop();
            }
            else
            {
                Util.SoftAssertFail();
                dialogStack.Clear();
            }
        };

        vm.CloseRequested += (_, e) =>
        {
            window.DialogResult = e.DialogResult;
            window.Close();
        };

        return window.ShowDialog();
    }
}
