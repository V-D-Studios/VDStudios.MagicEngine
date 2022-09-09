using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace VDStudios.Utilities.Grids;

/// <summary>
/// Describes a fixed-size grid of square cells
/// </summary>
/// <remarks>
/// This grid enumerates left-to-right and top-to-bottom; thus <c>(0, 0)</c> is the top-left cell, and the last cell is the bottom-right
/// </remarks>
/// <typeparam name="TContext">The type of data that each cell </typeparam>
public class SquareGrid<TContext> : IReadOnlyCollection<SquareGrid<TContext>.SquareCell>
{
    public struct SquareCell
    {
        private readonly SquareGrid<TContext> Owner;

        internal readonly int Up;
        internal readonly int Down;
        internal readonly int Left;
        internal readonly int Right;

        public bool HasUp { get; }
        public bool HasDown { get; }
        public bool HasLeft { get; }
        public bool HasRight { get; }
        public int X { get; }
        public int Y { get; }

        internal SquareCell(int x, int y, int? up, int? down, int? left, int? right, SquareGrid<TContext> owner)
        {
            X = x;
            Y = y;

            if (up is int _u)
            {
                HasUp = true;
                Up = _u;
            }

            if (down is int _d)
            {
                HasDown = true;
                Up = _d;
            }
            
            if (left is int _l)
            {
                HasLeft = true;
                Up = _l;
            }
            
            if (right is int _r)
            {
                HasRight = true;
                Up = _r;
            }

            Owner = owner;
        }

        public SquareCell GetUp() => HasUp ? Owner._grid[Up] : throw new InvalidOperationException("There is not a square to the up of this one");
        public SquareCell GetDown() => HasDown ? Owner._grid[Down] : throw new InvalidOperationException("There is not a square to the down of this one");
        public SquareCell GetLeft() => HasLeft ? Owner._grid[Left] : throw new InvalidOperationException("There is not a square to the left of this one");
        public SquareCell GetRight() => HasRight ? Owner._grid[Right] : throw new InvalidOperationException("There is not a square to the right of this one");

        public bool TryGetUp([NotNullWhen(true)] out SquareCell result)
        {
            result = HasUp ? Owner._grid[Up] : default;
            return HasUp;
        }

        public bool TryGetDown([NotNullWhen(true)] out SquareCell result) 
        {
            result = HasDown ? Owner._grid[Down] : default;
            return HasDown;
        }

        public bool TryGetLeft([NotNullWhen(true)] out SquareCell result) 
        {
            result = HasLeft ? Owner._grid[Left] : default;
            return HasLeft;
        }

        public bool TryGetRight([NotNullWhen(true)] out SquareCell result) 
        {
            result = HasRight ? Owner._grid[Right] : default;
            return HasRight;
        }
    }
    private readonly SquareCell[] _grid;

    public SquareGrid(int height, int width)
    {
        var size = height * width;
        var g = _grid = new SquareCell[size];

        g[0, 0] = new(0, 0, null, width, null, 1, this);
        g[0, width - 1] = new(width - 1, 0, null, width * 2 - 1, width - 2, null, this);
        g[height - 1, 0] = new(height - 1, 0, null, width * 2 - 1, width - 2, null, this);
        g[width - 1] = new(width - 1, 0, null, width * 2 - 1, width - 2, null, this);

        for (int i = 1; i < width - 1; i++) 
    }
}
