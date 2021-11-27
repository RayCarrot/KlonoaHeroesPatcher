using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using BinarySerializer;

namespace KlonoaHeroesPatcher;

public class ColorHelpers
{
    public static int GetPaletteLength(int bpp) => (int)Math.Pow(2, bpp);

    public static BaseColor[] CreateDummyPalette(int length, bool firstTransparent = true, int? wrap = null)
    {
        BaseColor[] pal = new BaseColor[length];
            
        wrap ??= length;
            
        if (firstTransparent)
            pal[0] = BaseColor.Clear;
            
        for (int i = firstTransparent ? 1 : 0; i < length; i++)
        {
            float val = (float)(i % wrap.Value) / (wrap.Value - 1);
            pal[i] = new CustomColor(val, val, val);
        }
            
        return pal;
    }

    public static IList<Color> ConvertColors(IEnumerable<BaseColor> colors, int bpp, bool trimPalette)
    {
        int wrap = GetPaletteLength(bpp);

        var c = colors.Select((x, i) => Color.FromArgb(
            a: (byte)(i % wrap == 0 ? 0 : 255),
            r: (byte)(x.Red * 255),
            g: (byte)(x.Green * 255),
            b: (byte)(x.Blue * 255))).ToArray();

        if (trimPalette && c.Length >= wrap)
            c = c.Take(wrap).ToArray();

        return c;
    }

    public static float GetDistance(BaseColor current, BaseColor match)
    {
        float redDifference = current.Red - match.Red;
        float greenDifference = current.Green - match.Green;
        float blueDifference = current.Blue - match.Blue;

        return redDifference * redDifference + greenDifference * greenDifference + blueDifference * blueDifference;
    }

    public static int FindNearestColor(BaseColor[] palette, BaseColor color)
    {
        int index = -1;
        float shortestDistance = Single.MaxValue;

        for (int i = 0; i < palette.Length; i++)
        {
            BaseColor match = palette[i];
            float distance = GetDistance(color, match);

            if (distance < shortestDistance)
            {
                index = i;
                shortestDistance = distance;
            }
        }

        return index;
    }
}