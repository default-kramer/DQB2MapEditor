using LibDQB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MinimapEditor;

static class TileFontManager
{
    public static readonly ITileFont DefaultFont;

    static TileFontManager()
    {
        if (!SimpleTileFont.TryLoad(DefaultFontFileContent.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None), out var defaultFont))
        {
            throw new Exception("Assert fail");
        }
        DefaultFont = defaultFont;

        var fontDir = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "Content", "TileFonts"));
        try
        {
            fontDir.Create();
            File.WriteAllText(Path.Combine(fontDir.FullName, DefaultFontFilename), DefaultFontFileContent);
        }
        catch (Exception)
        {
            return;
        }

        // Future work: load all fonts from fontDir and allow the user to choose which font they want
    }

    sealed class SimpleTileFont : ITileFont
    {
        sealed record TileChar
        {
            public required char Character { get; init; }
            public required IReadOnlyGrid<bool> Grid { get; init; }
            public required int HorizontalSpace { get; init; }
        }

        private readonly IReadOnlyDictionary<char, TileChar> chars;
        private readonly TileChar? defaultFallbackChar;

        private SimpleTileFont(IReadOnlyDictionary<char, TileChar> chars, TileChar? defaultFallback)
        {
            this.chars = chars;
            this.defaultFallbackChar = defaultFallback;
        }

        public IReadOnlyGrid<bool> CreateText(string text, HashSet<char> missingCharCollector)
        {
            text = text.Replace("\r\n", "\n");

            var grid = new ExpandableGrid<bool>(false, new Rect(XZ.Zero, new XZ(20, 10)));
            XZ position = XZ.Zero;
            int nextZ = position.Z;
            const int verticalSpace = 1;

            void PutChar(TileChar tileChar)
            {
                var src = tileChar.Grid.TranslateTo(position);
                grid.Set(src.Bounds.Start, false); // expand bounds before CopyFrom
                grid.Set(src.Bounds.End.Add(-1, -1), false);
                grid.CopyFrom(src);

                int dx = tileChar.Grid.Bounds.Size.X + tileChar.HorizontalSpace;
                position = position.Add(dx, 0);
                nextZ = Math.Max(nextZ, position.Z + tileChar.Grid.Bounds.Size.Z + verticalSpace);
            }

            var itr = text.GetEnumerator();
            while (itr.MoveNext())
            {
                if (chars.TryGetValue(itr.Current, out var tileChar))
                {
                    PutChar(tileChar);
                }
                else if (itr.Current == '\n')
                {
                    position = XZ.Zero.Add(0, nextZ);
                }
                else
                {
                    missingCharCollector.Add(itr.Current);
                    if (defaultFallbackChar != null)
                    {
                        PutChar(defaultFallbackChar);
                    }
                }
            }

            return grid;
        }

        private static bool IsNotComment(string line) => !line.StartsWith("//");

        public static bool TryLoad(FileInfo fontFile, out SimpleTileFont font)
        {
            var lines = File.ReadAllLines(fontFile.FullName);
            return TryLoad(lines, out font);
        }

        public static bool TryLoad(IEnumerable<string> fileLines, out SimpleTileFont font)
        {
            var lines = new Queue<string>(fileLines.Where(IsNotComment));

            Dictionary<char, TileChar> chars = new();

            while (lines.TryDequeue(out var line))
            {
                if (line.Length == 1)
                {
                    char ch = line[0];
                    if (TryBuildChar(ch, lines, out var tileChar))
                    {
                        chars[ch] = tileChar;
                    }
                }
            }

            if (chars.Count == 0)
            {
                font = null!;
                return false;
            }

            var fallback = BuildDefaultFallback(chars, InclusiveRange('A', 'Z').Concat(InclusiveRange('0', '9')))
                ?? BuildDefaultFallback(chars, InclusiveRange('a', 'z').Concat(InclusiveRange('0', '9')))
                ?? BuildDefaultFallback(chars, chars.Keys);

            SetupFallbacks(chars);

            font = new SimpleTileFont(chars, fallback);
            return true;
        }

        private static bool TryBuildChar(char ch, Queue<string> lines, out TileChar tileChar)
        {
            int width;
            if (!lines.TryPeek(out var firstLine))
            {
                tileChar = null!;
                return false;
            }

            width = firstLine.Replace("_", "").Length;
            List<bool[]> grid = new();

            int horizontalSpace = 0;

            while (lines.TryPeek(out var line) && line.Length >= width)
            {
                line = lines.Dequeue();
                var lineArray = new bool[width];
                for (int i = 0; i < width; i++)
                {
                    lineArray[i] = IsOn(line[i]);
                }
                grid.Add(lineArray);
                horizontalSpace = Math.Max(horizontalSpace, line.Length - width);
            }

            if (grid.Count == 0)
            {
                tileChar = null!;
                return false;
            }

            var array = new Array2D<bool>(new Rect(XZ.Zero, new XZ(width, grid.Count)), false);
            array.CopyFrom(grid);
            tileChar = new TileChar
            {
                Character = ch,
                Grid = array,
                HorizontalSpace = horizontalSpace,
            };
            return true;
        }

        private static bool IsOn(char ch)
        {
            if (Char.IsWhiteSpace(ch))
            {
                return false;
            }

            switch (ch)
            {
                case '.':
                case '_':
                    return false;
                default:
                    return true;
            }
        }

        private static IEnumerable<char> InclusiveRange(char min, char max)
        {
            if (max < min)
            {
                throw new ArgumentException(nameof(max));
            }

            while (min <= max)
            {
                yield return min;
                min++;
            }
        }

        private static void SetupFallbacks(Dictionary<char, TileChar> font)
        {
            foreach (char lower in InclusiveRange('a', 'z'))
            {
                char upper = char.ToUpperInvariant(lower);
                var lowerTile = font.GetValueOrDefault(lower);
                var upperTile = font.GetValueOrDefault(upper);

                if (lowerTile == null && upperTile != null)
                {
                    font[lower] = upperTile;
                }
                if (upperTile == null && lowerTile != null)
                {
                    font[upper] = lowerTile;
                }
            }
        }

        private static TileChar? BuildDefaultFallback(IReadOnlyDictionary<char, TileChar> font, IEnumerable<char> sampleFrom)
        {
            var chars = sampleFrom.Select(font.GetValueOrDefault)
                .WhereNotNull()
                .ToList();

            if (chars.Count == 0)
            {
                return null;
            }

            T GetMostCommonValue<T>(Func<TileChar, T> selector) => chars.Select(selector)
                .GroupBy(val => val)
                .OrderByDescending(grp => grp.Count())
                .First()
                .First();

            var width = GetMostCommonValue(x => x.Grid.Bounds.Size.X);
            var height = GetMostCommonValue(x => x.Grid.Bounds.Size.Z);
            var horizontalSpace = GetMostCommonValue(x => x.HorizontalSpace);

            var grid = new ConstantGrid<bool>
            {
                Bounds = new Rect(XZ.Zero, new XZ(width, height)),
                Value = true,
            };

            return new TileChar
            {
                Character = (char)0,
                Grid = grid,
                HorizontalSpace = horizontalSpace,
            };
        }
    }

    private const string DefaultFontFilename = "default-font.en.txt";

    private const string DefaultFontFileContent = @$"// {DefaultFontFilename} is a an auto-generated file.
// If you make changes, save using a different filename.
//
// Any line starting with ""//"" is a comment.
//
// The definition of a letter or character is:
//  * the letter on a line by itself,
//  * followed by some lines of ASCII art
//  * followed by a blank line
// The first line of the ASCII art defines the width of that letter.
// Within the ASCII art, the '.' and '_' characters indicate emptiness.
// All other (non-whitespace) characters indicate non-emptiness.
// (This file uses 'X' for non-emptiness.)

A
..X..
.X.X.
X...X
X...X
XXXXX
X...X
X...X
X...X_

// The underscore above is not part of the letter
// (because it extends past the width of 5 defined by the first line)
// but it does define how much horizontal space should follow the letter.
// If the last line were ""X...X__"" then two tiles of space would be used instead of one.

B
XXXX.
X...X
X...X
XXXX.
X...X
X...X
X...X
XXXX._

C
.XXX.
X...X
X....
X....
X....
X....
X...X
.XXX._

D
XXXX.
X...X
X...X
X...X
X...X
X...X
X...X
XXXX._

E
XXXXX
X....
X....
XXXX.
X....
X....
X....
XXXXX_

F
XXXXX
X....
X....
XXXX.
X....
X....
X....
X...._

G
.XXX.
X...X
X....
X....
X.XXX
X...X
X...X
.XXX._

H
X...X
X...X
X...X
XXXXX
X...X
X...X
X...X
X...X_

I
XXXXX
..X..
..X..
..X..
..X..
..X..
..X..
XXXXX_

J
....X
....X
....X
....X
....X
X...X
X...X
.XXX._

K
X...X
X..X.
X.X..
XX...
XX...
X.X..
X..X.
X...X_

L
X....
X....
X....
X....
X....
X....
X....
XXXXX_

M
X...X
XX.XX
X.X.X
X...X
X...X
X...X
X...X
X...X_

N
X...X
XX..X
XX..X
X.X.X
X.X.X
X..XX
X..XX
X...X_

O
.XXX.
X...X
X...X
X...X
X...X
X...X
X...X
.XXX._

P
XXXX.
X...X
X...X
XXXX.
X....
X....
X....
X...._

Q
.XXX.
X...X
X...X
X...X
X...X
X.X.X
X..X.
.XX.X_

R
XXXX.
X...X
X...X
XXXX.
X.X..
X..X.
X...X
X...X_

S
.XXX.
X...X
X...X
.XX..
...X.
X...X
X...X
.XXX._

T
XXXXX
..X..
..X..
..X..
..X..
..X..
..X..
..X.._

U
X...X
X...X
X...X
X...X
X...X
X...X
X...X
.XXX._

V
X...X
X...X
X...X
X...X
X...X
X...X
.X.X.
..X.._

W
X...X
X...X
X...X
X...X
X...X
X.X.X
XX.XX
X...X_

X
X...X
X...X
.X.X.
..X..
.X.X.
X...X
X...X
X...X_

Y
X...X
X...X
X...X
.X.X.
..X..
..X..
..X..
..X.._

Z
XXXXX
....X
...X.
..X..
.X...
.X...
X....
XXXXX_

// space character, width=3 with no trailing spacing:
 
...
...
...
...
...
...
...
.._

!
X
X
X
X
X
X
.
X_

// period, 1 trailing space
.
.
.
.
.
.
.
.
X_

0
.XXX.
X...X
X...X
X.X.X
X.X.X
X...X
X...X
.XXX._

1
...X
..XX
.X.X
X..X
...X
...X
...X
...X_

2
.XXX.
X...X
X...X
...X.
..X..
.X...
X....
XXXXX

3
.XXX.
X...X
....X
..XX.
....X
....X
X...X
.XXX._

4
...XX
..X.X
.X..X
X...X
XXXXX
....X
....X
....X_

5
XXXXX
X....
X....
XXXX.
....X
....X
X...X
.XXX._

6
.XXX.
X...X
X....
XXXX.
X...X
X...X
X...X
.XXX._

7
XXXXX
....X
....X
...X.
...X.
..X..
..X..
..X.._

8
.XXX.
X...X
X...X
.XXX.
X...X
X...X
X...X
.XXX._

9
.XXX.
X...X
X...X
.XXXX
....X
....X
X...X
.XXX._

?
.XXX.
X...X
....X
...X.
..X..
..X..
.....
..X.._

(
.XX
X..
X..
X..
X..
X..
X..
.XX_

)
XX.
..X
..X
..X
..X
..X
..X
XX._

[
XXX
X..
X..
X..
X..
X..
X..
XXX_

]
XXX
..X
..X
..X
..X
..X
..X
XXX_

{"{"}
..XX
.X..
.X..
X...
.X..
.X..
.X..
..XX_

{"}"}
XX..
..X.
..X.
...X
..X.
..X.
..X.
XX.._

@
.XXXX.
X....X
X.XX.X
X.X.XX
X.X..X
X..XX.
X.....
.XXXX._

'
X
X
.
.
.
.
.
._

";
}
