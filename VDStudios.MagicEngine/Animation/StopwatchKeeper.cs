using System.Diagnostics;

namespace VDStudios.MagicEngine.Animation;

/// <summary>
/// An <see cref="ISequenceTimeKeeper{TTime}"/> that keeps time using a <see cref="Stopwatch"/>
/// </summary>
public class StopwatchSequenceTimeKeeper : ISequenceTimeKeeper<TimeSpan>
{
    private readonly Stopwatch Watch = new();

    /// <summary>
    /// The amount of time each frame can be allocated
    /// </summary>
    public TimeSpan TimePerFrame { get; set; }

    private TimeSpan timePerFrame;
    private TimeSpan correctionOffset;

    /// <summary>
    /// Constructs a new object of type <see cref="StopwatchSequenceTimeKeeper"/>
    /// </summary>
    /// <param name="timePerFrame">The amount of time each frame can be allocated</param>
    public StopwatchSequenceTimeKeeper(TimeSpan timePerFrame)
    {
        this.timePerFrame = TimePerFrame = timePerFrame;
    }

    /// <inheritdoc/>
    public int QueryAdvance<TState>(int currentState, TimedSequence<TState, TimeSpan>.Frame[] frames) where TState : notnull
    {
        var elapsed = Watch.Elapsed;
        var t = timePerFrame + frames[currentState].Offset + correctionOffset;
        int advances = 0;
        if (elapsed > t)
        {
            do
            {
                advances++;
                t += timePerFrame + frames[(currentState + 1) % frames.Length].Offset;
            } while (elapsed > t);

            Watch.Restart();
            correctionOffset = t - elapsed;
            timePerFrame = TimePerFrame;
        }

        return advances;
    }

    /// <inheritdoc/>
    public void Stop()
        => Watch.Stop();

    /// <inheritdoc/>
    public void Start()
        => Watch.Start();

    /// <inheritdoc/>
    public void Reset()
        => Watch.Reset();

    /// <inheritdoc/>
    public void Restart()
        => Watch.Restart();
}