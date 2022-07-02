using System.Collections.Immutable;
using Veldrid;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a <see cref="Node"/> that can be drawn. 
/// </summary>
public interface IDrawableNode
{
    /// <summary>
    /// This method is called automatically when <see cref="HasPendingRegistrations"/> is true
    /// </summary>
    /// <remarks>
    /// It's highly recommended to set <see cref="HasPendingRegistrations"/> registrations to <c>false</c> in this method; otherwise it will be called every loop, possibly wasting time. This method will be called from the Update thread
    /// </remarks>
    /// <param name="main">Represents <see cref="Game.MainGraphicsManager"/></param>
    /// <param name="allManagers">A list of active <see cref="GraphicsManager"/> that operations can be registered onto</param>
    public ValueTask RegisterDrawOperations(GraphicsManager main, IReadOnlyList<GraphicsManager> allManagers);

    /// <summary>
    /// <c>false</c> if the draw sequence should be propagated into this <see cref="Node"/>'s children, <c>true</c> otherwise. If this is <c>true</c>, Draw handlers for children will also be skipped
    /// </summary>
    public bool SkipPropagation { get; }

    /// <summary>
    /// Set this to true when <see cref="RegisterDrawOperations(GraphicsManager, IReadOnlyList{GraphicsManager})"/> should be called
    /// </summary>
    /// <remarks>
    /// This property will *not* be set automatically for you. Manually set it to <c>true</c> when you want to register draw operations, and manually set it to <c>false</c> when you no longer want to.
    /// </remarks>
    public bool HasPendingRegistrations { get; }

    /// <summary>
    /// Adds an object representing the <see cref="Node"/>'s drawing operations into the <see cref="IDrawQueue{TOp}"/>
    /// </summary>
    /// <remarks>
    /// This method will be called from the respective <see cref="GraphicsManager"/>'s thread
    /// </remarks>
    /// <param name="queue">The queue associated with <paramref name="operation"/> into which to add the draw operations</param>
    /// <param name="operation">A specific registered <see cref="DrawOperation"/></param>
    public void AddToDrawQueue(IDrawQueue<DrawOperation> queue, DrawOperation operation);
}
