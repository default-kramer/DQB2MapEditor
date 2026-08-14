using MinimapEditor.Viewmodels;
using System.Windows;
using System.Windows.Controls;

namespace MinimapEditor;

/// <summary>
/// Interaction logic for StartupControl.xaml
/// </summary>
public partial class StartupControl : UserControl
{
    public StartupControl()
    {
        InitializeComponent();
    }

    private static bool debugResetLatch = false;
    private void SaveCmndatAs_Click(object sender, RoutedEventArgs e)
    {
        var viewmodel = DataContext as StartupViewmodel;
        if (viewmodel == null)
        {
            return;
        }

        if (!debugResetLatch && System.Diagnostics.Debugger.IsAttached)
        {
            // Because I don't want to forget that this functionality exists,
            // reset the flag every time I run with the debugger attached.
            Properties.Settings.Default.DontShowBackupWarningAgain = false;
            debugResetLatch = true;
        }

        bool doSaveAs;
        if (Properties.Settings.Default.DontShowBackupWarningAgain)
        {
            doSaveAs = true;
        }
        else
        {
            var popup = new SaveBackupWarningDialog();
            popup.Owner = this.VisualAncestors().OfType<Window>().FirstOrDefault();
            popup.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            doSaveAs = popup.ShowDialog().GetValueOrDefault(false);
            if (doSaveAs && popup.DontShowWarningAgain)
            {
                Properties.Settings.Default.DontShowBackupWarningAgain = true;
            }
        }

        if (doSaveAs)
        {
            viewmodel.SaveCmndatAs();
        }
    }
}
