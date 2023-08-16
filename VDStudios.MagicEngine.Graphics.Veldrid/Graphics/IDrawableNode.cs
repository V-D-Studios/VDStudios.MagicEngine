﻿namespace VDStudios.MagicEngine.Graphics.Veldrid.Graphics;

/// <summary>
/// Represents a <see cref="Node"/> that can be drawn. 
/// </summary>
public interface IDrawableNode
{
    /// <summary>
    /// Represents the class that manages this <see cref="IDrawableNode"/>'s <see cref="VeldridDrawOperation"/>s
    /// </summary>
    public DrawOperationManager DrawOperationManager { get; }

    /// <summary>
    /// An optional name for debugging purposes
    /// </summary>
    public string? Name { get; }
}