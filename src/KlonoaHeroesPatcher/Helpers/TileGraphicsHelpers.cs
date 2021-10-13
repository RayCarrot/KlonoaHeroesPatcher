using BinarySerializer;
using BinarySerializer.GBA;
using BinarySerializer.Klonoa.KH;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KlonoaHeroesPatcher
{
    public static class TileGraphicsHelpers
    {
        public const int TileWidth = GBAConstants.TileSize;
        public const int TileHeight = GBAConstants.TileSize;

        public static BitmapSource CreateImageSource(byte[] tileSet, int bpp, IList<BaseColor> palette, GraphicsTile[] tileMap, int width, int height, bool trimPalette)
        {
            // Get the format
            bool hasPalette = palette?.Any() == true;
            PixelFormat format = GetPixelFormat(bpp, !hasPalette);
            float bppFactor = 1 / (8f / bpp);

            // Get the dimensions
            int tilesWidth = width / TileWidth;
            int tilesHeight = height / TileHeight;
            int stride = GetStride(width, format);

            // Get the palette
            BitmapPalette bmpPal = hasPalette ? new BitmapPalette(ConvertColors(palette, bpp, trimPalette)) : null;

            // Set tile map to null if not available
            if (tileMap?.Any() != true)
                tileMap = null;

            // Create a buffer for the preview pixel data
            var previewPixelData = new byte[(int)(width * height * bppFactor)];

            // Get the length of each tile in bytes
            int tileLength = (int)(TileWidth * TileHeight * bppFactor);

            // Enumerate every tile
            for (int tileY = 0; tileY < tilesHeight; tileY++)
            {
                var absTileY = tileY * TileHeight;

                for (int tileX = 0; tileX < tilesWidth; tileX++)
                {
                    var absTileX = tileX * TileWidth * bppFactor;

                    int mapIndex = tileY * tilesWidth + tileX;
                 
                    if (tileMap != null && tileMap[mapIndex] == null)
                        continue;

                    GraphicsTile mapTile = tileMap?[mapIndex];
                    int tileIndex = mapTile?.TileSetIndex ?? mapIndex;
                    int tileSetOffset = tileIndex * tileLength;

                    for (int y = 0; y < TileHeight; y++)
                    {
                        for (int x = 0; x < TileWidth * bppFactor; x++)
                        {
                            var b = tileSet[(int)(tileSetOffset + y * TileWidth * bppFactor + x)];

                            // Reverse the bits if 4bpp
                            if (bpp == 4)
                            {
                                var b1 = BitHelpers.SetBits(b, BitHelpers.ExtractBits(b, 4, 0), 4, 4);
                                b = (byte)BitHelpers.SetBits(b1, BitHelpers.ExtractBits(b, 4, 4), 4, 0);
                            }

                            var sourceTileX = mapTile?.FlipX != true ? x : TileWidth - x - 1;
                            var sourceTileY = mapTile?.FlipY != true ? y : TileHeight - y - 1;

                            previewPixelData[(int)((absTileY + sourceTileY) * width * bppFactor + (absTileX + sourceTileX))] = b;
                        }
                    }
                }
            }

            return BitmapSource.Create(width, height, 96, 96, format, bmpPal, previewPixelData, stride);
        }

        public static BitmapSource CreatePaletteImageSource(BitmapPalette bmpPal, int scale = 16, int offset = 0, int? optionalLength = null, int? optionalWrap = null, bool reverseY = true)
        {
            PixelFormat format = PixelFormats.Indexed8;
            int length = optionalLength ?? bmpPal.Colors.Count;
            int wrap = optionalWrap ?? length;
            int width = Math.Min(length, wrap) * scale;
            int height = (int)Math.Ceiling(length / (float)wrap) * scale;
            var bytes = new byte[width * height];

            for (int i = 0; i < length; i++)
            {
                int mainY = (height / scale) - 1 - (i / wrap);
                int mainX = i % wrap;

                for (int y = 0; y < scale; y++)
                {
                    for (int x = 0; x < scale; x++)
                    {
                        var xx = mainX * scale + x;
                        var yy = mainY * scale + y;

                        if (reverseY)
                            yy = height - yy - 1;

                        bytes[yy * width + xx] = (byte)(offset + i);
                    }
                }
            }

            return BitmapSource.Create(width, height, 96, 96, format, bmpPal, bytes, GetStride(width, format));
        }

        public static (byte[] tileSet, GraphicsTile[] tileMap) CreateTileData(byte[] imgData, int bpp, int width, int height, bool createMap)
        {
            if (createMap)
                throw new NotImplementedException("Importing to graphics with a tile map is currently not supported");

            // Get the format
            float bppFactor = 1 / (8f / bpp);

            // Get the dimensions
            int tilesWidth = width / TileWidth;
            int tilesHeight = height / TileHeight;

            // Get the length of each tile in bytes
            int tileLength = (int)(TileWidth * TileHeight * bppFactor);

            var tileSet = new byte[imgData.Length];

            // Enumerate every tile
            for (int tileY = 0; tileY < tilesHeight; tileY++)
            {
                var absTileY = tileY * TileHeight;

                for (int tileX = 0; tileX < tilesWidth; tileX++)
                {
                    var absTileX = tileX * TileWidth * bppFactor;

                    int tileIndex = tileY * tilesWidth + tileX;
                    int tileSetOffset = tileIndex * tileLength;

                    for (int y = 0; y < TileHeight; y++)
                    {
                        for (int x = 0; x < TileWidth * bppFactor; x++)
                        {
                            var b = imgData[(int)((absTileY + y) * width * bppFactor + (absTileX + x))];

                            // Reverse the bits if 4bpp
                            if (bpp == 4)
                            {
                                var b1 = BitHelpers.SetBits(b, BitHelpers.ExtractBits(b, 4, 0), 4, 4);
                                b = (byte)BitHelpers.SetBits(b1, BitHelpers.ExtractBits(b, 4, 4), 4, 0);
                            }

                            tileSet[(int)(tileSetOffset + y * TileWidth * bppFactor + x)] = b;
                        }
                    }
                }
            }

            return (tileSet, new GraphicsTile[0]);
        }

        public static PixelFormat GetPixelFormat(int bpp, bool isGrayScale = false)
        {
            if (bpp != 4 && bpp != 8)
                throw new Exception($"Unsupported bpp {bpp}");

            if (!isGrayScale)
                return bpp == 4 ? PixelFormats.Indexed4 : PixelFormats.Indexed8;
            else
                return bpp == 4 ? PixelFormats.Gray4 : PixelFormats.Gray8;
        }

        public static int GetStride(int width, PixelFormat format)
        {
            int stride = (int)(width / (8f / format.BitsPerPixel));

            if (stride % 4 != 0)
                stride += 4 - stride % 4;

            return stride;
        }

        public static IList<Color> ConvertColors(IEnumerable<BaseColor> colors, int bpp, bool trimPalette)
        {
            int wrap = (int)Math.Pow(2, bpp);

            var c = colors.Select((x, i) => Color.FromArgb(
                a: (byte)(i % wrap == 0 ? 0 : 255),
                r: (byte)(x.Red * 255),
                g: (byte)(x.Green * 255),
                b: (byte)(x.Blue * 255))).ToArray();

            if (trimPalette && c.Length >= wrap)
                c = c.Take(wrap).ToArray();

            return c;
        }
    }
}