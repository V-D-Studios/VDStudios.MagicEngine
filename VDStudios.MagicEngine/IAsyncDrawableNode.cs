namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a <see cref="Node"/> that can be drawn and can be added into the Draw Queue asynchronously. Takes precedence over <see cref="IDrawableNode"/> if both are implemented
/// </summary>
/// <remarks>
/// The actual draw operation is synchronous and is vital it remains so.
/// </remarks>
public interface IAsyncDrawableNode
{
    /// <summary>
    /// Asynchronously adds an object representing the <see cref="Node"/>'s drawing operations into the <see cref="IDrawQueue"/>
    /// </summary>
    /// <param name="queue">The queue into which to add the draw operations</param>
    public Task AddToDrawQueueAsync(IDrawQueue queue);
}