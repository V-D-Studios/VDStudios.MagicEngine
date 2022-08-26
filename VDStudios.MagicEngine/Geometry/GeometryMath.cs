using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

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
}
