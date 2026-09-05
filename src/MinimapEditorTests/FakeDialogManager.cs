using Microsoft.Win32;
using MinimapEditor;
using System.Windows;

namespace MinimapEditorTests;

class FakeDialogManager : DialogManager
{
    public (bool? retval, string filename)? nextDialogResult = null;
    public override bool? ShowDialog(FileDialog fd)
    {
        if (nextDialogResult.HasValue)
        {
            var x = nextDialogResult.Value;
            nextDialogResult = null;
            fd.FileName = x.filename;
            return x.retval;
        }
        else
        {
            throw new Exception("No dialog handler configured");
        }
    }

    public MessageBoxInterception? nextMessageBoxResult = null;
    public override MessageBoxResult ShowMessageBox(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
    {
        if (nextMessageBoxResult == null)
        {
            throw new Exception("No message box handler configured");
        }
        var current = nextMessageBoxResult;
        nextMessageBoxResult = null;

        Assert.AreEqual(messageBoxText, current.AssertText ?? messageBoxText);
        Assert.AreEqual(caption, current.AssertCaption ?? caption);
        return current.Result;
    }
}
