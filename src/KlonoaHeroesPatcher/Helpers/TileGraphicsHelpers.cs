﻿using BinarySerializer;
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

        public static BitmapSource CreateImageSource(byte[] tileSet, int bpp, IList<BaseColor> palette, GraphicsTile[] tileMap, int width, int height, bool trimPalette)
        {
            // Get the format
            bool hasPalette = palette?.Any() == true;
            PixelFormat format = GetPixelFormat(bpp, !hasPalette);
            float bppFactor = bpp / 8f;

            // Get the dimensions
            int tilesWidth = width / TileWidth;
            int tilesHeight = height / TileHeight;
            int stride = GetStride(width, format);

            // Get the palette
            BitmapPalette bmpPal = hasPalette ? new BitmapPalette(ColorHelpers.ConvertColors(palette, bpp, trimPalette)) : null;

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

            return BitmapSource.Create(width, height, DpiX, DpiY, format, bmpPal, previewPixelData, stride);
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

        public static (byte[] tileSet, GraphicsTile[] tileMap) CreateTileData(byte[] srcImgData, PixelFormat srcFormat, int dstBpp, BaseColor[] dstPalette, int width, int height, bool createMap)
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

            var colorsCount = (int)Math.Pow(2, dstBpp);

            // Trim and remove the transparent color from the palette
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

                    // TODO: If we're creating a map we want to check if the tile matches one which has already been added to the tileset, in which case we use that. Perhaps start by writing to the set, then compare bytes and then go back and overwrite next turn?

                    int tileSetOffset = tileSetIndex * tileLength;

                    int tileMapIndex = tileY * tilesWidth + tileX;

                    if (createMap)
                        tileMap[tileMapIndex] = new GraphicsTile
                        {
                            TileSetIndex = tileSetIndex,
                        };

                    tileSetIndex++;

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

                            // TODO: Support multiple formats, such as 4-bit, 8-bit and 24-bit
                            if (srcFormat == PixelFormats.Bgra32)
                            {
                                b = srcImgData[imgDataPixelOffset + 0];
                                g = srcImgData[imgDataPixelOffset + 1];
                                r = srcImgData[imgDataPixelOffset + 2];
                                a = srcImgData[imgDataPixelOffset + 3];
                            }
                            else
                            {
                                throw new Exception($"Source format {srcFormat} is not supported. Has to be BGRA32.");
                            }

                            // Find the matching color from the palette to use. If fully transparen then use color 0.
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
                }
            }

            int tileSetLength = tileSetIndex * tileLength;

            if (tileSetLength != tileSet.Length)
                Array.Resize(ref tileSet, tileSetLength);

            return (tileSet, tileMap);
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
    }
}