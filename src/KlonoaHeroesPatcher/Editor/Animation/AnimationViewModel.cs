using BinarySerializer;
using BinarySerializer.GBA;
using BinarySerializer.Klonoa.KH;
using ImageMagick;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace KlonoaHeroesPatcher
{
    public class AnimationViewModel : BaseViewModel
    {
        public AnimationViewModel(Animation_File animationFile, int group, int anim)
        {
            AnimationFile = animationFile;
            AnimGroup = group;
            AnimIndex = anim;
            Animation = animationFile.AnimationGroups[group].Animations[anim];

            ExportCommand = new RelayCommand(Export);
            ExportFramesCommand = new RelayCommand(ExportFrames);
        }

        public ICommand ExportCommand { get; }
        public ICommand ExportFramesCommand { get; }

        public Animation_File AnimationFile { get; }
        public int AnimGroup { get; }
        public int AnimIndex { get; }
        public Animation Animation { get; }

        public string DisplayName => $"Animation {AnimGroup}-{AnimIndex}";

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;

                if (GIFStream == null)
                    RefreshGIF();
            }
        }

        public Stream GIFStream { get; set; }
        public double Width { get; set; }

        public MagickImageCollection GetImageCollection()
        {
            var collection = new MagickImageCollection();

            int minX = Animation.Frames.SelectMany(x => x.Sprites).Min(x => x.XPos);
            int minY = Animation.Frames.SelectMany(x => x.Sprites).Min(x => x.YPos);
            int maxX = Animation.Frames.SelectMany(x => x.Sprites).Max(x => x.XPos + GBAConstants.GetSpriteShape(x.ObjAttr.SpriteShape, x.ObjAttr.SpriteSize).Width);
            int maxY = Animation.Frames.SelectMany(x => x.Sprites).Max(x => x.YPos + GBAConstants.GetSpriteShape(x.ObjAttr.SpriteShape, x.ObjAttr.SpriteSize).Height);

            int width = maxX - minX;
            int height = maxY - minY;

            foreach (AnimationFrame frame in Animation.Frames)
            {
                var imgData = new byte[width * height * 4];

                foreach (AnimationSprite sprite in frame.Sprites)
                {
                    int bpp = sprite.ObjAttr.Is8Bit ? 8 : 4;
                    float bppFactor = bpp / 8f;
                    int tileLength = (int)(TileGraphicsHelpers.TileWidth * TileGraphicsHelpers.TileHeight * bppFactor);
                    GBAConstants.Size shape = GBAConstants.GetSpriteShape(sprite.ObjAttr.SpriteShape, sprite.ObjAttr.SpriteSize);
                    int offset = (int)sprite.TileSetOffset;

                    for (int y = 0; y < shape.Height; y += TileGraphicsHelpers.TileHeight)
                    {
                        for (int x = 0; x < shape.Width; x += TileGraphicsHelpers.TileWidth)
                        {
                            TileGraphicsHelpers.DrawTileToRGBAImg(
                                tileSet: AnimationFile.TileSet,
                                tileSetOffset: offset,
                                tileSetBpp: bpp,
                                flipX: sprite.ObjAttr.HorizontalFlip,
                                flipY: sprite.ObjAttr.VerticalFlip,
                                imgData: imgData,
                                xPos: sprite.XPos + (sprite.ObjAttr.HorizontalFlip ? shape.Width - x - TileGraphicsHelpers.TileWidth : x) - minX,
                                yPos: sprite.YPos + (sprite.ObjAttr.VerticalFlip ? shape.Height - y - TileGraphicsHelpers.TileHeight : y) - minY,
                                imgWidth: width,
                                palette: AnimationFile.Palette,
                                basePalette: sprite.PaletteIndex);

                            offset += tileLength;
                        }
                    }
                }

                MagickImage img = new MagickImage(imgData, new MagickReadSettings()
                {
                    Format = MagickFormat.Rgba,
                    Width = width,
                    Height = height,
                });

                collection.Add(img);

                img.AnimationDelay = frame.Speed;
                img.AnimationTicksPerSecond = 60;

                img.GifDisposeMethod = GifDisposeMethod.Background;
            }

            Width = width;
            return collection;
        }

        public void RefreshGIF()
        {
            GIFStream?.Dispose();
            using var collection = GetImageCollection();
            var gifStream = new MemoryStream();
            collection.Write(gifStream, MagickFormat.Gif);
            gifStream.Position = 0;
            GIFStream = gifStream;
        }

        public void Export()
        {
            Pointer offset = BinaryHelpers.GetROMPointer(AnimationFile.Offset);

            var dialog = new SaveFileDialog()
            {
                Title = "Export animation",
                Filter = "GIF Files (*.gif)|*.gif",
                FileName = $"{offset.StringAbsoluteOffset}.gif"
            };

            var result = dialog.ShowDialog();

            if (result != true)
                return;

            try
            {
                using var collection = GetImageCollection();
                collection.Write(dialog.FileName, MagickFormat.Gif);

                MessageBox.Show($"The animation was successfully exported");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred when exporting. Error: {ex.Message}", "Error exporting", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ExportFrames()
        {
            using var dialog = new CommonOpenFileDialog
            {
                Title = "Export animation frames",
                AllowNonFileSystemItems = false,
                IsFolderPicker = true,
                EnsureFileExists = true,
                EnsurePathExists = true
            };

            var result = dialog.ShowDialog();

            if (result != CommonFileDialogResult.Ok)
                return;

            try
            {
                using var collection = GetImageCollection();
                for (int i = 0; i < collection.Count; i++)
                {
                    var img = collection[i];
                    img.Write(Path.Combine(dialog.FileName, $"{i}.png"), MagickFormat.Png);
                }

                MessageBox.Show($"The animation frames were successfully exported");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred when exporting. Error: {ex.Message}", "Error exporting", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}