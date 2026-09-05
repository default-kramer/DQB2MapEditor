using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace MinimapEditor;

/// <summary>
/// Hook for testability
/// </summary>
public class DialogManager
{
    public virtual bool? ShowDialog(FileDialog fd) => fd.ShowDialog();

    public virtual MessageBoxResult ShowMessageBox(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
    {
        return MessageBox.Show(messageBoxText, caption, button, icon);
    }
}
