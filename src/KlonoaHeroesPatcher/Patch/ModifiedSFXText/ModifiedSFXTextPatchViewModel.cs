using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BinarySerializer.Klonoa.KH;

namespace KlonoaHeroesPatcher;

public class ModifiedSFXTextPatchViewModel : BaseViewModel
{
    public ModifiedSFXTextPatchViewModel(ModifiedSFXTextPatch patch, Animation_File animationFile)
    {
        Patch = patch;
        AnimationFile = animationFile;

        RefreshCommand = new RelayCommand(RefreshItems);

        RefreshItems();
    }

    public ICommand RefreshCommand { get; }

    public ModifiedSFXTextPatch Patch { get; }
    public Animation_File AnimationFile { get; }
    public ObservableCollection<SFXEntryViewModel> SFXEntries { get; set; }

    public void RefreshItems()
    {
        SFXEntries = new ObservableCollection<SFXEntryViewModel>(Enumerable.Range(0, 6).Select(x => new SFXEntryViewModel(AnimationFile, Patch.Entries[x].AnimGroupIndices, Patch.Entries[x].AnimIndex)));
    }

    public class SFXEntryViewModel : BaseViewModel
    {
        public SFXEntryViewModel(Animation_File animationFile, int[] animGroupIndexes, int animIndex)
        {
            AnimationFile = animationFile;
            AnimGroupIndexes = animGroupIndexes;
            AnimIndex = animIndex;

            Text = String.Join(' ', AnimGroupIndexes);

            UnsavedChanges = false;

            ApplyCommand = new RelayCommand(Apply);

            RefreshPreviewImage();
        }

        public ICommand ApplyCommand { get; }

        public Animation_File AnimationFile { get; }

        private bool _isRed;
        public bool IsRed
        {
            get => _isRed;
            set
            {
                _isRed = value;
                UnsavedChanges = true;
            }
        }

        public int AnimIndex
        {
            get => IsRed ? 0 : 1;
            set => IsRed = value == 0;
        }

        public int[] AnimGroupIndexes { get; set; }

        private string _text;
        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                UnsavedChanges = true;
            }
        }

        public bool UnsavedChanges { get; set; }

        public ImageSource TextPreviewImgSource { get; set; }
        public int TextPreviewWidth { get; set; }

        public void Apply()
        {
            const string errorHeader = "Error parsing text";

            if (String.IsNullOrWhiteSpace(Text))
            {
                MessageBox.Show("The text can not be empty", errorHeader, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string[] values = Text.Split(' ');

            List<int> indexes = new();

            foreach (string value in values)
            {
                int index = Int32.TryParse(value, out int i) ? i : -1;

                if (index == -1)
                {
                    MessageBox.Show($"{value} is not a valid index", errorHeader, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (index < 0 || index >= AnimationFile.AnimationGroups.Length)
                {
                    MessageBox.Show($"{value} is not a valid index. Must be a number between 0 and {AnimationFile.AnimationGroups.Length - 1}", errorHeader, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                indexes.Add(index);
            }

            AnimGroupIndexes = indexes.Take(8).ToArray();
            Text = String.Join(' ', AnimGroupIndexes);

            UnsavedChanges = false;

            RefreshPreviewImage();
        }

        public void RefreshPreviewImage()
        {
            try
            {
                // We assume each character is 16x16 (2x2 tiles)
                const int charTilesWidth = 2;
                const int charTilesHeight = 2;

                int width = AnimGroupIndexes.Length * TileGraphicsHelpers.TileWidth * charTilesWidth;
                const int height = TileGraphicsHelpers.TileHeight * charTilesHeight;

                // Get the format
                const int bpp = 4;
                const float bppFactor = bpp / 8f;
                PixelFormat format = PixelFormats.Indexed8;
                int stride = TileGraphicsHelpers.GetStride(width, format);
                const int tileLength = (int)(TileGraphicsHelpers.TileWidth * TileGraphicsHelpers.TileHeight * bppFactor);

                // Get the palette
                BitmapPalette bmpPal = new BitmapPalette(ColorHelpers.ConvertColors(AnimationFile.Palette, bpp, false));

                byte[] imgData = new byte[width * height];

                for (int i = 0; i < AnimGroupIndexes.Length; i++)
                {
                    AnimationSprite sprite = AnimationFile.AnimationGroups[AnimGroupIndexes[i]].Animations[AnimIndex].Frames[0].Sprites[0];

                    int tileOffset = (int)sprite.TileSetOffset;

                    for (int y = 0; y < charTilesHeight; y++)
                    {
                        for (int x = 0; x < charTilesWidth; x++)
                        {
                            TileGraphicsHelpers.DrawTileTo8BPPImg(
                                tileSet: AnimationFile.TileSet,
                                tileSetOffset: tileOffset,
                                tileSetBpp: bpp,
                                paletteOffset: 0,
                                flipX: false,
                                flipY: false,
                                imgData: imgData,
                                xPos: i * TileGraphicsHelpers.TileWidth * charTilesWidth + x * TileGraphicsHelpers.TileWidth,
                                yPos: y * TileGraphicsHelpers.TileHeight,
                                imgWidth: width);

                            tileOffset += tileLength;
                        }
                    }
                }

                TextPreviewImgSource = BitmapSource.Create(width, height, TileGraphicsHelpers.DpiX, TileGraphicsHelpers.DpiY, format, bmpPal, imgData, stride);

                // Display at double the size
                TextPreviewWidth = width * 2;
            }
            catch (Exception ex)
            {
                TextPreviewImgSource = null;
                MessageBox.Show($"An error occurred when refreshing the preview image. Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}