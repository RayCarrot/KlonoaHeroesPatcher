using BinarySerializer.Klonoa.KH;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BinarySerializer;

namespace KlonoaHeroesPatcher
{
    public class GraphicsFileEditorViewModel : FileEditorViewModel
    {
        public GraphicsFileEditorViewModel()
        {
            ExportImageCommand = new RelayCommand(ExportImage);
            ImportImageCommand = new RelayCommand(() => ImportImage(false));
            ImportImageWithPaletteCommand = new RelayCommand(() => ImportImage(true));
        }

        public Graphics_File GraphicsFile => (Graphics_File)SerializableObject;

        public ICommand ExportImageCommand { get; }
        public ICommand ImportImageCommand { get; }
        public ICommand ImportImageWithPaletteCommand { get; }

        public ObservableCollection<DuoGridItemViewModel> InfoItems { get; set; }
        public BitmapSource PreviewImgSource { get; set; }
        public BitmapSource PalettePreviewImgSource { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public bool HasPalette => GraphicsFile.Palette?.Any() == true;

        protected override void Load()
        {
            PreviewImgSource = TileGraphicsHelpers.CreateImageSource(
                tileSet: GraphicsFile.TileSet, 
                bpp: GraphicsFile.BPP, 
                palette: GraphicsFile.Palette, 
                tileMap: GraphicsFile.TileMap, 
                width: GraphicsFile.TileMapWidth, 
                height: GraphicsFile.TileMapHeight,
                trimPalette: true);

            Width = PreviewImgSource.Width;
            Height = PreviewImgSource.Height;

            if (HasPalette)
                PalettePreviewImgSource = TileGraphicsHelpers.CreatePaletteImageSource(
                    bmpPal: new BitmapPalette(TileGraphicsHelpers.ConvertColors(GraphicsFile.Palette, GraphicsFile.BPP, false)), 
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
            };
        }

        protected override object GetEditor() => new GraphicsFileEditor();

        public void ExportImage()
        {
            Pointer offset = GraphicsFile.Offset.File is VirtualFile virtualFile ? virtualFile.ParentPointer : GraphicsFile.Offset;

            var dialog = new SaveFileDialog()
            {
                Title = "Export image",
                Filter = "Image Files (*.png, *.jpg)|*.png;*.jpg",
                FileName = $"{offset.StringAbsoluteOffset}_{GraphicsFile.BPP}bit.png"
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

        public void ImportImage(bool includePalette)
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
                var img = new BitmapImage(new Uri(dialog.FileName));

                PixelFormat expectedFormat = TileGraphicsHelpers.GetPixelFormat(GraphicsFile.BPP);

                if (img.Format != expectedFormat)
                {
                    MessageBox.Show($"The image file has to be saved as an {GraphicsFile.BPP}-bit file in order to be imported", "Error importing", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Make sure the image can be tiled
                if (img.PixelWidth % TileGraphicsHelpers.TileWidth != 0 || img.PixelHeight % TileGraphicsHelpers.TileHeight != 0)
                {
                    MessageBox.Show($"The image dimensions are invalid. Make sure the width and height are a multiple of 8.", "Error importing", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                byte[] imgData = new byte[(int)(img.PixelWidth * img.PixelHeight * (img.Format.BitsPerPixel / 8f))];
                img.CopyPixels(imgData, TileGraphicsHelpers.GetStride(img.PixelWidth, img.Format), 0);

                (var tileSet, GraphicsTile[] tileMap) = TileGraphicsHelpers.CreateTileData(imgData, img.Format.BitsPerPixel, img.PixelWidth, img.PixelHeight, GraphicsFile.TileMapLength != 0);

                // Update the properties
                GraphicsFile.TileMapWidth = (ushort)img.PixelWidth;
                GraphicsFile.TileMapHeight = (ushort)img.PixelHeight;
                GraphicsFile.TileSet = tileSet;
                GraphicsFile.TileSetLength = (uint)tileSet.Length;
                GraphicsFile.TileMap = tileMap;
                GraphicsFile.TileMapLength = (uint)(tileMap.Length * 2);
                GraphicsFile.TileMapOffset = GraphicsFile.TileSetOffset + GraphicsFile.TileSetLength;

                // Update the palette
                if (img.Palette != null && includePalette)
                {
                    for (int i = 0; i < img.Palette.Colors.Count; i++)
                    {
                        var c = img.Palette.Colors[i];
                        GraphicsFile.Palette[i] = new RGBA5551Color(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
                    }
                }

                // Relocate the data
                RelocateFile();

                // Reload
                Load();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred when importing. Error: {ex.Message}", "Error importing", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}