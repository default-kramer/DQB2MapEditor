using LibDQB;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinimapEditor;

public interface ITileFont
{
    IReadOnlyGrid<bool> CreateText(string text, HashSet<char> missingCharCollector);

    IReadOnlyGrid<bool> CreateText(string text) => CreateText(text, new());
}
