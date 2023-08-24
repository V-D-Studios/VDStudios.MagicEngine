using System.Collections.Immutable;
using System.Diagnostics;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a sequence of elements of type <typeparamref name="T"/> where the currently selected element shifts across time
/// </summary>
/// <typeparam name="T">The type of element this sequence has</typeparam>
public class TimedSequence<T>
{
    private readonly Stopwatch _timer = new();

    /// <summary>
    /// Whether this <see cref="TimedSequence{T}"/> travels backwards upon reaching the end
    /// </summary>
    public bool Reverse { get; set; }

    /// <summary>
    /// The previous index of this <see cref="TimedSequence{T}"/>
    /// </summary>
    public int PrevIndex { get; private set; }

    /// <summary>
    /// The current index of this <see cref="TimedSequence{T}"/>
    /// </summary>
    public int Index { get; private set; }

    /// <summary>
    /// The next index this <see cref="TimedSequence{T}"/> will have
    /// </summary>
    public int NextIndex { get; private set; }

    /// <summary>
    /// Whether this <see cref="TimedSequence{T}"/> is currently going backwards
    /// </summary>
    public bool IsReversing { get; private set; }

    /// <summary>
    /// The items contained in this <see cref="TimedSequence{T}"/>
    /// </summary>
    public IEnumerable<T> Items => _items;

    /// <summary>
    /// The element selected by <see cref="Index"/>
    /// </summary>
    public T CurrentElement => _items[Index];

    /// <summary>
    /// The amount of times per second this <see cref="TimedSequence{T}"/> will step
    /// </summary>
    public double IntervalsPerSecond => 1 / Interval.TotalSeconds;

    /// <summary>
    /// The amount of time every value of <see cref="Index"/> will remain selected
    /// </summary>
    public TimeSpan Interval
    {
        get => IntervalField;
        set
        {
            IntervalField = value;
            _timer.Restart();
        }
    }

    private TimeSpan IntervalField;

    private readonly ImmutableArray<T> _items;

    private (int index, bool reverse) NextStep()
        => !IsReversing
            ? Index != _items.Length - 1
                ? ((int index, bool reverse))(Index + 1, false)
                : Reverse ? ((int index, bool reverse))(Index, true) : ((int index, bool reverse))(0, false)
            : Index > 0 ? ((int index, bool reverse))(Index - 1, true) : ((int index, bool reverse))(Index + 1, false);

    /// <summary>
    /// Forces the <see cref="TimedSequence{T}"/> to step to the next index
    /// </summary>
    public void Step()
    {
        PrevIndex = Index;
        (Index, IsReversing) = NextStep();
        NextIndex = NextStep().index;
        _timer.Restart();
    }

    /// <summary>
    /// Checks if this <see cref="TimedSequence{T}"/> should update
    /// </summary>
    public void Update()
    {
        if (_timer.Elapsed > Interval)
            Step();
    }

    /// <summary>
    /// Creates a new instance of class <see cref="TimedSequence{T}"/> over <paramref name="items"/> with an interval of <c>1 / <paramref name="intervalsPerSecond"/></c>
    /// </summary>
    /// <param name="intervalsPerSecond">The amount of intervals that should happen in a single second</param>
    /// <param name="items">The items to create this <see cref="TimedSequence{T}"/> over</param>
    public TimedSequence(double intervalsPerSecond, IEnumerable<T> items) : this(TimeSpan.FromSeconds(1d / intervalsPerSecond), items) { }

    /// <summary>
    /// Creates a new instance of class <see cref="TimedSequence{T}"/> over <paramref name="items"/>
    /// </summary>
    /// <param name="interval">The amount of time before performing a step</param>
    /// <param name="items">The items to create this <see cref="TimedSequence{T}"/> over</param>
    public TimedSequence(TimeSpan interval, IEnumerable<T> items)
    {
        _items = items.ToImmutableArray();
        Interval = interval;
    }
}
