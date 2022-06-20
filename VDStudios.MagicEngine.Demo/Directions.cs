using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.Demo;
public static class Directions
{
    public static readonly Vector2 Up;
    public static readonly Vector2 Down;
    public static readonly Vector2 Left;
    public static readonly Vector2 Right;

    public static readonly Vector2 UpRight;
    public static readonly Vector2 UpLeft;

    public static readonly Vector2 DownRight;
    public static readonly Vector2 DownLeft;

    static Directions()
    {
        Up = new(0, -1);
        Down = new(0, 1);
        Left = new(-1, 0);
        Right = new(1, 0);
        UpRight = Up + Left;
        UpLeft = Up + Right;
        DownRight = Down + Left;
        DownLeft = Down + Right;
    }
}
