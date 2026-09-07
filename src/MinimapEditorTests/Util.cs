using LibDQB;
using LibDQB.DQB2Minimap;

namespace MinimapEditorTests;

static class Util
{
    /// <summary>
    /// Because some WPF commands are `async void` and tests need to wait for them to complete.
    /// </summary>
    public static void WaitFor(Func<bool> predicate, TimeSpan? timeout = null)
    {
        var endTime = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(10));
        while (DateTime.UtcNow < endTime)
        {
            if (predicate())
            {
                return;
            }
            Thread.Yield();
        }
        throw new Exception("Timeout expired");
    }

    private static DirectoryInfo FindTestProjectDir()
    {
        const string csprojName = "MinimapEditorTests.csproj";
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            if (dir.GetFiles(csprojName).Length > 0)
            {
                return dir;
            }
            dir = dir.Parent;
        }

        throw new Exception($"Failed to find {csprojName}");
    }

    public static DirectoryInfo FindCmndatFilesDir()
    {
        return FindTestProjectDir().GetDirectories("CmndatFiles").Single();
    }

    public static DirectoryInfo FindSnapshotDir()
    {
        return FindTestProjectDir().GetDirectories("Snapshots").Single();
    }

    public static void DoSnapshotTest(string snapshotName, IReadOnlyGrid<MinimapTile> map)
    {
        int size = map.Bounds.Size.X * map.Bounds.Size.Z * 2;
        var buffer = new byte[size];
        var stream = new MemoryStream(buffer);
        foreach (var xz in map.Bounds.Enumerate())
        {
            var value = map.Get(xz).TileValue;
            byte lo = (byte)(value & 0xFF);
            byte hi = (byte)((value >> 8) & 0xFF);
            stream.WriteByte(lo);
            stream.WriteByte(hi);
        }

        Assert.AreEqual(size, stream.Position);
        DoSnapshotTest(snapshotName, buffer.AsSpan());
    }

    public static void DoSnapshotTest(string snapshotName, ReadOnlySpan<byte> actualBytes)
    {
        var snapshotDir = FindSnapshotDir();

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
