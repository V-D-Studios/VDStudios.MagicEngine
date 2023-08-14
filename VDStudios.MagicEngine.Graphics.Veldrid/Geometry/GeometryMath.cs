using SDL2.NET;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace VDStudios.MagicEngine.Graphics.Veldrid.Geometry;

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
        return MathF.Abs(MathF.Atan2(ba.Cross(ca), Vector2.Dot(ba, ca)));
    }

    /// <summary>
    /// Calculates the cross product between <see cref="Vector2"/>s <paramref name="a"/> and <paramref name="b"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Cross(this Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;

    /// <summary>
    /// Creates a <see cref="Matrix4x4"/> that translates and scales the view of a texture to only display a portion of it
    /// </summary>
    /// <param name="bottomright">The bottom-right point of the area, denoted in relative coordinates from 0,0 to 1,1</param>
    /// <param name="topleft">The top-left point of the area, denoted in relative coordinates from 0,0 to 1,1</param>
    /// <returns>The transformation matrix for the texture coordinates</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 Create2DView(Vector2 topleft, Vector2 bottomright)
    {
        var diff = bottomright - topleft;
        return Matrix4x4.CreateScale(diff.X, diff.Y, 1f) *
                Matrix4x4.CreateTranslation(topleft.X, topleft.Y, 1f);
    }

    /// <summary>
    /// Creates a <see cref="Matrix4x4"/> that translates and scales the view of a texture to only display a portion of it
    /// </summary>
    /// <param name="area">The area to view in the texture in texels</param>
    /// <param name="texture">The texture to create the view for</param>
    /// <returns>The transformation matrix for the texture coordinates</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 Create2DView(this Texture texture, FRectangle area)
        => Matrix4x4.CreateScale(area.Width / texture.Width, area.Height / texture.Height, 1f) *
            Matrix4x4.CreateTranslation(area.X / texture.Width, area.Y / texture.Height, 1f);

    /// <summary>
    /// Creates a set of transformation matrices to fill <paramref name="transformationViews"/> with out of an evenly spaced grid
    /// </summary>
    /// <remarks>
    /// The views are arranged from the top-left corner, going down, and back to the top-most row in the next column. For example, a 4x4 grid would produce the following, where 0, 0 (column, row) is the top-left corner:
    /// <list type="table">
    /// <item> 0, 0  |  1, 0  |  2, 0  |  3, 0 </item>
    /// <item> 0, 1  |  1, 1  |  2, 1  |  3, 1 </item>
    /// <item> 0, 2  |  1, 2  |  2, 2  |  3, 2 </item>
    /// <item> 0, 3  |  1, 3  |  2, 3  |  3, 3 </item>
    /// </list>
    /// would be ordered in the buffer as: 0, 0 -> 0, 1 -> 0, 2 -> 0, 3 -> 1, 0 -> 1, 1 -> 1, 2 -> 1, 3 -> 1, 0 -> 1, 1 -> 1, 2 -> 1, 3 -> 2, 0 -> 2, 1 -> 2, 2 -> 2, 3 -> 3, 0 -> 3, 1 -> 3, 2 -> 3, 3
    /// </remarks>
    /// <param name="rows">The amount of rows to divide the grid in</param>
    /// <param name="columns">The amount of columns to divide the grid in</param>
    /// <param name="transformationViews">The buffer in which to store the views</param>
    public static void DivideInto2DViews(Span<Matrix4x4> transformationViews, int rows, int columns)
        => Div2DViews(transformationViews, rows, columns, 0, 0, 1f / columns, 1f / rows);

    /// <summary>
    /// Creates a set of transformation matrices to fill <paramref name="transformationViews"/> with out of an evenly spaced grid graphed along the area specified within <paramref name="texture"/>'s dimensions
    /// </summary>
    /// <remarks>
    /// The views are arranged from the top-left corner, going down, and back to the top-most row in the next column. For example, a 4x4 grid would produce the following, where 0, 0 (column, row) is the top-left corner:
    /// <list type="table">
    /// <item> 0, 0  |  1, 0  |  2, 0  |  3, 0 </item>
    /// <item> 0, 1  |  1, 1  |  2, 1  |  3, 1 </item>
    /// <item> 0, 2  |  1, 2  |  2, 2  |  3, 2 </item>
    /// <item> 0, 3  |  1, 3  |  2, 3  |  3, 3 </item>
    /// </list>
    /// would be ordered in the buffer as: 0, 0 -> 0, 1 -> 0, 2 -> 0, 3 -> 1, 0 -> 1, 1 -> 1, 2 -> 1, 3 -> 1, 0 -> 1, 1 -> 1, 2 -> 1, 3 -> 2, 0 -> 2, 1 -> 2, 2 -> 2, 3 -> 3, 0 -> 3, 1 -> 3, 2 -> 3, 3
    /// </remarks>
    /// <param name="texture">The texture to create the views for</param>
    /// <param name="rows">The amount of rows to divide the height of <paramref name="texture"/> in</param>
    /// <param name="columns">The amount of columns to divide the width of <paramref name="texture"/> in</param>
    /// <param name="transformationViews">The buffer in which to store the views</param>
    /// <param name="area">The area of the texture in texels within which to graph the grid</param>
    public static void DivideInto2DViews(Texture texture, Span<Matrix4x4> transformationViews, FRectangle area, int rows, int columns)
        => Div2DViews(transformationViews, rows, columns, area.X / texture.Width, area.Y / texture.Height, area.Width / columns / texture.Width, area.Height / rows / texture.Height);

    /// <summary>
    /// Creates a set of transformation matrices to fill <paramref name="transformationViews"/> with out of an evenly spaced grid within a defined relative area
    /// </summary>
    /// <remarks>
    /// The views are arranged from the top-left corner, going down, and back to the top-most row in the next column. For example, a 4x4 grid would produce the following, where 0, 0 (column, row) is the top-left corner:
    /// <list type="table">
    /// <item> 0, 0  |  1, 0  |  2, 0  |  3, 0 </item>
    /// <item> 0, 1  |  1, 1  |  2, 1  |  3, 1 </item>
    /// <item> 0, 2  |  1, 2  |  2, 2  |  3, 2 </item>
    /// <item> 0, 3  |  1, 3  |  2, 3  |  3, 3 </item>
    /// </list>
    /// would be ordered in the buffer as: 0, 0 -> 0, 1 -> 0, 2 -> 0, 3 -> 1, 0 -> 1, 1 -> 1, 2 -> 1, 3 -> 1, 0 -> 1, 1 -> 1, 2 -> 1, 3 -> 2, 0 -> 2, 1 -> 2, 2 -> 2, 3 -> 3, 0 -> 3, 1 -> 3, 2 -> 3, 3
    /// </remarks>
    /// <param name="rows">The amount of rows to divide the height of the relative area in</param>
    /// <param name="columns">The amount of columns to divide the width of the relative area in</param>
    /// <param name="transformationViews">The buffer in which to store the views</param>
    /// <param name="bottomright">The bottom-right point of the area, denoted in relative coordinates from 0,0 to 1,1</param>
    /// <param name="topleft">The top-left point of the area, denoted in relative coordinates from 0,0 to 1,1</param>
    public static void DivideInto2DViews(Span<Matrix4x4> transformationViews, Vector2 topleft, Vector2 bottomright, int rows, int columns)
    {
        var diff = bottomright - topleft;
        Div2DViews(transformationViews, rows, columns, topleft.X, topleft.Y, diff.X / rows, diff.Y / columns);
    }

    private static void Div2DViews(Span<Matrix4x4> transformationViews, int rows, int cols, float xoff, float yoff, float woff, float hoff)
    {
        var len = rows * cols;
        if (len > transformationViews.Length)
            throw new ArgumentException($"The transformationViews buffer doesn't have enough space to fit all the views: It has a length of {transformationViews.Length}, while a length of {len} is necessary", nameof(transformationViews));

        int i = 0;
        var scale = Matrix4x4.CreateScale(woff, hoff, 1);
        for (int x = 0; x < cols; x++)
            for (int y = 0; y < rows; y++)
                transformationViews[i++] = scale * Matrix4x4.CreateTranslation(xoff + woff * x, yoff + hoff * y, 0);
    }
}
