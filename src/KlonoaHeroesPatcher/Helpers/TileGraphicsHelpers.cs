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

        public const double DpiX = 96;
        public const double DpiY = 96;

        public static BitmapSource CreateImageSource(byte[] tileSet, int bpp, IList<BaseColor> palette, GraphicsTile[] tileMap, int width, int height, int basePalette)
        {
            // Get the format
            PixelFormat format = PixelFormats.Indexed8; // Always do 8-bit since 4-bit images can use multiple palettes
            float bppFactor = bpp / 8f;

            // Get the dimensions
            int tilesWidth = width / TileWidth;
            int tilesHeight = height / TileHeight;
            int stride = GetStride(width, format);

            if (palette?.Any() != true)
                palette = ColorHelpers.CreateDummyPalette(256, true, wrap: ColorHelpers.GetPaletteLength(bpp));

            // Get the palette
            BitmapPalette bmpPal = new BitmapPalette(ColorHelpers.ConvertColors(palette, bpp, false));

            // Set tile map to null if not available
            if (tileMap?.Any() != true)
                tileMap = null;

            // Create a buffer for the image data
            var imgData = new byte[width * height];

            // Get the length of each tile in bytes
            int tileLength = (int)(TileWidth * TileHeight * bppFactor);

            // Enumerate every tile
            for (int tileY = 0; tileY < tilesHeight; tileY++)
            {
                int absTileY = tileY * TileHeight;

                for (int tileX = 0; tileX < tilesWidth; tileX++)
                {
                    int absTileX = tileX * TileWidth;

                    int mapIndex = tileY * tilesWidth + tileX;

                    if (tileMap != null && tileMap[mapIndex] == null)
                        continue;

                    GraphicsTile mapTile = tileMap?[mapIndex];
                    int tileIndex = mapTile?.TileSetIndex ?? mapIndex;
                    int tileSetOffset = tileIndex * tileLength;

                    for (int y = 0; y < TileHeight; y++)
                    {
                        for (int x = 0; x < TileWidth; x++)
                        {
                            byte b = tileSet[(int)(tileSetOffset + (y * TileWidth + x) * bppFactor)];

                            if (bpp == 4)
                                b = (byte)BitHelpers.ExtractBits(b, 4, x % 2 == 0 ? 0 : 4);

                            var sourceTileX = mapTile?.FlipX != true ? x : TileWidth - x - 1;
                            var sourceTileY = mapTile?.FlipY != true ? y : TileHeight - y - 1;

                            if (bpp == 4)
                            {
                                var paletteIndex = mapTile?.PaletteIndex ?? 0;
                                b = (byte)(b + (basePalette + paletteIndex) * 16);
                            }

                            imgData[(absTileY + sourceTileY) * width + absTileX + sourceTileX] = b;
                        }
                    }
                }
            }

            return BitmapSource.Create(width, height, DpiX, DpiY, format, bmpPal, imgData, stride);
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

            return BitmapSource.Create(width, height, DpiX, DpiY, format, bmpPal, bytes, GetStride(width, format));
        }

        public static (byte[] tileSet, GraphicsTile[] tileMap) CreateTileData(byte[] srcImgData, PixelFormat srcFormat, BitmapPalette srcPalette, int dstBpp, BaseColor[] dstPalette, int width, int height, bool createMap, int basePalette)
        {
            // Get the format
            float tileSetBppFactor = dstBpp / 8f;
            float imgDataBppFactor = srcFormat.BitsPerPixel / 8f;

            // Get the dimensions
            int tilesWidth = width / TileWidth;
            int tilesHeight = height / TileHeight;

            // Get the length of each tile in bytes
            int tileLength = (int)(TileWidth * TileHeight * tileSetBppFactor);

            var tileSet = new byte[tilesWidth * tilesHeight * tileLength]; // Max size, might be smaller if we reuse tiles in which case we shrink it later
            var tileMap = new GraphicsTile[createMap ? tilesWidth * tilesHeight : 0];

            int tileSetIndex = 0;

            var colorsCount = ColorHelpers.GetPaletteLength(dstBpp);

            // If there is no palette we use a gray-scale one
            if (dstPalette?.Any() != true)
                dstPalette = ColorHelpers.CreateDummyPalette(colorsCount);

            // Trim and remove the transparent color from the palette. We don't want to check against that when matching colors.
            if (dstPalette.Length > colorsCount)
                dstPalette = dstPalette.Take(colorsCount).Skip(1).ToArray();
            else
                dstPalette = dstPalette.Skip(1).ToArray();

            // Enumerate every tile
            for (int tileY = 0; tileY < tilesHeight; tileY++)
            {
                var absTileY = tileY * TileHeight;

                for (int tileX = 0; tileX < tilesWidth; tileX++)
                {
                    var absTileX = tileX * TileWidth;

                    int tileSetOffset = tileSetIndex * tileLength;

                    int tileMapIndex = tileY * tilesWidth + tileX;

                    if (createMap)
                        tileMap[tileMapIndex] = new GraphicsTile
                        {
                            TileSetIndex = tileSetIndex,
                            PaletteIndex = basePalette, // TODO: Support tiles having different palette indices
                        };

                    for (int y = 0; y < TileHeight; y++)
                    {
                        for (int x = 0; x < TileWidth; x++)
                        {
                            var imgDataPixelOffset = (int)(((absTileY + y) * width + (absTileX + x)) * imgDataBppFactor);
                            var tileSetPixelOffset = (int)(tileSetOffset + (y * TileWidth + x) * tileSetBppFactor);

                            byte r;
                            byte g;
                            byte b;
                            byte a;

                            if (srcFormat == PixelFormats.Bgra32)
                            {
                                b = srcImgData[imgDataPixelOffset + 0];
                                g = srcImgData[imgDataPixelOffset + 1];
                                r = srcImgData[imgDataPixelOffset + 2];
                                a = srcImgData[imgDataPixelOffset + 3];
                            }
                            else if (srcFormat == PixelFormats.Bgr24 || srcFormat == PixelFormats.Bgr32)
                            {
                                b = srcImgData[imgDataPixelOffset + 0];
                                g = srcImgData[imgDataPixelOffset + 1];
                                r = srcImgData[imgDataPixelOffset + 2];
                                a = 255;
                            }
                            else if (srcFormat == PixelFormats.Indexed8)
                            {
                                var index = srcImgData[imgDataPixelOffset];
                                b = srcPalette.Colors[index].B;
                                g = srcPalette.Colors[index].G;
                                r = srcPalette.Colors[index].R;
                                a = srcPalette.Colors[index].A;
                            }
                            else if (srcFormat == PixelFormats.Indexed4)
                            {
                                var index = BitHelpers.ExtractBits(srcImgData[imgDataPixelOffset], 4, x % 2 == 0 ? 4 : 0);
                                b = srcPalette.Colors[index].B;
                                g = srcPalette.Colors[index].G;
                                r = srcPalette.Colors[index].R;
                                a = srcPalette.Colors[index].A;
                            }
                            else
                            {
                                throw new Exception($"Source format {srcFormat} is not supported");
                            }

                            // Find the matching color from the palette to use. If fully transparent then use color 0.
                            int paletteIndex = a == 0 ? 0 : ColorHelpers.FindNearestColor(dstPalette, new BGR888Color()
                            {
                                R = r,
                                G = g,
                                B = b,
                            }) + 1;

                            // Set the byte in the tile set
                            if (dstBpp == 8)
                                tileSet[tileSetPixelOffset] = (byte)paletteIndex;
                            else if (dstBpp == 4)
                                tileSet[tileSetPixelOffset] = (byte)BitHelpers.SetBits(tileSet[tileSetPixelOffset], paletteIndex, dstBpp, x % 2 == 0 ? 0 : 4);
                            else
                                throw new Exception($"Destination BPP {dstBpp} is not supported. Has to be 4 or 8.");
                        }
                    }

                    // If we're creating a map we want to check if this tile matches a previous one, and if so use that instead
                    if (createMap)
                    {
                        for (int i = 0; i < tileSetIndex; i++)
                        {
                            bool matchesNormal = true;
                            bool matchesFlipX = true;
                            bool matchesFlipY = true;
                            bool matchesFlipXY = true;

                            for (int y = 0; y < TileHeight; y++)
                            {
                                for (int x = 0; x < TileWidth; x++)
                                {
                                    byte p = getPixel(tileSetIndex, x, y);

                                    if (matchesNormal)
                                        matchesNormal = p == getPixel(i, x, y);

                                    if (matchesFlipX)
                                        matchesFlipX = p == getPixel(i, TileWidth - x - 1, y);

                                    if (matchesFlipY)
                                        matchesFlipY = p == getPixel(i, x, TileHeight - y - 1);

                                    if (matchesFlipXY)
                                        matchesFlipXY = p == getPixel(i, TileWidth - x - 1, TileHeight - y - 1);

                                    byte getPixel(int baseTileIndex, int xOffset, int yOffset)
                                    {
                                        var pixel = tileSet[(int)(baseTileIndex * tileLength + (yOffset * TileWidth + xOffset) * tileSetBppFactor)];

                                        if (dstBpp == 4)
                                            pixel = (byte)BitHelpers.ExtractBits(pixel, 4, x % 2 == 0 ? 0 : 4);

                                        return pixel;
                                    }
                                }
                            }

                            if (matchesNormal || matchesFlipX || matchesFlipY || matchesFlipXY)
                            {
                                tileMap[tileMapIndex].TileSetIndex = i;

                                if (!matchesNormal)
                                {
                                    tileMap[tileMapIndex].FlipX = matchesFlipX || matchesFlipXY;
                                    tileMap[tileMapIndex].FlipY = matchesFlipY || matchesFlipXY;
                                }

                                tileSetIndex--;
                                break;
                            }
                        }
                    }

                    tileSetIndex++;
                }
            }

            int tileSetLength = tileSetIndex * tileLength;

            if (tileSetLength != tileSet.Length)
                Array.Resize(ref tileSet, tileSetLength);

            return (tileSet, tileMap);
        }

        public static int GetStride(int width, PixelFormat format)
        {
            int stride = (int)(width / (8f / format.BitsPerPixel));

            if (stride % 4 != 0)
                stride += 4 - stride % 4;

            return stride;
        }
    }
}