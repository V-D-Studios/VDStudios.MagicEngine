namespace VDStudios.MagicEngine.Animation;

/// <summary>
/// Represents a Time Keeper for a <see cref="TimedSequence{TState, TTime}"/>; rules over the measure of time used to advance the sequence
/// </summary>
public interface ISequenceTimeKeeper<TTime> where TTime : struct
{
    /// <summary>
    /// Queries this keeper about how many frames it should advance
    /// </summary>
    /// <remarks>
    /// If the value returned by this method is greater than 0, the keeper will automatically know to focus on the upcoming frames
    /// </remarks>
    /// <param name="currentState">The current state of the sequence</param>
    /// <param name="frames">The frames in the sequence</param>
    /// <typeparam name="TState">The type of the state used by a sequence</typeparam>
    /// <returns>The amount of frames to advance the sequence for</returns>
    public int QueryAdvance<TState>(int currentState, TimedSequence<TState, TTime>.Frame[] frames) where TState : notnull;

    /// <summary>
    /// Stops keeping time
    /// </summary>
    public void Stop();

    /// <summary>
    /// Starts keeping time
    /// </summary>
    public void Start();

    /// <summary>
    /// Resets the keeper back to its default state
    /// </summary>
    public void Reset();

    /// <summary>
    /// Resets the keeper back to its default state and starts it
    /// </summary>
    public void Restart();
}