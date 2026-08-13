using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MinimapEditor;

/// <summary>
/// Interaction logic for SaveBackupWarningDialog.xaml
/// </summary>
public partial class SaveBackupWarningDialog : Window
{
    public SaveBackupWarningDialog()
    {
        InitializeComponent();
        DataContext = this;
    }

    private static ImageSource _warningIcon = Imaging.CreateBitmapSourceFromHIcon(
        SystemIcons.Warning.Handle,
        Int32Rect.Empty,
        BitmapSizeOptions.FromEmptyOptions());

    public ImageSource WarningIcon => _warningIcon;

    private void ButtonOk_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        this.Close();
    }

    private void ButtonCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        this.Close();
    }

    public bool DontShowWarningAgain => cbDontShowAgain.IsChecked.GetValueOrDefault(false);
}
