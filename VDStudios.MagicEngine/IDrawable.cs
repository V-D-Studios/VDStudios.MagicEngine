namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a <see cref="Node"/> that can be drawn. <see cref="IDrawableAsync"/> takes precedence if both are implemented
/// </summary>
public interface IDrawable
{
    /// <summary>
    /// Adds an object representing the <see cref="Node"/>'s drawing operations into the <see cref="IDrawQueue"/>
    /// </summary>
    /// <param name="queue">The queue into which to add the draw operations</param>
    public void AddToDrawQueue(IDrawQueue queue);
}
