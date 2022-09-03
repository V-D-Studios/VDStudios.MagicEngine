using SDL2.NET;
using System.Numerics;
using System.Reflection;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a set of loosely related extensions
/// </summary>
public static class AssortedExtensions
{
    /// <summary>
    /// This method serves a shorthand for calling <see cref="object.GetType"/> on <paramref name="obj"/> and taking <see cref="MemberInfo.Name"/>
    /// </summary>
    /// <param name="obj">The object to get the type name from</param>
    /// <returns>The type name of <paramref name="obj"/></returns>
    public static string GetTypeName(this object obj) => obj.GetType().Name;

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
