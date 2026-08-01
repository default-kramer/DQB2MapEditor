using LibDQB.B2;
using LibDQB.B2.Records;
using LibDQB.DQB2Minimap;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace MinimapEditor.Viewmodels;

internal class StartupViewmodel : ViewmodelBase
{
    public interface ICallback
    {
        void OpenMap(string cmndatPath, RawCommonData cmndat, IMinimap map);
    }

    private readonly ICallback callback;

    public StartupViewmodel(ICallback callback)
    {
        this.callback = callback;
        CommandBrowse1907 = new RelayCommand(_ => true, _ => Browse());
        CommandOpen9616 = new RelayCommand(_ => CanOpen().HasValue, _ => Open());
        IslandChoices8930 = IslandViewmodel.Islands().ToList();
    }

    public ICommand CommandBrowse1907 { get; }
    public ICommand CommandOpen9616 { get; }

    private string _cmndatPath = "";
    public string CmndatPath2301
    {
        get => _cmndatPath;
        private set => ChangeProperty(ref _cmndatPath, value);
    }

    public IReadOnlyList<IslandViewmodel> IslandChoices8930 { get; }

    private IslandViewmodel? _selectedIsland;
    public IslandViewmodel? SelectedIsland2951
    {
        get => _selectedIsland;
        set => ChangeProperty(ref _selectedIsland, value);
    }

    private void Browse()
    {
        var dialog = new OpenFileDialog();
        dialog.Multiselect = false;
        dialog.Filter = "DQB2 CMNDAT files|CMNDAT.BIN|All files|*.*";
        var sd = TryFindSD();
        if (sd != null)
        {
            dialog.InitialDirectory = sd.FullName;
        }

        if (dialog.ShowDialog().GetValueOrDefault(false))
        {
            CmndatPath2301 = dialog.FileName;
        }
    }

    private (string cmndatPath, IslandId islandId)? CanOpen()
    {
        if (!string.IsNullOrWhiteSpace(CmndatPath2301) && SelectedIsland2951 != null)
        {
            return (CmndatPath2301, SelectedIsland2951.IslandId2242);
        }
        return null;
    }

    private void Open() => DoOpenAsync();

    private async void DoOpenAsync()
    {
        var args = CanOpen();
        if (!args.HasValue)
        {
            return;
        }
        var a = args.Value;

        RawCommonData cmndat;
        try
        {
            cmndat = await FileFactory.LoadCommonDataAsync(new FileInfo(a.cmndatPath));
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Invalid File");
            return;
        }

        var map = cmndat.GetMinimap(a.islandId);
        callback.OpenMap(a.cmndatPath, cmndat, map);
    }

    private static DirectoryInfo? TryFindSD()
    {
        var userprofile = Environment.GetEnvironmentVariable("USERPROFILE");
        if (string.IsNullOrWhiteSpace(userprofile))
        {
            return null;
        }

        var path = $"{userprofile}\\Documents\\My Games\\DRAGON QUEST BUILDERS II\\Steam\\";
        var dir = new DirectoryInfo(path);
        if (!dir.Exists)
        {
            return null;
        }

        var candidates = dir.GetDirectories()
            .Where(dir => BigInteger.TryParse(dir.Name, out _))
            .Select(dir => dir.GetDirectories("SD").FirstOrDefault())
            .Where(dir => dir != null)
            .ToList();

        return candidates.FirstOrDefault();
    }
}
