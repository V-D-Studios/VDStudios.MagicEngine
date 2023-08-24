using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDL2.NET;

namespace VDStudios.MagicEngine.SDL.Demo;

public static class Helper
{
    public static Rectangle[] GetRectangleArray(ReadOnlySpan<(int X, int Y, int Width, int Height)> span)
    {
        var ar = new Rectangle[span.Length];
        for (int i = 0; i < ar.Length; i++) 
        {
            var (X, Y, Width, Height) = span[i];
            ar[i] = new(Width, Height, X, Y);
        }
        return ar;
    }
}
