using System.Collections;
using System.Diagnostics;
using SDL2.NET;

namespace VDStudios.MagicEngine.Demo.Common.Utilities;

public class CharacterAnimationContainer<TViewport> : IReadOnlyCollection<KeyValuePair<CharacterAnimationKind, TimedSequence<TViewport>>>
{
    private readonly Stopwatch _stopwatch;
    private readonly TimedSequence<TViewport>[] _seqs;

    public CharacterAnimationKind CurrentKind { get; private set; }
    public TimedSequence<TViewport> CurrentAnimation => this[CurrentKind];

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

    public TimedSequence<TViewport> this[CharacterAnimationKind kind]
        => kind is > CharacterAnimationKind.DownLeft or < 0 ? throw new ArgumentException($"Unknown CharacterAnimationKind {kind}", nameof(kind)) : _seqs[(int)kind];

    public CharacterAnimationContainer(TimeSpan interval, IEnumerable<TViewport> idle, bool doesIdleReverse, IEnumerable<TViewport> active, bool doesActiveReverse, IEnumerable<TViewport> up, bool doesUpReverse, IEnumerable<TViewport> down, bool doesDownReverse, IEnumerable<TViewport> left, bool doesLeftReverse, IEnumerable<TViewport> right, bool doesRightReverse, IEnumerable<TViewport> upRight, bool doesUpRightReverse, IEnumerable<TViewport> downRight, bool doesDownRightReverse, IEnumerable<TViewport> upLeft, bool doesUpLeftReverse, IEnumerable<TViewport> downLeft, bool doesDownLeftReverse)
    {
        _stopwatch = new();
        _seqs = new TimedSequence<TViewport>[(int)CharacterAnimationKind.DownLeft + 1];

        _seqs[(int)CharacterAnimationKind.Idle] = new TimedSequence<TViewport>(_stopwatch, interval, idle ?? throw new ArgumentNullException(nameof(idle))) { Reverse = doesIdleReverse };
        _seqs[(int)CharacterAnimationKind.Active] = new TimedSequence<TViewport>(_stopwatch, interval, active ?? throw new ArgumentNullException(nameof(active))) { Reverse = doesActiveReverse };
        _seqs[(int)CharacterAnimationKind.Up] = new TimedSequence<TViewport>(_stopwatch, interval, up ?? throw new ArgumentNullException(nameof(up))) { Reverse = doesUpReverse };
        _seqs[(int)CharacterAnimationKind.Down] = new TimedSequence<TViewport>(_stopwatch, interval, down ?? throw new ArgumentNullException(nameof(down))) { Reverse = doesDownReverse };
        _seqs[(int)CharacterAnimationKind.Left] = new TimedSequence<TViewport>(_stopwatch, interval, left ?? throw new ArgumentNullException(nameof(left))) { Reverse = doesLeftReverse };
        _seqs[(int)CharacterAnimationKind.Right] = new TimedSequence<TViewport>(_stopwatch, interval, right ?? throw new ArgumentNullException(nameof(right))) { Reverse = doesRightReverse };
        _seqs[(int)CharacterAnimationKind.UpRight] = new TimedSequence<TViewport>(_stopwatch, interval, upRight ?? throw new ArgumentNullException(nameof(upRight))) { Reverse = doesUpRightReverse };
        _seqs[(int)CharacterAnimationKind.DownRight] = new TimedSequence<TViewport>(_stopwatch, interval, downRight ?? throw new ArgumentNullException(nameof(downRight))) { Reverse = doesDownRightReverse };
        _seqs[(int)CharacterAnimationKind.UpLeft] = new TimedSequence<TViewport>(_stopwatch, interval, upLeft ?? throw new ArgumentNullException(nameof(upLeft))) { Reverse = doesUpLeftReverse };
        _seqs[(int)CharacterAnimationKind.DownLeft] = new TimedSequence<TViewport>(_stopwatch, interval, downLeft ?? throw new ArgumentNullException(nameof(downLeft))) { Reverse = doesDownLeftReverse };

        CurrentKind = CharacterAnimationKind.Idle;
    }

    public CharacterAnimationContainer(double intervalsPerSecond, IEnumerable<TViewport> idle, bool doesIdleReverse, IEnumerable<TViewport> active, bool doesActiveReverse, IEnumerable<TViewport> up, bool doesUpReverse, IEnumerable<TViewport> down, bool doesDownReverse, IEnumerable<TViewport> left, bool doesLeftReverse, IEnumerable<TViewport> right, bool doesRightReverse, IEnumerable<TViewport> upRight, bool doesUpRightReverse, IEnumerable<TViewport> downRight, bool doesDownRightReverse, IEnumerable<TViewport> upLeft, bool doesUpLeftReverse, IEnumerable<TViewport> downLeft, bool doesDownLeftReverse)
    : this(TimeSpan.FromSeconds(1d / intervalsPerSecond), idle, doesIdleReverse, active, doesActiveReverse, up, doesUpReverse, down, doesDownReverse, left, doesLeftReverse, right, doesRightReverse, upRight, doesUpRightReverse, downRight, doesDownRightReverse, upLeft, doesUpLeftReverse, downLeft, doesDownLeftReverse)
    { }

    public TimedSequence<TViewport> Idle => this[CharacterAnimationKind.Idle];

    public TimedSequence<TViewport> Active => this[CharacterAnimationKind.Active];

    public TimedSequence<TViewport> Up => this[CharacterAnimationKind.Up];

    public TimedSequence<TViewport> Down => this[CharacterAnimationKind.Down];

    public TimedSequence<TViewport> Left => this[CharacterAnimationKind.Left];

    public TimedSequence<TViewport> Right => this[CharacterAnimationKind.Right];

    public TimedSequence<TViewport> UpRight => this[CharacterAnimationKind.UpRight];

    public TimedSequence<TViewport> DownRight => this[CharacterAnimationKind.DownRight];

    public TimedSequence<TViewport> UpLeft => this[CharacterAnimationKind.UpLeft];

    public TimedSequence<TViewport> DownLeft => this[CharacterAnimationKind.DownLeft];

    public IEnumerator<KeyValuePair<CharacterAnimationKind, TimedSequence<TViewport>>> GetEnumerator()
    {
        for (int i = 0; i < _seqs.Length; i++)
            yield return new((CharacterAnimationKind)i, _seqs[i]);
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}