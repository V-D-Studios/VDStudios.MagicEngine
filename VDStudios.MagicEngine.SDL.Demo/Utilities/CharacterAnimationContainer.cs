using System.Collections;
using System.Diagnostics;
using SDL2.NET;

namespace VDStudios.MagicEngine.SDL.Demo.Utilities;

public class CharacterAnimationContainer : IReadOnlyCollection<KeyValuePair<CharacterAnimationKind, TimedSequence<Rectangle>>>
{
    private readonly Stopwatch _stopwatch;
    private readonly TimedSequence<Rectangle>[] _seqs;

    public CharacterAnimationKind CurrentKind { get; private set; }
    public TimedSequence<Rectangle> CurrentAnimation => this[CurrentKind];

    public int Count => _seqs.Length;

    public bool SwitchTo(CharacterAnimationKind kind)
    {
        if (kind == CurrentKind) return false;
        if (kind is > CharacterAnimationKind.DownLeft or < 0)
            throw new ArgumentException($"Unknown CharacterAnimationKind {kind}", nameof(kind));

        _stopwatch.Restart();
        CurrentAnimation.Reset();
        CurrentKind = kind;
        return true;
    }

    public TimedSequence<Rectangle> this[CharacterAnimationKind kind]
        => kind is > CharacterAnimationKind.DownLeft or < 0 ? throw new ArgumentException($"Unknown CharacterAnimationKind {kind}", nameof(kind)) : _seqs[(int)kind];

    public CharacterAnimationContainer(TimeSpan interval, IEnumerable<Rectangle> idle, bool doesIdleReverse, IEnumerable<Rectangle> active, bool doesActiveReverse, IEnumerable<Rectangle> up, bool doesUpReverse, IEnumerable<Rectangle> down, bool doesDownReverse, IEnumerable<Rectangle> left, bool doesLeftReverse, IEnumerable<Rectangle> right, bool doesRightReverse, IEnumerable<Rectangle> upRight, bool doesUpRightReverse, IEnumerable<Rectangle> downRight, bool doesDownRightReverse, IEnumerable<Rectangle> upLeft, bool doesUpLeftReverse, IEnumerable<Rectangle> downLeft, bool doesDownLeftReverse)
    {
        _stopwatch = new();
        _seqs = new TimedSequence<Rectangle>[(int)CharacterAnimationKind.DownLeft + 1];

        _seqs[(int)CharacterAnimationKind.Idle] = new TimedSequence<Rectangle>(_stopwatch, interval, idle ?? throw new ArgumentNullException(nameof(idle))) { Reverse = doesIdleReverse };
        _seqs[(int)CharacterAnimationKind.Active] = new TimedSequence<Rectangle>(_stopwatch, interval, active ?? throw new ArgumentNullException(nameof(active))) { Reverse = doesActiveReverse };
        _seqs[(int)CharacterAnimationKind.Up] = new TimedSequence<Rectangle>(_stopwatch, interval, up ?? throw new ArgumentNullException(nameof(up))) { Reverse = doesUpReverse };
        _seqs[(int)CharacterAnimationKind.Down] = new TimedSequence<Rectangle>(_stopwatch, interval, down ?? throw new ArgumentNullException(nameof(down))) { Reverse = doesDownReverse };
        _seqs[(int)CharacterAnimationKind.Left] = new TimedSequence<Rectangle>(_stopwatch, interval, left ?? throw new ArgumentNullException(nameof(left))) { Reverse = doesLeftReverse };
        _seqs[(int)CharacterAnimationKind.Right] = new TimedSequence<Rectangle>(_stopwatch, interval, right ?? throw new ArgumentNullException(nameof(right))) { Reverse = doesRightReverse };
        _seqs[(int)CharacterAnimationKind.UpRight] = new TimedSequence<Rectangle>(_stopwatch, interval, upRight ?? throw new ArgumentNullException(nameof(upRight))) { Reverse = doesUpRightReverse };
        _seqs[(int)CharacterAnimationKind.DownRight] = new TimedSequence<Rectangle>(_stopwatch, interval, downRight ?? throw new ArgumentNullException(nameof(downRight))) { Reverse = doesDownRightReverse };
        _seqs[(int)CharacterAnimationKind.UpLeft] = new TimedSequence<Rectangle>(_stopwatch, interval, upLeft ?? throw new ArgumentNullException(nameof(upLeft))) { Reverse = doesUpLeftReverse };
        _seqs[(int)CharacterAnimationKind.DownLeft] = new TimedSequence<Rectangle>(_stopwatch, interval, downLeft ?? throw new ArgumentNullException(nameof(downLeft))) { Reverse = doesDownLeftReverse };

        CurrentKind = CharacterAnimationKind.Idle;
    }

    public CharacterAnimationContainer(double intervalsPerSecond, IEnumerable<Rectangle> idle, bool doesIdleReverse, IEnumerable<Rectangle> active, bool doesActiveReverse, IEnumerable<Rectangle> up, bool doesUpReverse, IEnumerable<Rectangle> down, bool doesDownReverse, IEnumerable<Rectangle> left, bool doesLeftReverse, IEnumerable<Rectangle> right, bool doesRightReverse, IEnumerable<Rectangle> upRight, bool doesUpRightReverse, IEnumerable<Rectangle> downRight, bool doesDownRightReverse, IEnumerable<Rectangle> upLeft, bool doesUpLeftReverse, IEnumerable<Rectangle> downLeft, bool doesDownLeftReverse)
    : this(TimeSpan.FromSeconds(1d / intervalsPerSecond), idle, doesIdleReverse, active, doesActiveReverse, up, doesUpReverse, down, doesDownReverse, left, doesLeftReverse, right, doesRightReverse, upRight, doesUpRightReverse, downRight, doesDownRightReverse, upLeft, doesUpLeftReverse, downLeft, doesDownLeftReverse)
    { }

    public TimedSequence<Rectangle> Idle => this[CharacterAnimationKind.Idle];

    public TimedSequence<Rectangle> Active => this[CharacterAnimationKind.Active];

    public TimedSequence<Rectangle> Up => this[CharacterAnimationKind.Up];

    public TimedSequence<Rectangle> Down => this[CharacterAnimationKind.Down];

    public TimedSequence<Rectangle> Left => this[CharacterAnimationKind.Left];

    public TimedSequence<Rectangle> Right => this[CharacterAnimationKind.Right];

    public TimedSequence<Rectangle> UpRight => this[CharacterAnimationKind.UpRight];

    public TimedSequence<Rectangle> DownRight => this[CharacterAnimationKind.DownRight];

    public TimedSequence<Rectangle> UpLeft => this[CharacterAnimationKind.UpLeft];

    public TimedSequence<Rectangle> DownLeft => this[CharacterAnimationKind.DownLeft];

    public IEnumerator<KeyValuePair<CharacterAnimationKind, TimedSequence<Rectangle>>> GetEnumerator()
    {
        for (int i = 0; i < _seqs.Length; i++)
            yield return new((CharacterAnimationKind)i, _seqs[i]);
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}