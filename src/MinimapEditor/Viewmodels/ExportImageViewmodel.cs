using LibDQB;
using LibDQB.DQB2Minimap;
using Microsoft.Win32;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace MinimapEditor.Viewmodels;

sealed class ExportImageViewmodel : ViewmodelBase, IDialogViewmodel
{
    System.Windows.Window IDialogViewmodel.CreateWindow() => new ExportImageDialog();

    public required DialogManager DialogManager { get; init; }
    private readonly Cache cache;
    private readonly Rect fullBounds;
    private readonly Rect visibleBounds;
    private readonly Rect currentCrop;
    private static string? prevExportDir = null;
    public ICommand CommandExport1298 { get; }
    public ICommand CommandBrowse6020 { get; }
    public EventHandler<DialogCloseEventArgs>? CloseRequested { get; set; } = null;

    public ExportImageViewmodel(MapEditorViewmodel.IImageExporter exporter, IReadOnlyGrid<MinimapTile> mapGrid, Rect currentCrop)
    {
        this.fullBounds = mapGrid.Bounds;
        this.visibleBounds = MapEditorViewmodel.CropToVisibleTiles(mapGrid) ?? fullBounds;
        this.currentCrop = currentCrop;

        this.cache = new Cache()
        {
            Exporter = exporter,
            MapGrid = mapGrid,
        };

        RefreshPreviewImage();

        try
        {
            prevExportDir = prevExportDir ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }
        catch (PlatformNotSupportedException) { }
        if (prevExportDir != null)
        {
            SaveToPath3518 = Path.Combine(prevExportDir, $"DQB2_minimap_{DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss.fff")}.png");
        }

        CommandExport1298 = new RelayCommand(_ => CanExport(out var _), _ => Export());
        CommandBrowse6020 = new RelayCommand(_ => true, _ => Browse());
    }

    sealed record PreviewKey
    {
        /// <summary>
        /// Here we use a Rect instead of a <see cref="CropKind"/> to allow kinds
        /// which have the same Rect to share the cached result.
        /// (Probably will only happen when the user's current crop is equal to no crop.)
        /// </summary>
        public required Rect Crop { get; init; }
    }

    sealed record PreviewValue
    {
        public required BitmapFrame Frame { get; init; }
    }

    private string _saveToPath = "";
    public string SaveToPath3518
    {
        get => _saveToPath;
        set
        {
            if (ChangeProperty(ref _saveToPath, value.Trim()))
            {
                var newDir = Path.GetDirectoryName(value);
                if (newDir != null && Directory.Exists(newDir))
                {
                    prevExportDir = newDir;
                }
            }
        }
    }

    public string Dimensions6326
    {
        get => PreviewImage4497 == null ? "" : GetDimensions(PreviewImage4497.PixelWidth, PreviewImage4497.PixelHeight);
    }

    public string PlaceholderDimensions1371 => GetDimensions(9999, 9999);
    private static string GetDimensions(int width, int height) => $"{width} x {height} px";

    private BitmapFrame? _previewImage = null;
    public BitmapFrame? PreviewImage4497
    {
        get => _previewImage;
        private set => ChangeProperty(ref _previewImage, value, nameof(PreviewImage4497), nameof(Dimensions6326));
    }

    private CropKind _selectedCropKind = CropKind.CropToVisibility;
    private CropKind SelectedCropKind
    {
        get => _selectedCropKind;
        set
        {
            if (ChangeProperty(ref _selectedCropKind, value,
                nameof(IsNoCrop7138),
                nameof(IsAutoCrop3562),
                nameof(IsCustomCrop1422)))
            {
                RefreshPreviewImage();
            }
        }
    }

    public bool IsNoCrop7138
    {
        get => SelectedCropKind == CropKind.NoCrop;
        set => SelectedCropKind = CropKind.NoCrop;
    }

    public bool IsAutoCrop3562
    {
        get => SelectedCropKind == CropKind.CropToVisibility;
        set => SelectedCropKind = CropKind.CropToVisibility;
    }

    public bool IsCustomCrop1422
    {
        get => SelectedCropKind == CropKind.CurrentCrop;
        set => SelectedCropKind = CropKind.CurrentCrop;
    }

    enum CropKind
    {
        NoCrop,
        CropToVisibility,
        CurrentCrop,
    }

    private void RefreshPreviewImage()
    {
        Rect rect;
        switch (SelectedCropKind)
        {
            case CropKind.NoCrop:
                rect = fullBounds;
                break;
            case CropKind.CropToVisibility:
                rect = visibleBounds;
                break;
            case CropKind.CurrentCrop:
                rect = currentCrop;
                break;
            default:
                Util.SoftAssertFail();
                rect = visibleBounds;
                break;
        }

        var key = new PreviewKey { Crop = rect };
        var result = cache.GetPreviewValue(key);
        PreviewImage4497 = result.Frame;
    }

    const string pngExtension = ".png";

    private bool CanExport([NotNullWhen(true)] out BitmapFrame? frame)
    {
        frame = PreviewImage4497;
        var extension = Path.GetExtension(SaveToPath3518);

        return pngExtension.Equals(extension, StringComparison.OrdinalIgnoreCase)
            && frame != null;
    }

    private void Export()
    {
        if (!CanExport(out var frame))
        {
            Util.SoftAssertFail();
            return;
        }

        try
        {
            using var stream = new FileStream(SaveToPath3518, FileMode.Create);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(frame);
            encoder.Save(stream);
            stream.Flush();
            stream.Close();
            CloseRequested?.Invoke(this, new DialogCloseEventArgs { DialogResult = true });
        }
        catch (Exception ex)
        {
            DialogManager.ShowError(ex.Message, "Error during Export");
        }
    }

    private void Browse()
    {
        var sfd = new SaveFileDialog();
        sfd.FileName = SaveToPath3518;
        sfd.InitialDirectory = Path.GetDirectoryName(SaveToPath3518) ?? sfd.InitialDirectory;
        sfd.Filter = $"PNG files|*{pngExtension}";
        if (DialogManager.ShowDialog(sfd).GetValueOrDefault(false))
        {
            SaveToPath3518 = sfd.FileName;
        }
    }

    sealed class Cache
    {
        public required MapEditorViewmodel.IImageExporter Exporter { get; init; }
        public required IReadOnlyGrid<MinimapTile> MapGrid { get; init; }
        private readonly ConcurrentDictionary<PreviewKey, PreviewValue> cache = new();

        public PreviewValue GetPreviewValue(PreviewKey key)
        {
            PreviewValue Rebuild(PreviewKey key)
            {
                BitmapFrame frame;
                if (key.Crop == MapGrid.Bounds)
                {
                    frame = Exporter.ExportFullImage();
                }
                else
                {
                    var cropped = MapGrid.Crop(key.Crop);
                    frame = Exporter.ExportCroppedImage(cropped);
                }

                return new PreviewValue { Frame = frame };
            }

            return cache.GetOrAdd(key, Rebuild);
        }
    }
}
