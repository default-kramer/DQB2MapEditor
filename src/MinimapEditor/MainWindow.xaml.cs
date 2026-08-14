using MinimapEditor.Viewmodels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace MinimapEditor;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly StartupViewmodel startupVM;

    public MainWindow()
    {
        InitializeComponent();
        startupVM = new();
        DataContext = startupVM;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        startupVM.OnAppExiting(e);
        base.OnClosing(e);
    }

    private void ContentControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is StartupViewmodel.TabItemViewmodel tabVM
            && tabVM.Viewmodel2249 is MapEditorViewmodel
            && sender is ContentControl cc)
        {
            // This is needed so that the keyboard shortcuts on the map editor control work immediately
            Dispatcher.BeginInvoke(() =>
            {
                var mapEditors = VisualTreeBFS.MakeSimpleFilter<MapEditorControl>().DoBFS(cc).ToList();
                if (mapEditors.Count == 1)
                {
                    mapEditors[0].Focusable = true;
                    mapEditors[0].Focus();
                }
                else
                {
                    Util.SoftAssertFail();
                }
            });
        }
    }
}
