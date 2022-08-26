using SDL2.NET;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace VDStudios.MagicEngine.Geometry;

/// <summary>
/// Contains an assortment of helper and extension methods in relation to geometry
/// </summary>
public static class GeometryMath
{
    /// <summary>
    /// Calculates the angle in radians between a line that passes through <paramref name="a"/> and <paramref name="b"/> and a line that passes through <paramref name="a"/> and <paramref name="c"/>
    /// </summary>
    /// <param name="a">The point at which the line <paramref name="b"/><paramref name="a"/> and <paramref name="c"/><paramref name="a"/> intersect</param>
    /// <param name="b">The second point <paramref name="b"/><paramref name="a"/> passes through</param>
    /// <param name="c">The second point <paramref name="c"/><paramref name="a"/> passes through</param>
    /// <returns>The angle in radians between <paramref name="b"/><paramref name="a"/> and <paramref name="c"/><paramref name="a"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Angle(Vector2 b, Vector2 a, Vector2 c)
    {
        var ba = b - a;
        var ca = c - a;
        return MathF.Abs(MathF.Atan2(Cross(ba, ca), Vector2.Dot(ba, ca)));
    }

    /// <summary>
    /// Calculates the cross product between <see cref="Vector2"/>s <paramref name="a"/> and <paramref name="b"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Cross(this Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;

    /// <summary>
    /// Creates a <see cref="Matrix4x4"/> that translates and scales the view of a texture to only display a portion of it
    /// </summary>
    /// <param name="area">The area to view in the texture</param>
    /// <param name="texture">The texture to create the view for</param>
    /// <returns>The transformation matrix for the texture coordinates</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 Create2DView(this Texture texture, FRectangle area)
        => Matrix4x4.CreateScale(area.Width / texture.Width, area.Height / texture.Height, 1f) * 
            Matrix4x4.CreateTranslation(area.X / texture.Width, area.Y / texture.Height, 1f);

    /// <summary>
    /// Creates a set of transformation matrices to fill <paramref name="transformationViews"/> with out of an evenly spaced grid graphed along the <see cref="Texture"/>'s dimensions
    /// </summary>
    /// <remarks>
    /// The views are arranged from the top-left corner, going right, and back to the left-most column in the next row. For example, on a 4x4 grid, where 0,0 (col, row) is the top-left slot, the views would be ordered as: 0,0 -> 1,0 -> 2,0 -> 3,0 -> 0,1 -> 1,1 -> 2,1 -> 3,1 -> 0,2 -> 1,2 -> 2,2 -> 3,2 -> 0,3 -> 1,3 -> 2,3 -> 3,3
    /// </remarks>
    /// <param name="texture">The texture to create the views for</param>
    /// <param name="rows">The amount of rows to divide the height of <paramref name="texture"/> in</param>
    /// <param name="columns">The amount of columns to divide the width of <paramref name="texture"/> in</param>
    /// <param name="transformationViews">The buffer in which to store the views</param>
    public static void DivideInto2DViews(this Texture texture, Span<Matrix4x4> transformationViews, int rows, int columns)
        => Div2DViews(transformationViews, rows, columns, texture.Width / columns, texture.Height / rows);

    /// <summary>
    /// Creates a set of transformation matrices to fill <paramref name="transformationViews"/> with out of an evenly spaced grid graphed along the <see cref="Texture"/>'s dimensions
    /// </summary>
    /// <remarks>
    /// The views are arranged from the top-left corner, going right, and back to the left-most column in the next row. For example, on a 4x4 grid, where 0,0 (col, row) is the top-left slot, the views would be ordered as: 0,0 -> 1,0 -> 2,0 -> 3,0 -> 0,1 -> 1,1 -> 2,1 -> 3,1 -> 0,2 -> 1,2 -> 2,2 -> 3,2 -> 0,3 -> 1,3 -> 2,3 -> 3,3
    /// </remarks>
    /// <param name="texture">The texture to create the views for</param>
    /// <param name="rows">The amount of rows to divide the height of <paramref name="texture"/> in</param>
    /// <param name="columns">The amount of columns to divide the width of <paramref name="texture"/> in</param>
    /// <param name="transformationViews">The buffer in which to store the views</param>
    /// <param name="area">The area of the texture within which to graph the grid</param>
    public static void DivideInto2DViews(this Texture texture, Span<Matrix4x4> transformationViews, FRectangle area, int rows, int columns)
        => Div2DViews(transformationViews, rows, columns, texture.Width / (area.Width / columns), texture.Height / (area.Height / rows));

    private static void Div2DViews(Span<Matrix4x4> transformationViews, int rows, int cols, float xoff, float yoff)
    {
        var len = rows * cols;
        if (len > transformationViews.Length)
            throw new ArgumentException($"The transformationViews buffer doesn't have enough space to fit all the views: It has a length of {transformationViews.Length}, while a length of {len} is necessary", nameof(transformationViews));

        int i = 0, x = 0, y = 0;
        var scale = Matrix4x4.CreateScale(xoff, yoff, 1);
        for (; x < cols; x++)
            for (; y < rows; y++)
                transformationViews[i] = scale * Matrix4x4.CreateTranslation(xoff * x, yoff * y, 0);
    }
}
