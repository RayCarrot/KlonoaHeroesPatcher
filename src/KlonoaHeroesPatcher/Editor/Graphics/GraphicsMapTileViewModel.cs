using System.Windows.Input;
using System.Windows.Media.Imaging;
using BinarySerializer.GBA;

namespace KlonoaHeroesPatcher
{
    public class GraphicsMapTileViewModel : BaseViewModel
    {
        public GraphicsMapTileViewModel(GraphicsFileEditorViewModel editorViewModel, CroppedBitmap previewImgSource, MapTile tile, int tilesCount)
        {
            EditorViewModel = editorViewModel;
            PreviewImgSource = previewImgSource;
            Tile = tile;

            TileSetIndex = Tile.TileIndex;
            MinTileSetIndex = 0;
            MaxTileSetIndex = tilesCount - 1;

            PaletteIndex = Tile.PaletteIndex;
            MinPaletteIndex = 0;
            MaxPaletteIndex = 15;

            FlipX = Tile.FlipX;
            FlipY = Tile.FlipY;

            ApplyCommand = new RelayCommand(Apply);
        }

        public ICommand ApplyCommand { get; }

        public GraphicsFileEditorViewModel EditorViewModel { get; }
        public CroppedBitmap PreviewImgSource { get; }
        public MapTile Tile { get; }

        public int TileSetIndex { get; set; }
        public int MinTileSetIndex { get; }
        public int MaxTileSetIndex { get; }

        public int PaletteIndex { get; set; }
        public int MinPaletteIndex { get; }
        public int MaxPaletteIndex { get; }

        public bool FlipX { get; set; }
        public bool FlipY { get; set; }

        public bool IsHighlighted { get; set; }

        public void Apply()
        {
            // Update the properties
            Tile.TileIndex = TileSetIndex;
            Tile.PaletteIndex = PaletteIndex;
            Tile.FlipX = FlipX;
            Tile.FlipY = FlipY;

            // Refresh
            int selectedIndex = EditorViewModel.SelectedMapTileIndex;
            EditorViewModel.RefreshBasePalette();
            EditorViewModel.RefreshImage();
            EditorViewModel.SelectedMapTileIndex = selectedIndex;

            EditorViewModel.RelocateFile();
        }
    }
}