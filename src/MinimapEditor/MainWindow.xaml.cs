using LibDQB;
using LibDQB.B2;
using LibDQB.B2.Records;
using LibDQB.DQB2Minimap;
using MinimapEditor.Viewmodels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MinimapEditor;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, StartupViewmodel.ICallback
{
    public MainWindow()
    {
        InitializeComponent();

        var control = new StartupControl();
        var vm = new StartupViewmodel(this);
        control.DataContext = vm;
        SetContent(control);
    }

    private void SetContent(UIElement element)
    {
        mainGrid.Children.Clear();
        mainGrid.Children.Add(element);
        element.Focusable = true;
        element.Focus();
    }

    public void OpenMap(string cmndatPath, RawCommonData cmndat, IMinimap minimap)
    {
        if (1.ToString() == "nope")
        {
            PutTestPattern(minimap);
        }

        var layers = new BitmapLayers();

        var tileDecorator = new WpfMinimapGrid
        {
            Grid = minimap,
            Layers = layers,
            Dispatcher = this.Dispatcher,
        };

        tileDecorator.RefreshAll();

        var selectionGrid = new Array2D<bool>(minimap.Bounds, false);

        var selectionDecorator = new SelectionGridDecorator
        {
            SelectionGrid = selectionGrid,
            Repainter = layers,
        };
        selectionDecorator.Refresh(selectionGrid.Bounds);

        var viewmodel = new MapEditorViewmodel(tileDecorator, selectionDecorator)
        {
            Cmndat = cmndat,
            CmndatPath3902 = cmndatPath,
            BitmapLayers = layers,
        };

        var control = new MapEditorControl();
        control.DataContext = viewmodel;
        SetContent(control);

        if (1.ToString() == "nope")
        {
            for (byte i = IslandId.IoA.Value; i <= IslandId.Buildertopia3.Value; i++)
            {
                ValidateShores(cmndat.GetMinimap(new IslandId(i)));
            }
        }
    }

    private static void PutTestPattern(IGrid<MinimapTile> minimap)
    {
        for (int i = 0; i < 2048 * 2 / 32; i++)
        {
            int val;
            if (i % 16 == 0)
            {
                val = 1 + 3 * 11;
                val |= 0x8000;
            }
            else if (i % 4 == 0)
            {
                val = 1 + 5 * 11;
                val |= 0x8000;
            }
            else
            {
                val = 1;
            }
            minimap.Set(new XZ(2, i + 3), new MinimapTile { TileValue = val });
        }

        int wantTileId = 0;
        for (int i = 0; i < 2048 * 2; i++)
        {
            int x = i % 32 + 3;
            int z = i / 32 + 3;
            int val = wantTileId * 11 + 1;
            if (val >= 0x4000)
            {
                int baseVal = val / 0x4000 * 0x4000;
                int blah = 1 + baseVal / 11;
                val = baseVal + (wantTileId - blah) * 11 + 1;
            }
            wantTileId++;

            var tile = new MinimapTile { TileValue = val };
            int overlay = tile.SeaTypeIndex switch
            {
                SeaTypeIndex.DeepSea => 7, // mountain
                SeaTypeIndex.ShallowSea => 1, // tree 1
                SeaTypeIndex.ClearWater => 3, // tree 2
                _ => 0,
            };
            overlay = 3;
            val += overlay;
            val |= 0x8000 * 3;
            tile = new MinimapTile { TileValue = val };
            if (tile.BaseTileId < 0)
            {
                tile = new MinimapTile { TileValue = 1 };
            }
            minimap.Set(new XZ(x, z), tile);
        }
    }

    private static void ValidateShores(IReadOnlyGrid<MinimapTile> grid)
    {
        if (grid.Get(new XZ(0, 0)).TileValue == -1)
        {
            return;
        }

        bool hasAlerted = false;

        foreach (var xz in grid.Bounds.Enumerate())
        {
            var tile = grid.Get(xz);
            if (tile.IsVisible && tile.CanHaveShoreline())
            {
                var key = MinimapShorelineKey.Compute(xz, grid);
                if (tile.BaseTileId == key.DeepSeaBaseTileId)
                {
                    // okay
                }
                else if (tile.BaseTileId == key.ShallowSeaBaseTileId)
                {
                    // okay
                }
                else if (tile.BaseTileId == key.ClearWaterBaseTileId)
                {
                    // okay
                }
                else if (!hasAlerted)
                {
                    hasAlerted = true;
                    // Alert! If this map hasn't been hacked, it means that I haven't learned
                    // everything about how Shorelines tiles are chosen.
                    // (But if this is a hacked map, it probably means nothing. Carry on.)
                    System.Diagnostics.Debugger.Break();
                }
            }
        }
    }
}
