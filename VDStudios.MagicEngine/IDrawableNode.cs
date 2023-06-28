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
    /// An optional name for debugging purposes
    /// </summary>
    public string? Name { get; }
}
