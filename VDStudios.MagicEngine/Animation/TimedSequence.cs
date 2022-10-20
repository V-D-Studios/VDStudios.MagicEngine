using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D11;

namespace VDStudios.MagicEngine.Animation;

/// <summary>
/// A set of standard and default modes for sequencing through a <see cref="TimedSequence{TState, TTime}"/>
/// </summary>
public enum TimedSequenceMode
{
    /// <summary>
    /// Causes the <see cref="TimedSequence{TState, TTime}"/> to go back to the first frame upon reaching the last one
    /// </summary>
    Loop,

    /// <summary>
    /// Causes the <see cref="TimedSequence{TState, TTime}"/> to walk backwards through the frames upon reaching the last one
    /// </summary>
    Rewind,

    /// <summary>
    /// Causes the <see cref="TimedSequence{TState, TTime}"/> to walk backwards through the frames upon reaching the last one, using the first and last frames for an extra frame
    /// </summary>
    BufferedRewind
}

/// <summary>
/// Represents a sequence of <typeparamref name="TState"/> states that moves across time
/// </summary>
/// <typeparam name="TState">The type of the state this <see cref="TimedSequence{TState, TTime}"/> will have at any given frame</typeparam>
/// <typeparam name="TTime">The type of the object representing time</typeparam>
public class TimedSequence<TState, TTime> : IReadOnlyList<TState> where TState : notnull where TTime : struct
{
    #region Helpers

    /// <summary>
    /// Represents a description for a selector: Can either be a <see cref="TimedSequenceMode"/> or a <see cref="FrameSelector"/>
    /// </summary>
    public readonly struct FrameSelectorDescription
    {
        internal readonly TimedSequenceMode Mode;
        internal readonly FrameSelector? Selector;

        private FrameSelectorDescription(TimedSequenceMode mode) : this() => Mode = mode;
        private FrameSelectorDescription(FrameSelector? selector) : this() => Selector = selector;

        /// <inheritdoc/>
        public static implicit operator FrameSelectorDescription(TimedSequenceMode mode) => new(mode);
        /// <inheritdoc/>
        public static implicit operator FrameSelectorDescription(FrameSelector selector) => new(selector);
    }

    /// <summary>
    /// Represents a method that selects the previous, next and current frame for a <see cref="TimedSequence{TState, TTime}"/>
    /// </summary>
    /// <param name="caller">The <see cref="TimedSequence{TState, TTime}"/> that called the method delegate</param>
    /// <param name="current">The current frame <paramref name="caller"/> is at</param>
    /// <param name="total">The total amount of frames <paramref name="caller"/> has</param>
    /// <returns>A tuple containing the previous, next and current frame indices, where the current frame index is the index the sequence will move onto</returns>
    public delegate (int Previous, int Next, int Current) FrameSelector(TimedSequence<TState, TTime> caller, int current, int total);

    private (int Previous, int Next, int Current) LoopSelector(TimedSequence<TState, TTime> caller, int c, int t)
        => (c % t, (c + 2) % t, (c + 1) % t);

    bool isRewinding = false;
    private (int Previous, int Next, int Current) RewindSelector(TimedSequence<TState, TTime> caller, int c, int t)
    {
        if (isRewinding)
        {
            if (c == 1)
            {
                isRewinding = false;
                return (c, 1 % t, 0);
            }
            return (c, c - 2, c - 1);
        }

        if (c == t - 2)
        {
            isRewinding = true;
            return (c, t - 2, t - 1);
        }
        return (c, (c + 2) % t, (c + 1) % t);
    }

    private (int Previous, int Next, int Current) BufferedRewindSelector(TimedSequence<TState, TTime> caller, int c, int t)
    {
        (int, int, int) p;
        if (isRewinding)
        {
            if (c == 0)
            {
                isRewinding = false;
                p = (c, 1, 0);
                return p;
            }
            p = (c, int.Max(0, c - 2), c - 1);
            return p;
        }

        if (c == t - 1)
        {
            isRewinding = true;
            p = (c, t - 2, t - 1);
            return p;
        }
        p = (c, int.Min(c + 2, t - 1), (c + 1) % t);
        return p;
    }

    /// <summary>
    /// Represents a Frame in a sequence
    /// </summary>
    /// <param name="State">The state the sequence will represent when this frame is active</param>
    /// <param name="Offset">A value representing a time offset between frames, for non uniform sequences</param>
    public readonly record struct Frame(TState State, TTime Offset = default)
    {
        /// <summary>
        /// Implicitly converts a Tuple with an equal structure to <see cref="Frame"/> to a <see cref="Frame"/>
        /// </summary>
        public static implicit operator Frame((TState State, TTime Offset) tuple)
            => new(tuple.State, tuple.Offset);

        /// <summary>
        /// Implicitly converts a Tuple with a similar structure to <see cref="Frame"/> to a <see cref="Frame"/>
        /// </summary>
        public static implicit operator Frame((TTime Offset, TState State) tuple)
            => new(tuple.State, tuple.Offset);
    }

    #endregion

    #region Construction

    /// <summary>
    /// Constructs a new object of type <see cref="TimedSequence{TState, TTime}"/>
    /// </summary>
    /// <param name="states">An enumerable containing the states for this sequence</param>
    /// <param name="timeKeeper">The keeper that will keep track of frame advances in this <see cref="TimedSequence{TState, TTime}"/></param>
    /// <param name="selector">The frame selector for this <see cref="TimedSequence{TState, TTime}"/>. The type listed performs an implicit conversion for either a <see cref="TimedSequenceMode"/> or a <see cref="FrameSelector"/></param>
    public TimedSequence(ISequenceTimeKeeper<TTime> timeKeeper, FrameSelectorDescription selector, IEnumerable<TState> states)
    {
        ThrowHelpers.ThrowIfNullOrEmpty(states);

        ArgumentNullException.ThrowIfNull(timeKeeper);
        _timeKeeper = timeKeeper;
        _selector = selector.Selector ?? selector.Mode switch
        {
            TimedSequenceMode.Loop => LoopSelector,
            TimedSequenceMode.Rewind => RewindSelector,
            TimedSequenceMode.BufferedRewind => BufferedRewindSelector,
            _ => throw new ArgumentException($"FrameSelectorDescription contains an unknown TimedSequenceMode: {selector.Mode}", nameof(selector))
        };

        Frame[] frames;
        if (states is not ICollection<TState> coll)
            frames = states.Select(x => new Frame(x, default)).ToArray();
        else
        {
            frames = new Frame[coll.Count];

            int i = 0;
            foreach (var state in states) frames[i++] = new(state, default);
        }

        _frames = frames;
    }

    /// <summary>
    /// Constructs a new object of type <see cref="TimedSequence{TState, TTime}"/>
    /// </summary>
    /// <param name="states">An array containing the states for this sequence</param>
    /// <param name="timeKeeper">The keeper that will keep track of frame advances in this <see cref="TimedSequence{TState, TTime}"/></param>/// <param name="selector">The frame selector for this <see cref="TimedSequence{TState, TTime}"/>. The type listed performs an implicit conversion for either a <see cref="TimedSequenceMode"/> or a <see cref="FrameSelector"/></param>
    public TimedSequence(ISequenceTimeKeeper<TTime> timeKeeper, FrameSelectorDescription selector, params TState[] states)
    {
        ThrowHelpers.ThrowIfNullOrEmpty(states);

        ArgumentNullException.ThrowIfNull(timeKeeper);
        _timeKeeper = timeKeeper;
        _selector = selector.Selector ?? selector.Mode switch
        {
            TimedSequenceMode.Loop => LoopSelector,
            TimedSequenceMode.Rewind => RewindSelector,
            TimedSequenceMode.BufferedRewind => BufferedRewindSelector,
            _ => throw new ArgumentException($"FrameSelectorDescription contains an unknown TimedSequenceMode: {selector.Mode}", nameof(selector))
        };

        _frames = new Frame[states.Length];
        for (int i = 0; i < states.Length; i++)
            _frames[i] = new(states[i], default);
    }

    /// <summary>
    /// Constructs a new object of type <see cref="TimedSequence{TState, TTime}"/>
    /// </summary>
    /// <param name="frames">An array containing the frames for this sequence</param>
    /// <param name="timeKeeper">The keeper that will keep track of frame advances in this <see cref="TimedSequence{TState, TTime}"/></param>/// <param name="selector">The frame selector for this <see cref="TimedSequence{TState, TTime}"/>. The type listed performs an implicit conversion for either a <see cref="TimedSequenceMode"/> or a <see cref="FrameSelector"/></param>
    public TimedSequence(ISequenceTimeKeeper<TTime> timeKeeper, FrameSelectorDescription selector, params Frame[] frames)
    {
        ThrowHelpers.ThrowIfNullOrEmpty(frames);

        ArgumentNullException.ThrowIfNull(timeKeeper);
        _timeKeeper = timeKeeper;
        _selector = selector.Selector ?? selector.Mode switch
        {
            TimedSequenceMode.Loop => LoopSelector,
            TimedSequenceMode.Rewind => RewindSelector,
            TimedSequenceMode.BufferedRewind => BufferedRewindSelector,
            _ => throw new ArgumentException($"FrameSelectorDescription contains an unknown TimedSequenceMode: {selector.Mode}", nameof(selector))
        };

        _frames = new Frame[frames.Length];
        frames.CopyTo(_frames, 0);
    }

    /// <summary>
    /// Constructs a new object of type <see cref="TimedSequence{TState, TTime}"/>
    /// </summary>
    /// <param name="frames">An array containing the frames for this sequence</param>
    /// <param name="timeKeeper">The keeper that will keep track of frame advances in this <see cref="TimedSequence{TState, TTime}"/></param>/// <param name="selector">The frame selector for this <see cref="TimedSequence{TState, TTime}"/>. The type listed performs an implicit conversion for either a <see cref="TimedSequenceMode"/> or a <see cref="FrameSelector"/></param>
    public TimedSequence(ISequenceTimeKeeper<TTime> timeKeeper, FrameSelectorDescription selector, IEnumerable<Frame> frames)
    {
        ThrowHelpers.ThrowIfNullOrEmpty(frames);

        ArgumentNullException.ThrowIfNull(timeKeeper);
        _timeKeeper = timeKeeper;
        _selector = selector.Selector ?? selector.Mode switch
        {
            TimedSequenceMode.Loop => LoopSelector,
            TimedSequenceMode.Rewind => RewindSelector,
            TimedSequenceMode.BufferedRewind => BufferedRewindSelector,
            _ => throw new ArgumentException($"FrameSelectorDescription contains an unknown TimedSequenceMode: {selector.Mode}", nameof(selector))
        };

        _frames = Frames.ToArray();
    }

    #endregion

    #region Work

    /// <summary>
    /// The <see cref="ISequenceTimeKeeper{TTime}"/> that keeps time for this <see cref="TimedSequence{TState, TTime}"/>
    /// </summary>
    public ISequenceTimeKeeper<TTime> TimeKeeper
    {
        get => _timeKeeper;
        protected set
        {
            ArgumentNullException.ThrowIfNull(value);
            _timeKeeper = value;
        }
    }
    private ISequenceTimeKeeper<TTime> _timeKeeper;

    /// <summary>
    /// The selector delegate that chooses this <see cref="TimedSequence{TState, TTime}"/>'s previous, next and current frame
    /// </summary>
    public FrameSelector Selector
    {
        get => _selector;
        protected set
        {
            ArgumentNullException.ThrowIfNull(value);
            _selector = value;
        }
    }
    private FrameSelector _selector;

    private Frame[] _frames;

    /// <summary>
    /// Represents an array of all the frames currently in this TimedSequence
    /// </summary>
    protected Frame[] Frames
    {
        get => _frames;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _frames = value;
        }
    }

    public void Update()
    {
        var tk = TimeKeeper;
        tk.Start();
        Updating();
        var jumps = tk.QueryAdvance<TState>(CurrentIndex, Frames);

        if (jumps-- > 0)
        {
            var (p, n, c) = Selector(this, CurrentIndex + jumps, Frames.Length);
            CurrentIndex = c;
            NextIndex = n;
            PreviousIndex = p;
        }
    }

    protected virtual void Updating() { }

    public int CurrentIndex { get; protected set; }
    public int NextIndex { get; private set; }
    public int PreviousIndex { get; private set; }
    public TState Current { get; private set; }
    public TState Next { get; private set; }
    public TState Previous { get; private set; }

    public TState First { get; }
    public TState Last { get; }

    private void UpdateIndices()
    {

    }

    #endregion

    #region IReadOnlyList

    /// <inheritdoc/>
    public TState this[int index] => Frames[index].State;

    /// <inheritdoc/>
    public virtual int Count => Frames.Length;

    /// <inheritdoc/>
    public virtual IEnumerator<TState> GetEnumerator()
    {
        var fr = Frames;
        for (int i = 0; i < fr.Length; i++)
            yield return fr[i].State;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion
}
