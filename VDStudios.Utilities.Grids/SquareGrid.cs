using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace VDStudios.Utilities.Grids;

public struct SquareCell<TContext>
{
    internal struct GridAccesor
    {
        public int X;
        public int Y;
        public SquareCell<TContext> Get(SquareCell<TContext>[,] grid) => grid[X, Y];
    }

    private readonly SquareGrid<TContext> Owner;

    internal readonly GridAccesor Up;
    internal readonly GridAccesor Down;
    internal readonly GridAccesor Left;
    internal readonly GridAccesor Right;

    public TContext? Context => _context;
    internal TContext? _context;
    public bool HasUp { get; }
    public bool HasDown { get; }
    public bool HasLeft { get; }
    public bool HasRight { get; }
    public int X { get; }
    public int Y { get; }

    internal SquareCell(int x, int y, (int x, int y)? up, (int x, int y)? down, (int x, int y)? left, (int x, int y)? right, SquareGrid<TContext> owner)
    {
        X = x;
        Y = y;

        if (up is (int, int) _u)
        {
            HasUp = true;
            Up = new() { X = _u.x, Y = _u.y };
        }

        if (down is (int, int) _d)
        {
            HasDown = true;
            Up = new() { X = _d.x, Y = _d.y };
        }

        if (left is (int, int) _l)
        {
            HasLeft = true;
            Up = new() { X = _l.x, Y = _l.y };
        }

        if (right is (int, int) _r)
        {
            HasRight = true;
            Up = new() { X = _r.x, Y = _r.y };
        }

        Owner = owner;
    }

    public SquareCell<TContext> GetUp() => HasUp ? Up.Get(Owner._grid) : throw new InvalidOperationException("There is not a square to the up of this one");
    public SquareCell<TContext> GetDown() => HasDown ? Down.Get(Owner._grid) : throw new InvalidOperationException("There is not a square to the down of this one");
    public SquareCell<TContext> GetLeft() => HasLeft ? Left.Get(Owner._grid) : throw new InvalidOperationException("There is not a square to the left of this one");
    public SquareCell<TContext> GetRight() => HasRight ? Right.Get(Owner._grid) : throw new InvalidOperationException("There is not a square to the right of this one");

    public bool TryGetUp([NotNullWhen(true)] out SquareCell<TContext> result)
    {
        result = HasUp ? Up.Get(Owner._grid) : default;
        return HasUp;
    }

    public bool TryGetDown([NotNullWhen(true)] out SquareCell<TContext> result)
    {
        result = HasDown ? Down.Get(Owner._grid) : default;
        return HasDown;
    }

    public bool TryGetLeft([NotNullWhen(true)] out SquareCell<TContext> result)
    {
        result = HasLeft ? Left.Get(Owner._grid) : default;
        return HasLeft;
    }

    public bool TryGetRight([NotNullWhen(true)] out SquareCell<TContext> result)
    {
        result = HasRight ? Right.Get(Owner._grid) : default;
        return HasRight;
    }
}

/// <summary>
/// Describes a fixed-size grid of square cells
/// </summary>
/// <remarks>
/// This grid enumerates left-to-right and top-to-bottom; thus <c>(0, 0)</c> is the top-left cell, and the last cell is the bottom-right
/// </remarks>
/// <typeparam name="TContext">The type of data that each cell </typeparam>
public class SquareGrid<TContext> : IReadOnlyCollection<SquareCell<TContext>>
{
    public delegate TContext ContextSelector(int x, int y);
    internal readonly SquareCell<TContext>[,] _grid;

    public SquareGrid(int height, int width)
    {
        var g = _grid = new SquareCell<TContext>[height, width];
        Height = height;
        Width = width;

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                g[x, y] = new(
                    x: x,
                    y: y,
                    up: y > 0 ? new(x, y - 1) : null,
                    down: y < height - 1 ? new(x, y + 1) : null,
                    left: x > 0 ? new(x - 1, y) : null,
                    right: x < width - 1 ? new(x + 1, y) : null,
                    owner: this
                );
    }

    public SquareGrid(int height, int width, ContextSelector selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        var g = _grid = new SquareCell<TContext>[height, width];
        Height = height;
        Width = width;

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                g[x, y] = new(
                    x: x,
                    y: y,
                    up: y > 0 ? new(x, y - 1) : null,
                    down: y < height - 1 ? new(x, y + 1) : null,
                    left: x > 0 ? new(x - 1, y) : null,
                    right: x < width - 1 ? new(x + 1, y) : null,
                    owner: this
                )
                {
                    _context = selector(x, y)
                };
    }

    public SquareCell<TContext> this[int x, int y] => _grid[x, y];
    public void SetContext(TContext context, int x, int y) => _grid[x, y]._context = context;

    public int Height { get; }
    public int Width { get; }
    public int Count { get; }

    public IEnumerator<SquareCell<TContext>> GetEnumerator()
    {
        for (int y = 0; y < _grid.GetLength(1); y++)
            for (int x = 0; x < _grid.GetLength(0); x++)
                yield return _grid[x, y];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
