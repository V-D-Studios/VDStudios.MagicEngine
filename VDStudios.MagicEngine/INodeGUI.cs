using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a <see cref="Node"/> that can be drawn into the GUI
/// </summary>
/// <remarks>
/// Due to ImGUI limitations, *ALL* GUI drawing is done synchronously -- Even <see cref="GraphicsManager"/>s are locked from eachother while drawing into ImGUI
/// </remarks>
public interface INodeGUI
{
    /// <summary>
    /// This method is called automatically when <see cref="HasPendingRegistrations"/> is true
    /// </summary>
    /// <remarks>
    /// It's highly recommended to set <see cref="HasPendingRegistrations"/> registrations to <c>false</c> in this method; otherwise it will be called every loop, possibly wasting time. This method will be called from the Update thread
    /// </remarks>
    /// <param name="main">Represents <see cref="Game.MainGraphicsManager"/></param>
    /// <param name="allManagers">A list of active <see cref="GraphicsManager"/> that operations can be registered onto</param>
    public void RegisterGUIOperations(GraphicsManager main, IReadOnlyList<GraphicsManager> allManagers);

    /// <summary>
    /// <c>false</c> if the GUI draw sequence should be propagated into this <see cref="Node"/>'s children, <c>true</c> otherwise. If this is <c>true</c>, GUI Draw handlers for children will also be skipped
    /// </summary>
    public bool SkipPropagation { get; }

    /// <summary>
    /// Set this to true when <see cref="RegisterGUIOperations(GraphicsManager, IReadOnlyList{GraphicsManager})"/> should be called
    /// </summary>
    /// <remarks>
    /// This property will *not* be set automatically for you. Manually set it to <c>true</c> when you want to register draw operations, and manually set it to <c>false</c> when you no longer want to.
    /// </remarks>
    public bool HasPendingRegistrations { get; }

    /// <summary>
    /// Adds an object representing the <see cref="Node"/>'s drawing operations into the <see cref="IDrawQueue{T}"/>
    /// </summary>
    /// <remarks>
    /// This method will be called from the respective <see cref="GraphicsManager"/>'s thread
    /// </remarks>
    /// <param name="queue">The queue associated with <paramref name="operation"/> into which to add the draw operations</param>
    /// <param name="operation">A specific registered <see cref="GUIDrawOperation"/></param>
    public void AddToDrawQueue(IDrawQueue<GUIDrawOperation> queue, GUIDrawOperation operation);
}
