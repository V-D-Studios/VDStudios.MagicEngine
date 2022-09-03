namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a <see cref="Node"/> that can be drawn. 
/// </summary>
public interface IDrawableNode
{
    /// <summary>
    /// Represents the class that manages this <see cref="IDrawableNode"/>'s <see cref="DrawOperation"/>s
    /// </summary>
    public DrawOperationManager DrawOperationManager { get; }

    /// <summary>
    /// <c>false</c> if the draw sequence should be propagated into this <see cref="Node"/>'s children, <c>true</c> otherwise. If this is <c>true</c>, Draw handlers for children will also be skipped
    /// </summary>
    public bool SkipDrawPropagation { get; }

    /// <summary>
    /// An optional name for debugging purposes
    /// </summary>
    public string? Name { get; }
}
