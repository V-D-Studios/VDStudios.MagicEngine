namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a <see cref="Node"/> that can be drawn. 
/// </summary>
public interface IDrawableNode
{
    /// <summary>
    /// Adds an object representing the <see cref="Node"/>'s drawing operations into the <see cref="IDrawQueue"/>
    /// </summary>
    /// <param name="queue">The queue into which to add the draw operations</param>
    /// <returns>Whether the draw sequence should be propagated into this <see cref="Node"/>'s children. If this is false, Draw handlers for children will also be skipped</returns>
    public ValueTask<bool> AddToDrawQueue(IDrawQueue queue);
}
