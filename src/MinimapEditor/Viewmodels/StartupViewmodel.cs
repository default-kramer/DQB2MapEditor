using LibDQB.B2;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Numerics;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace MinimapEditor.Viewmodels;

sealed class StartupViewmodel : ViewmodelBase, IslandViewmodel.ICallback
{
    public StartupViewmodel()
    {
        readmeTab = new TabItemViewmodel()
        {
            ClosesWithCmndat = false,
            Viewmodel2249 = new ReadmeViewmodel(),
            Header5924 = "README",
            CommandCloseTab2176 = new RelayCommand(_ => true, _ => CloseReadmeTab()),
        };
        Tabs4685.Add(readmeTab);

        startupTab = new TabItemViewmodel()
        {
            ClosesWithCmndat = false,
            Viewmodel2249 = this,
            Header5924 = "Startup",
            CommandCloseTab2176 = null,
        };
        Tabs4685.Add(startupTab);
        SelectedTab4149 = startupTab;

        CommandBrowse1907 = new RelayCommand(_ => !ActiveCmndat.HasValue, _ => Browse());
        CommandClose5823 = new RelayCommand(_ => ActiveCmndat.HasValue, _ => Close());
        CommandRecheckSteam5759 = new RelayCommand(_ => true, _ => RefreshSteamWarning());
        CommandDismissSteamWarning5909 = new RelayCommand(_ => true, _ => DismissSteamAutocloudWarning());
    }

    public ObservableCollection<TabItemViewmodel> Tabs4685 { get; } = new();

    private TabItemViewmodel? _selectedTab = null;
    public TabItemViewmodel? SelectedTab4149
    {
        get => _selectedTab;
        set => ChangeProperty(ref _selectedTab, value);
    }

    private readonly TabItemViewmodel readmeTab;
    private readonly TabItemViewmodel startupTab;
    private SapphireRetroTilesheet? _tilesheet = null;
    private SapphireRetroTilesheet Tilesheet => Util.LoadOnce(ref _tilesheet, () => SapphireRetroTilesheet.Instance);
    private DataDefinitions? _dataDefinitions = null;
    private DataDefinitions DataDefinitions => Util.LoadOnce(ref _dataDefinitions, () => new DataDefinitions(Tilesheet));

    public ICommand CommandBrowse1907 { get; }
    public ICommand CommandClose5823 { get; }
    public ICommand CommandRecheckSteam5759 { get; }
    public ICommand CommandDismissSteamWarning5909 { get; }

    private readonly record struct LoadedCmndat(string FullPath, RawCommonData Cmndat);
    private LoadedCmndat? _activeCmndat = null;
    private LoadedCmndat? ActiveCmndat
    {
        get => _activeCmndat;
        set => ChangeProperty(ref _activeCmndat, value,
            nameof(CmndatPath2301),
            nameof(CanSave8786),
            nameof(WindowTitle8643));
    }

    public string CmndatPath2301 => ActiveCmndat?.FullPath ?? "";
    public bool CanSave8786 => ActiveCmndat.HasValue;
    public string WindowTitle8643 => ActiveCmndat.HasValue
        ? $"Map Editor for DQB2 - {ActiveCmndat?.FullPath}"
        : $"Map Editor for DQB2";

    private IReadOnlyList<IslandViewmodel> _islandChoices = [];
    public IReadOnlyList<IslandViewmodel> IslandChoices8930
    {
        get => _islandChoices;
        private set => ChangeProperty(ref _islandChoices, value);
    }

    private IslandViewmodel? _selectedIsland;
    public IslandViewmodel? SelectedIsland2951
    {
        get => _selectedIsland;
        set => ChangeProperty(ref _selectedIsland, value);
    }

    public void OnAppExiting(CancelEventArgs args)
    {
        if (HasUnsavedChanges())
        {
            // Switch to Startup Tab before popping the question
            // so the user can see which maps have unsaved changes.
            SelectedTab4149 = startupTab;
            args.Cancel = !Close();
        }
    }

    private bool HasUnsavedChanges() => IslandChoices8930.Where(i => i.ChangedTileCount4506 > 0).Any();

    private bool Close()
    {
        if (HasUnsavedChanges())
        {
            var result = MessageBox.Show($"You have unsaved changes. Close anyway?", "Discard Changes?",
                MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.No);
            if (result != MessageBoxResult.Yes)
            {
                return false;
            }
        }

        ActiveCmndat = null;
        IslandChoices8930 = [];
        SelectedIsland2951 = null;
        for (int i = Tabs4685.Count - 1; i >= 0; i--)
        {
            if (Tabs4685[i].ClosesWithCmndat)
            {
                Tabs4685.RemoveAt(i);
            }
        }

        hasDismissedSteamAutocloudWarning = false;
        RefreshSteamWarning();
        return true;
    }

    private void Browse()
    {
        if (ActiveCmndat.HasValue)
        {
            Util.SoftAssertFail();
            return;
        }

        var dialog = new OpenFileDialog();
        dialog.Multiselect = false;
        dialog.Filter = "DQB2 CMNDAT files|*CMNDAT.BIN|All files|*.*";
        var sd = TryFindSD();
        if (sd != null)
        {
            dialog.InitialDirectory = sd.FullName;
        }

        if (dialog.ShowDialog().GetValueOrDefault(false))
        {
            LoadCmndat(dialog.FileName);
        }
    }

    private async void LoadCmndat(string fullPath)
    {
        RawCommonData cmndat;
        try
        {
            cmndat = await FileFactory.LoadCommonDataAsync(new FileInfo(fullPath));
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Invalid File");
            return;
        }
        ActiveCmndat = new LoadedCmndat(fullPath, cmndat);
        startupTab.Header5924 = Path.GetFileName(fullPath);

        var deps = new IslandViewmodel.Dependencies
        {
            Cmndat = cmndat,
            DataDefinitions = DataDefinitions,
            Tilesheet = Tilesheet,
            Callback = this,
        };

        IslandChoices8930 = IslandViewmodel.Islands().Select(item => new IslandViewmodel(deps, item.Item2)
        {
            IslandName3332 = item.Item1,
        }).ToList();

        RefreshSteamWarning();
    }

    private bool hasDismissedSteamAutocloudWarning = false;
    private void DismissSteamAutocloudWarning()
    {
        hasDismissedSteamAutocloudWarning = true;
        RefreshSteamWarning();
    }

    private bool _showSteamAutocloudWarning;
    public bool ShowSteamAutocloudWarning2483
    {
        get => _showSteamAutocloudWarning;
        private set => ChangeProperty(ref _showSteamAutocloudWarning, value);
    }

    private void RefreshSteamWarning()
    {
        ShowSteamAutocloudWarning2483 = !hasDismissedSteamAutocloudWarning
            && HasSteamAutocloud(ActiveCmndat)
            && !IsSteamRunning();
    }

    private static bool HasSteamAutocloud(LoadedCmndat? ActiveCmndat)
    {
        if (!ActiveCmndat.HasValue)
        {
            return false;
        }

        var file = new FileInfo(ActiveCmndat.Value.FullPath);
        var found = file?.Directory?.EnumerateFiles("steam_autocloud.vdf")?.Any();
        return found.GetValueOrDefault(false);
    }

    private static bool IsSteamRunning()
    {
        return System.Diagnostics.Process.GetProcessesByName("steam").Any()
            || System.Diagnostics.Process.GetProcessesByName("SteamService").Any();
    }

    void IslandViewmodel.ICallback.OpenMinimap(IslandViewmodel islandVM)
    {
        if (!IslandChoices8930.Contains(islandVM))
        {
            return;
        }

        var islandId = islandVM.IslandId2242;

        var tab = Tabs4685
            .Where(t => t.Viewmodel2249 is MapEditorViewmodel mev && mev.IslandId == islandId)
            .SingleOrDefault();

        if (tab == null)
        {
            var vm = islandVM.GetMapEditorVM();
            tab = new TabItemViewmodel
            {
                ClosesWithCmndat = true,
                Header5924 = islandVM.IslandName3332,
                Viewmodel2249 = vm,
                CommandCloseTab2176 = new RelayCommand(_ => true, _ => CloseTab(vm)),
            };
            Tabs4685.Add(tab);
        }

        SelectedTab4149 = tab;
    }

    private void CloseTab(MapEditorViewmodel vm)
    {
        var tab = Tabs4685.SingleOrDefault(t => t.Viewmodel2249 == vm);
        CloseTab(tab);
    }

    private void CloseReadmeTab() => CloseTab(readmeTab);

    private void CloseTab(TabItemViewmodel? tab)
    {
        if (tab != null && Tabs4685.Contains(tab))
        {
            if (SelectedTab4149 == tab)
            {
                SelectedTab4149 = startupTab;
            }
            Tabs4685.Remove(tab);
        }
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

    public void SaveCmndatAs()
    {
        if (!ActiveCmndat.HasValue)
        {
            return;
        }

        var prevFullPath = ActiveCmndat.Value.FullPath;

        var saveDialog = new SaveFileDialog();
        saveDialog.FileName = ActiveCmndat.Value.FullPath;
        saveDialog.Filter = "DQB2 CMNDAT files|*CMNDAT.BIN|All files|*.*";

        bool ok = saveDialog.ShowDialog().GetValueOrDefault(false);
        if (!ok)
        {
            return;
        }

        ActiveCmndat = ActiveCmndat.Value with { FullPath = saveDialog.FileName };

        var Cmndat = ActiveCmndat.Value.Cmndat;
        Cmndat.LastSaveTime = DateTime.UtcNow.AddYears(1000);
        Cmndat.Save(saveDialog.FileName);

        if (!string.Equals(ActiveCmndat.Value.FullPath.Trim(), prevFullPath.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            hasDismissedSteamAutocloudWarning = false;
            RefreshSteamWarning();
        }

        MessageBox.Show("Saved Successfully!", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public sealed class TabItemViewmodel : ViewmodelBase
    {
        internal required bool ClosesWithCmndat { get; init; }
        public required object Viewmodel2249 { get; init; }
        public required ICommand? CommandCloseTab2176 { get; init; }
        public bool CanCloseTab4739 => CommandCloseTab2176 != null;

        private string _header = "";
        public required string Header5924
        {
            get => _header;
            set => ChangeProperty(ref _header, value);
        }
    }
}
