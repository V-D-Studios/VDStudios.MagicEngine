using VDStudios.MagicEngine.GUILibrary.ImGUI;

namespace VDStudios.MagicEngine.Animation;

/// <summary>
/// An <see cref="ISequenceTimeKeeper{TTime}"/> that keeps time in respect to the manual stepping of itself
/// </summary>
public class ManualStepSequenceTimeKeeper : ISequenceTimeKeeper<int>
{
    private int steps;
    private bool started;

    /// <inheritdoc/>
    public int QueryAdvance<TState>(int currentState, TimedSequence<TState, int>.Frame[] frames) where TState : notnull
    {
        if (!started) return 0;
        var s = steps;
        var o = frames[currentState].Offset;
        int advances = 0;
        if (s > o)
        {
            do
            {
                advances++;
                o = frames[(currentState + 1) % frames.Length].Offset;
            } while (--s > o);

            steps = 0;
        }

        return advances;
    }

    /// <inheritdoc/>
    public void Step() => Interlocked.Increment(ref steps);

    /// <inheritdoc/>
    public void Stop() => started = false;

    /// <inheritdoc/>
    public void Start() => started = true;

    /// <inheritdoc/>
    public void Reset()
    {
        steps = 0;
        started = false;
    }

    /// <inheritdoc/>
    public void Restart()
    {
        steps = 0;
        started = true;
    }
}
