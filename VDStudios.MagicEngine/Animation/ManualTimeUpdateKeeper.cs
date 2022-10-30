using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDStudios.MagicEngine.GUILibrary.ImGUI;

namespace VDStudios.MagicEngine.Animation;

/// <summary>
/// Represents an <see cref="ISequenceTimeKeeper{TTime}"/> whose time reference is updated manually
/// </summary>
public class ManualTimeUpdateKeeper : ISequenceTimeKeeper<TimeSpan>
{
    private long ticks;
    private bool started;

    /// <summary>
    /// The amount of time each frame can be allocated
    /// </summary>
    public TimeSpan TimePerFrame { get; set; }

    private TimeSpan timePerFrame;
    private TimeSpan correctionOffset;

    /// <summary>
    /// Constructs a new object of type <see cref="ManualTimeUpdateKeeper"/>
    /// </summary>
    /// <param name="timePerFrame">The amount of time each frame can be allocated</param>
    public ManualTimeUpdateKeeper(TimeSpan timePerFrame)
    {
        this.timePerFrame = TimePerFrame = timePerFrame;
    }

    /// <inheritdoc/>
    public int QueryAdvance<TState>(int currentState, TimedSequence<TState, TimeSpan>.Frame[] frames) where TState : notnull
    {
        if (started is false) return 0;
        var elapsed = new TimeSpan(ticks);
        var t = timePerFrame + frames[currentState].Offset + correctionOffset;
        int advances = 0;
        if (elapsed > t)
        {
            do
            {
                advances++;
                t += timePerFrame + frames[(currentState + 1) % frames.Length].Offset;
            } while (elapsed > t);

            Restart();
            correctionOffset = t - elapsed;
            timePerFrame = TimePerFrame;
        }

        return advances;
    }

    /// <inheritdoc/>
    public void Add(TimeSpan time) => Interlocked.Add(ref ticks, time.Ticks);

    /// <inheritdoc/>
    public void Stop() => started = false;

    /// <inheritdoc/>
    public void Start() => started = true;

    /// <inheritdoc/>
    public void Reset()
    {
        ticks = 0;
        started = false;
    }

    /// <inheritdoc/>
    public void Restart()
    {
        ticks = 0;
        started = true;
    }
}
