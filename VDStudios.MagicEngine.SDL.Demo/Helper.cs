using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SDL2.NET;

namespace VDStudios.MagicEngine.SDL.Demo;

public static class Helper
{
    public static Vector2 GetDirection(this CharacterAnimationKind kind)
        => kind switch
        {
            CharacterAnimationKind.Idle | CharacterAnimationKind.Active => default,
            CharacterAnimationKind.Up => Directions.Up,
            CharacterAnimationKind.Down => Directions.Down,
            CharacterAnimationKind.Left => Directions.Left,
            CharacterAnimationKind.Right => Directions.Right,
            CharacterAnimationKind.UpRight => Directions.UpRight,
            CharacterAnimationKind.UpLeft => Directions.UpLeft,
            CharacterAnimationKind.DownRight => Directions.DownRight,
            CharacterAnimationKind.DownLeft => Directions.DownLeft,
            _ => throw new ArgumentException($"Unknown CharacterAnimationKind {kind}", nameof(kind))
        };

    public static bool TryGetFromDirection(Vector2 direction, out CharacterAnimationKind characterAnimationKind)
    {
        if (direction == default)
            characterAnimationKind = CharacterAnimationKind.Idle;
        else if (direction == Directions.Up)
            characterAnimationKind = CharacterAnimationKind.Up;
        else if (direction == Directions.Down)
            characterAnimationKind = CharacterAnimationKind.Down;
        else if (direction == Directions.Left)
            characterAnimationKind = CharacterAnimationKind.Left;
        else if (direction == Directions.Right)
            characterAnimationKind = CharacterAnimationKind.Right;
        else if (direction == Directions.UpRight)
            characterAnimationKind = CharacterAnimationKind.UpRight;
        else if (direction == Directions.UpLeft)
            characterAnimationKind = CharacterAnimationKind.UpLeft;
        else if (direction == Directions.DownRight)
            characterAnimationKind = CharacterAnimationKind.DownRight;
        else if (direction == Directions.DownLeft)
            characterAnimationKind = CharacterAnimationKind.DownLeft;
        else
        {
            characterAnimationKind = default;
            return false;
        }

        return true;
    }

    public static IEnumerable<Rectangle> GetRectangles(ImmutableArray<(int X, int Y, int Width, int Height)> span)
    {
        for (int i = 0; i < span.Length; i++) 
        {
            var (X, Y, Width, Height) = span[i];
            yield return new(Width, Height, X, Y);
        }
    }
}
