using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BinarySerializer.Klonoa.KH;

namespace KlonoaHeroesPatcher
{
    public class VariableWidthFontPatchViewModel : BaseViewModel
    {
        public VariableWidthFontPatchViewModel(VariableWidthFontPatch patch, Graphics_File font)
        {
            Patch = patch;
            Font = font;

            RefreshCommand = new RelayCommand(RefreshItems);

            RefreshItems();
        }

        public ICommand RefreshCommand { get; }

        public VariableWidthFontPatch Patch { get; }
        public Graphics_File Font { get; }

        public ObservableCollection<ItemViewModel> Items { get; set; }

        public void RefreshItems()
        {
            var count = (Font.TileMapWidth / TileGraphicsHelpers.TileWidth) * (Font.TileMapHeight / (TileGraphicsHelpers.TileHeight * 2));

            Patch.Widths ??= new byte[count];

            // Resize
            if (Patch.Widths.Length != count)
            {
                var widths = Patch.Widths;
                Array.Resize(ref widths, count);
                Patch.Widths = widths;
            }

            // Default to a width of 8
            for (int i = 0; i < Patch.Widths.Length; i++)
            {
                if (Patch.Widths[i] == 0)
                    Patch.Widths[i] = 8;
            }

            var img = TileGraphicsHelpers.CreateImageSource(
                tileSet: Font.TileSet,
                bpp: Font.BPP,
                palette: Font.Palette,
                tileMap: Font.TileMap,
                width: Font.TileMapWidth,
                height: Font.TileMapHeight,
                basePalette: 3);

            int fontWidth = Font.TileMapWidth / TileGraphicsHelpers.TileWidth;
            
            Items = new ObservableCollection<ItemViewModel>(Enumerable.Range(0, count).Select(i =>
            {
                int fontX = i % fontWidth;
                int fontY = i / fontWidth;

                var rect = new Int32Rect(fontX * TileGraphicsHelpers.TileWidth, fontY * TileGraphicsHelpers.TileHeight * 2, TileGraphicsHelpers.TileWidth, TileGraphicsHelpers.TileHeight * 2);

                return new ItemViewModel(Patch, img, rect, i);
            }));
        }

        public class ItemViewModel : BaseViewModel
        {
            public ItemViewModel(VariableWidthFontPatch patch, BitmapSource imageSource, Int32Rect imageRect, int index)
            {
                Patch = patch;
                ImageSource = new CroppedBitmap(imageSource, imageRect);
                Index = index;
            }

            public VariableWidthFontPatch Patch { get; }
            public ImageSource ImageSource { get; }
            public int Index { get; }

            public byte Width
            {
                get => Patch.Widths?.ElementAtOrDefault(Index) ?? 8;
                set
                {
                    if (Patch.Widths != null && Index < Patch.Widths.Length)
                        Patch.Widths[Index] = value;
                }
            }
        }
    }
}