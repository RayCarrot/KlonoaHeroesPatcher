using BinarySerializer;
using BinarySerializer.Klonoa.KH;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using BinarySerializer.GBA;

namespace KlonoaHeroesPatcher;

public class GraphicsFileEditorViewModel : FileEditorViewModel
{
    public GraphicsFileEditorViewModel()
    {
        ExportImageCommand = new RelayCommand(ExportImage);
        ImportImageCommand = new RelayCommand(ImportImage);
    }

    public Graphics_File GraphicsFile => (Graphics_File)SerializableObject;

    public ICommand ExportImageCommand { get; }
    public ICommand ImportImageCommand { get; }

    public ObservableCollection<DuoGridItemViewModel> InfoItems { get; set; }
    public BitmapSource PreviewImgSource { get; set; }
    public ObservableCollection<GraphicsMapTileViewModel> MapTiles { get; set; }
    public int MapTilesWidth { get; set; }

    private GraphicsMapTileViewModel _selectedMapTile;
    public GraphicsMapTileViewModel SelectedMapTile
    {
        get => _selectedMapTile;
        set
        {
            if (value == SelectedMapTile)
                return;

            _selectedMapTile = value;

            if (MapTiles == null)
                return;

            foreach (GraphicsMapTileViewModel tile in MapTiles)
                tile.IsHighlighted = tile != value && tile.Tile.TileIndex == value?.Tile.TileIndex;
        }
    }
    public int SelectedMapTileIndex { get; set; }

    public BitmapSource PalettePreviewImgSource { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public bool HasPalette => GraphicsFile.Palette?.Any() == true;
    public bool IsImageLoaded { get; set; }

    public bool CanChangeBasePalette { get; set; }
    public int MinBasePalette { get; set; }
    public int MaxBasePalette { get; set; }

    private int _basePalette;
    public int BasePalette
    {
        get => _basePalette;
        set
        {
            if (BasePalette == value)
                return;

            _basePalette = value;
            RefreshImage();
        }
    }

    public override void Load(bool firstLoad)
    {
        if (firstLoad)
            RefreshBasePalette();

        RefreshImage();

        if (HasPalette)
            PalettePreviewImgSource = TileGraphicsHelpers.CreatePaletteImageSource(
                bmpPal: new BitmapPalette(ColorHelpers.ConvertColors(GraphicsFile.Palette, GraphicsFile.BPP, false)),
                scale: 16,
                optionalWrap: 16);

        int tileSize = (int)(TileGraphicsHelpers.TileWidth * TileGraphicsHelpers.TileHeight / (8f / GraphicsFile.BPP));

        InfoItems = new ObservableCollection<DuoGridItemViewModel>()
        {
            new DuoGridItemViewModel("Size", $"{Width}x{Height}"),
            new DuoGridItemViewModel("Colors", $"{GraphicsFile.Palette?.Length ?? 0}"),
            new DuoGridItemViewModel("BPP", $"{GraphicsFile.BPP}"),
            new DuoGridItemViewModel("Tiles", $"{GraphicsFile.TileSet.Length / tileSize}"),
            new DuoGridItemViewModel("Has map", $"{GraphicsFile.TileMap?.Any() == true}"),
            new DuoGridItemViewModel("Is affine", $"{GraphicsFile.IsAffine}"),
        };
    }

    public override void Unload()
    {
        PreviewImgSource = null;
        IsImageLoaded = false;
        MapTiles = null;
        PalettePreviewImgSource = null;
        InfoItems = null;
    }

    public void RefreshBasePalette()
    {
        CanChangeBasePalette = GraphicsFile.BPP == 4;

        _basePalette = 0;

        if (CanChangeBasePalette)
        {
            if (GraphicsFile.TileMap.Any())
            {
                MaxBasePalette = 15 - GraphicsFile.TileMap.Max(x => x.PaletteIndex);
                MinBasePalette = -GraphicsFile.TileMap.Min(x => x.PaletteIndex);
                _basePalette = MinBasePalette;
            }
            else
            {
                MaxBasePalette = 15;
                MinBasePalette = 0;
                _basePalette = 0;
            }

            OnPropertyChanged(nameof(BasePalette));
        }
    }

    public void RefreshImage()
    {
        try
        {
            PreviewImgSource = TileGraphicsHelpers.CreateImageSource(
                tileSet: GraphicsFile.TileSet,
                bpp: GraphicsFile.BPP,
                palette: GraphicsFile.Palette,
                tileMap: GraphicsFile.TileMap,
                width: GraphicsFile.TileMapWidth,
                height: GraphicsFile.TileMapHeight,
                basePalette: BasePalette);

            Width = PreviewImgSource.Width;
            Height = PreviewImgSource.Height;

            IsImageLoaded = true;
        }
        catch (Exception ex)
        {
            PreviewImgSource = null;
            IsImageLoaded = false;
            MessageBox.Show($"Error loading image. Error: {ex.Message}", "Error loading image", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        try
        {
            if (GraphicsFile.TileMap.Any() && PreviewImgSource != null)
            {
                MapTiles = new ObservableCollection<GraphicsMapTileViewModel>();

                var tilesWidth = GraphicsFile.TileMapWidth / TileGraphicsHelpers.TileWidth;
                var tilesHeight = GraphicsFile.TileMapHeight / TileGraphicsHelpers.TileHeight;

                MapTilesWidth = tilesWidth;

                for (int y = 0; y < tilesHeight; y++)
                {
                    for (int x = 0; x < tilesWidth; x++)
                    {
                        var bmp = new CroppedBitmap(PreviewImgSource, new Int32Rect(
                            x: x * TileGraphicsHelpers.TileWidth, 
                            y: y * TileGraphicsHelpers.TileHeight, 
                            width: TileGraphicsHelpers.TileWidth, 
                            height: TileGraphicsHelpers.TileHeight));

                        var tilesCount = (int)(GraphicsFile.TileSetLength / (TileGraphicsHelpers.TileWidth * TileGraphicsHelpers.TileHeight * (GraphicsFile.BPP / 8f)));

                        MapTiles.Add(new GraphicsMapTileViewModel(this, bmp, GraphicsFile.TileMap[y * tilesWidth + x], tilesCount, GraphicsFile.BPP, GraphicsFile.IsAffine));
                    }
                }
            }
            else
            {
                MapTiles = null;
            }
        }
        catch (Exception ex)
        {
            MapTiles = null;
            MessageBox.Show($"Error loading image map. Error: {ex.Message}", "Error loading image map", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void ExportImage()
    {
        var dialog = new SaveFileDialog()
        {
            Title = "Export image",
            Filter = "Image Files (*.png, *.jpg)|*.png;*.jpg",
            FileName = $"{NavigationItem.DisplayOffset}.png"
        };

        var result = dialog.ShowDialog();

        if (result != true)
            return;

        try
        {
            BitmapEncoder encoder = Path.GetExtension(dialog.FileName) switch
            {
                ".png" => new PngBitmapEncoder(),
                ".jpg" => new JpegBitmapEncoder(),
                _ => throw new Exception("Invalid file extension"),
            };

            using var fileStream = new FileStream(dialog.FileName, FileMode.Create);
            encoder.Frames.Add(BitmapFrame.Create(PreviewImgSource));
            encoder.Save(fileStream);

            MessageBox.Show($"The image was successfully exported");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An error occurred when exporting. Error: {ex.Message}", "Error exporting", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void ImportImage()
    {
        var dialog = new OpenFileDialog()
        {
            Title = "Import image",
            Filter = "Image Files (*.png, *.jpg)|*.png;*.jpg"
        };

        var result = dialog.ShowDialog();

        if (result != true)
            return;

        try
        {
            // Read the image
            var img = new BitmapImage(new Uri(dialog.FileName));

            // Make sure the image can be tiled
            if (img.PixelWidth % TileGraphicsHelpers.TileWidth != 0 || img.PixelHeight % TileGraphicsHelpers.TileHeight != 0)
            {
                MessageBox.Show($"The image dimensions are invalid. Make sure the width and height are a multiple of 8.", "Error importing", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Get the raw image data
            byte[] imgData = new byte[(int)(img.PixelWidth * img.PixelHeight * (img.Format.BitsPerPixel / 8f))];
            img.CopyPixels(imgData, TileGraphicsHelpers.GetStride(img.PixelWidth, img.Format), 0);

            // Create the tile set and map
            (byte[] tileSet, MapTile[] tileMap) = TileGraphicsHelpers.CreateTileData(
                srcImgData: imgData, 
                srcFormat: img.Format, 
                srcPalette: img.Palette,
                dstBpp: GraphicsFile.BPP, 
                dstPalette: GraphicsFile.Palette, 
                width: img.PixelWidth, height: img.PixelHeight, 
                createMap: GraphicsFile.HasTileMap,
                basePalette: BasePalette + MinBasePalette);

            // Update the properties
            GraphicsFile.TileMapWidth = (ushort)img.PixelWidth;
            GraphicsFile.TileMapHeight = (ushort)img.PixelHeight;
            GraphicsFile.TileSet = tileSet;
            GraphicsFile.TileSetLength = (uint)tileSet.Length;
            GraphicsFile.TileMap = tileMap;
            GraphicsFile.TileMapLength = (uint)(tileMap.Length * (GraphicsFile.IsAffine ? 1 : 2));
            GraphicsFile.TileMapOffset = GraphicsFile.TileSetOffset + GraphicsFile.TileSetLength;

            // Relocate the data
            RelocateFile();

            // Reload
            Load(true);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An error occurred when importing. Error: {ex.Message}", "Error importing", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}