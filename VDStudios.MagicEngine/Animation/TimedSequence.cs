using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
/// Represents an event in which a <see cref="TimedSequence{TState, TTime}"/> advanced frames
/// </summary>
/// <typeparam name="TState">The type of the state that <paramref name="caller"/> will have at any given frame</typeparam>
/// <typeparam name="TTime">The type of the object representing time</typeparam>
/// <param name="caller">The object that fired this event</param>
/// <param name="current">The current state of <paramref name="caller"/></param>
/// <param name="next">The next state <paramref name="caller"/> will assume</param>
/// <param name="previous">The previous state <paramref name="caller"/> had</param>
public delegate void TimedSequenceAdvancementEvent<TState, TTime>(TimedSequence<TState, TTime> caller, TState current, TState next, TState previous)
    where TState : notnull 
    where TTime : struct;

/// <summary>
/// Represents a sequence of <typeparamref name="TState"/> states that moves across time
/// </summary>
/// <typeparam name="TState">The type of the state this <see cref="TimedSequence{TState, TTime}"/> will have at any given frame</typeparam>
/// <typeparam name="TTime">The type of the object representing time</typeparam>
public class TimedSequence<TState, TTime> : IReadOnlyList<TState>, IReadOnlyList<TimedSequence<TState, TTime>.Frame> 
    where TState : notnull 
    where TTime : struct
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
    /// This event is fired whenever this <see cref="TimedSequence{TState, TTime}"/> advances its frames
    /// </summary>
    public TimedSequenceAdvancementEvent<TState, TTime>? FrameAdvanced;

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

    /// <summary>
    /// Updates this <see cref="TimedSequence{TState, TTime}"/>, performing a frame advancement check and selecting its new frames if appropriate
    /// </summary>
    public void Update()
    {
        var tk = TimeKeeper;
        tk.Start();
        Updating();
        var jumps = tk.QueryAdvance<TState>(CurrentIndex, Frames);

        if (jumps-- > 0)
            UpdateIndices(Selector(this, CurrentIndex + jumps, Frames.Length));

        Updated(jumps > 0);
    }

    /// <summary>
    /// This method is called automatically when this <see cref="TimedSequence{TState, TTime}"/> is about to be updated
    /// </summary>
    protected virtual void Updating() { }

    /// <summary>
    /// This method is called automatically when this <see cref="TimedSequence{TState, TTime}"/> has been updated
    /// </summary>
    /// <param name="updatedIndices">Whether the object's indices and current states were updated</param>
    protected virtual void Updated(bool updatedIndices) { }

    /// <summary>
    /// The current frame index of this <see cref="TimedSequence{TState, TTime}"/>
    /// </summary>
    public int CurrentIndex { get; private set; }

    /// <summary>
    /// The next frame index of this <see cref="TimedSequence{TState, TTime}"/>
    /// </summary>
    public int NextIndex { get; private set; }

    /// <summary>
    /// The previous frame index of this <see cref="TimedSequence{TState, TTime}"/>
    /// </summary>
    public int PreviousIndex { get; private set; }

    /// <summary>
    /// The current state of this <see cref="TimedSequence{TState, TTime}"/>
    /// </summary>
    public TState Current { get; private set; }

    /// <summary>
    /// The next state this <see cref="TimedSequence{TState, TTime}"/> will assume
    /// </summary>
    public TState Next { get; private set; }

    /// <summary>
    /// The previous state this <see cref="TimedSequence{TState, TTime}"/> had
    /// </summary>
    public TState Previous { get; private set; }

    /// <summary>
    /// The actual previous state this <see cref="TimedSequence{TState, TTime}"/> had
    /// </summary>
    /// <remarks>
    /// Should remain consistently equal to <see cref="Previous"/>, but it may differ from timing errors or intentional advancement jumping
    /// </remarks>
    public TState LastState { get; private set; }

    /// <summary>
    /// The actual previous state index this <see cref="TimedSequence{TState, TTime}"/> had
    /// </summary>
    /// <remarks>
    /// Should remain consistently equal to <see cref="PreviousIndex"/>, but it may differ from timing errors or intentional advancement jumping
    /// </remarks>
    public int LastStateIndex { get; private set; }

    private void UpdateIndices((int p, int n, int c) indices)
    {
        var fr = Frames;
        var (prev, next, curr) = indices;

        LastState = Current;
        LastStateIndex = CurrentIndex;

        CurrentIndex = curr;
        PreviousIndex = prev;
        NextIndex = next;

        Current = fr[curr].State;
        Next = fr[next].State;
        Previous = fr[prev].State;

        FrameAdvanced?.Invoke(this, Current, Next, Previous);
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

    /// <inheritdoc/>
    Frame IReadOnlyList<Frame>.this[int index] => GetFrame(index);

    /// <summary>
    /// Queries the frame at index <paramref name="index"/> in this <see cref="TimedSequence{TState, TTime}"/>
    /// </summary>
    /// <param name="index">The index at which to query the frame</param>
    /// <returns>The found frame</returns>
    public Frame GetFrame(int index) => Frames[index];

    /// <summary>
    /// Gets an enumerable representing this <see cref="TimedSequence{TState, TTime}"/>'s frames
    /// </summary>
    /// <remarks>
    /// This method returns the <see cref="TimedSequence{TState, TTime}"/> itself
    /// </remarks>
    /// <returns>An <see cref="IEnumerable{T}"/> that can be used to enumerate this <see cref="TimedSequence{TState, TTime}"/>'s frames</returns>
    public IEnumerable<Frame> GetFrames() => this;

    /// <inheritdoc/>
    IEnumerator<Frame> IEnumerable<Frame>.GetEnumerator()
    {
        var fr = Frames;
        for (int i = 0; i < fr.Length; i++)
            yield return fr[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion
}
