using LibDQB.B2;
using LibDQB.B2.Records;
using MinimapEditor.Viewmodels;
using System.Windows;

namespace MinimapEditorTests;

sealed class AppFacade
{
    private readonly FakeDialogManager fakeDialogManager;
    private readonly StartupViewmodel startupVM;

    private AppFacade()
    {
        fakeDialogManager = new();
        startupVM = new StartupViewmodel()
        {
            DialogManager = fakeDialogManager,
        };
    }

    public static AppFacade Create() => new();

    public void LoadCmndat(string filename)
    {
        Assert.IsTrue(startupVM.CommandBrowse1907.CanExecute(null));
        Assert.IsFalse(startupVM.HasActiveCmndat());

        filename = Path.Combine(Util.FindCmndatFilesDir().FullName, filename);
        fakeDialogManager.nextDialogResult = (true, filename);
        startupVM.CommandBrowse1907.Execute(null);
        Util.WaitFor(() => startupVM.HasActiveCmndat() && startupVM.IslandChoices8930.Count > 0);
    }

    public async Task<SaveResult> SaveCmndat()
    {
        // We could add this pattern to .gitignore but for now I'd
        // rather be alerted if the cleanup deletion fails.
        var filename = Path.Combine(Util.FindCmndatFilesDir().FullName, $"__TESTOUT__{Guid.NewGuid().ToString("D")}.BIN");
        try
        {
            fakeDialogManager.nextDialogResult = (true, filename);
            fakeDialogManager.nextMessageBoxResult = new MessageBoxInterception
            {
                Result = MessageBoxResult.OK,
                AssertText = "Saved Successfully!",
            };
            startupVM.SaveCmndatAs();
            var reloaded = await FileFactory.LoadCommonDataAsync(new FileInfo(filename));
            return new SaveResult(reloaded) { FullPath = filename };
        }
        finally
        {
            // cleanup, but don't crash if it fails
            try { File.Delete(filename); }
            catch (Exception) { }
        }
    }

    public IslandFacade OpenMapEditor(IslandId islandId)
    {
        var islandVM = startupVM.IslandChoices8930.Single(i => i.IslandId2242 == islandId);
        Assert.IsTrue(islandVM.CommandOpenMinimap5775.CanExecute(null));
        islandVM.CommandOpenMinimap5775.Execute(null);
        return new IslandFacade(islandVM, startupVM);
    }

    public sealed record SaveResult
    {
        public required string FullPath { get; init; }
        private readonly RawCommonData cmndat;
        public SaveResult(RawCommonData cmndat)
        {
            this.cmndat = cmndat;
        }

        public void DoSnapshotTest(string snapshotName, params IslandId[] islandIds)
        {
            if (islandIds.Length < 1)
            {
                throw new ArgumentException(nameof(islandIds));
            }

            var snapshotDir = Util.FindSnapshotDir();

            using var stream = new MemoryStream();
            foreach (var islandId in islandIds)
            {
                var map = cmndat.GetMinimap(islandId);
                stream.Write(map.RawBytes);
            }
            stream.Flush();
            var actualBytes = stream.GetBuffer().AsSpan().Slice(0, (int)stream.Position);
            Assert.AreEqual(actualBytes.Length, stream.Position);

            var expectPath = Path.Combine(snapshotDir.FullName, $"{snapshotName}.expected.bin");
            var actualPath = Path.Combine(snapshotDir.FullName, $"{snapshotName}.actual.bin");

            if (File.Exists(expectPath))
            {
                var expectBytes = File.ReadAllBytes(expectPath);
                if (!expectBytes.SequenceEqual(actualBytes))
                {
                    File.WriteAllBytes(actualPath, actualBytes);
                    Assert.Fail($"Snapshots differ, see {actualPath}");
                }
            }
            else
            {
                File.WriteAllBytes(expectPath, actualBytes);
                Assert.Inconclusive($"New snapshot created: {expectPath}");
            }
        }
    }
}
