﻿namespace VDStudios.MagicEngine;

/// <summary>
/// Represents the base class for graphical operations, such as <see cref="DrawOperation"/>, <see cref="GUIElement"/>
/// </summary>
/// <remarks>
/// This class cannot be instanced or inherited by user code
/// </remarks>
public abstract class GraphicsObject : GameObject
{
    internal GraphicsObject(string facility) : base(facility, "Rendering")
    { }

    /// <summary>
    /// This <see cref="DrawOperation"/>'s unique identifier, generated automatically
    /// </summary>
    public Guid Identifier { get; } = Guid.NewGuid();

    /// <summary>
    /// The <see cref="GraphicsManager"/> this operation is registered onto
    /// </summary>
    /// <remarks>
    /// Will be null if this operation is not registered
    /// </remarks>
    public GraphicsManager? Manager { get; internal set; }
}
