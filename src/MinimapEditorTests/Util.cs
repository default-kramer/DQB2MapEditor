using System;
using System.Collections.Generic;
using System.Text;

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
}
