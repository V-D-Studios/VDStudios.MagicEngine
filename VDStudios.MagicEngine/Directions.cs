using System.Numerics;

namespace VDStudios.MagicEngine;

/// <summary>
/// A collection of the standard 8 directions
/// </summary>
/// <remarks>
/// This directions assume that, in a graphical aspect, the origin of the output is at the top-left corner
/// </remarks>
public static class Directions
{
    /// <summary>
    /// (0, -1)
    /// </summary>
    public static Vector2 Up { get; }

    /// <summary>
    /// (0, 1)
    /// </summary>
    public static Vector2 Down { get; }

    /// <summary>
    /// (-1, 0)
    /// </summary>
    public static Vector2 Left { get; }

    /// <summary>
    /// (1, 0)
    /// </summary>
    public static Vector2 Right { get; }

    /// <summary>
    /// (1, -1)
    /// </summary>
    public static Vector2 UpRight { get; }

    /// <summary>
    /// (-1, -1)
    /// </summary>
    public static Vector2 UpLeft { get; }

    /// <summary>
    /// (1, 1)
    /// </summary>
    public static Vector2 DownRight { get; }

    /// <summary>
    /// (-1, 1)
    /// </summary>
    public static Vector2 DownLeft { get; }

    static Directions()
    {
        Up = new(0, -1);
        Down = new(0, 1);
        Left = new(-1, 0);
        Right = new(1, 0);
        UpRight = Up + Right;
        UpLeft = Up + Left;
        DownRight = Down + Right;
        DownLeft = Down + Left;
    }
}