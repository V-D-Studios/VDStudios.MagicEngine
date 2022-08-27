using SDL2.NET;
using System.Numerics;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a set of loosely related extensions
/// </summary>
public static class AssortedExtensions
{
    /// <summary>
    /// Converts a <see cref="Vector2"/> into an SDL.NET <see cref="FPoint"/>
    /// </summary>
    public static FPoint ToFPoint(this Vector2 vector)
        => new(vector.X, vector.Y);

    /// <summary>
    /// Converts an SLD.NET <see cref="FPoint"/> into a <see cref="Vector2"/>
    /// </summary>
    public static Vector2 ToVector(this FPoint point)
        => new(point.X, point.Y);

    /// <summary>
    /// Gets the bytes associated to a given <see cref="string"/>
    /// </summary>
    public static byte[] GetUTF8Bytes(this string str)
        => Encoding.UTF8.GetBytes(str);
}
