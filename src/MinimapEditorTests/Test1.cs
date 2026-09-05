using LibDQB.B2.Records;
using LibDQB.DQB2Minimap;

namespace MinimapEditorTests;

[TestClass]
public sealed class Test1
{
    [TestMethod]
    public async Task saving_resets_modified_tile_count()
    {
        async Task doTest(bool closeTheTab, string snapshotName)
        {
            var islandId = IslandId.IoA;
            var origTile = MinimapTile.FromRawValue(1);
            var newTile = MinimapTile.FromRawValue(2);
            const int x = 2;
            const int z = 2;

            var app = AppFacade.Create();
            app.LoadCmndat("01_CMNDAT.BIN");
            var map = app.OpenMapEditor(islandId);

            // Setting to origTile is no change...
            map.SetTile(x, z, origTile);
            Assert.AreEqual(0, map.ChangedTileCount);
            // ... but setting to newTile is a change
            map.SetTile(x, z, newTile);
            Assert.AreEqual(1, map.ChangedTileCount);

            if (closeTheTab)
            {
                map.CloseTab();
            }

            var saveResult = await app.SaveCmndat();
            saveResult.DoSnapshotTest(snapshotName, islandId);

            Assert.AreEqual(0, map.ChangedTileCount);

            if (closeTheTab) // need to re-open it for the assertions that follow
            {
                map = app.OpenMapEditor(islandId);
            }

            // Now that we have saved, the newTile should be considered unmodified
            // and the origTile should be considered modified.
            map.SetTile(x, z, newTile);
            Assert.AreEqual(0, map.ChangedTileCount);
            map.SetTile(x, z, origTile);
            Assert.AreEqual(1, map.ChangedTileCount);
            map.SetTile(x, z, newTile);
            Assert.AreEqual(0, map.ChangedTileCount);
        }

        // Snapshot must be identical whether or not the tab is closed
        const string snapshotName = "ff2cd865-0f7c-4f45-b867-6fffeb1cbbaf";

        // Regression: the first fix for this bug didn't work if the tab had been closed.
        await doTest(closeTheTab: false, snapshotName);
        await doTest(closeTheTab: true, snapshotName);
    }
}
