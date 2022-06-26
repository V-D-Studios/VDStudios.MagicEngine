using SDL2.NET;
using System.Numerics;
using Veldrid;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents an operation that is ready to be drawn
/// </summary>
/// <remarks>
/// Try to keep an object created from this class cached somewhere in a node, as they incur a number of allocations that should be avoided in a HotPath like the rendering sequence
/// </remarks>
public abstract class DrawOperation
{
    protected DrawOperation(GraphicsDevice device)
    {
        Device = device;
        Commands = device.ResourceFactory.CreateCommandList();
    }

    internal CommandList Commands;
    internal GraphicsDevice Device;

    internal void InternalDraw(Vector2 offset, GraphicsDevice device)
    {
        if (!ReferenceEquals(device, Device))
            throw new InvalidOperationException($"Can't perform a Draw operation on a different device than this operation was created with");
        Commands.Begin();
        Draw(offset, Commands, device);
        Commands.End();
    }

    /// <summary>
    /// The method that will be used to draw the component
    /// </summary>
    /// <param name="offset">The translation offset of the drawing operation</param>
    /// <param name="device">The Veldrid <see cref="GraphicsDevice"/></param>
    /// <param name="commandList">The <see cref="CommandList"/> opened specifically for this call. <see cref="CommandList.End"/> will be called AFTER this method returns, so don't call it yourself</param>
    protected abstract void Draw(Vector2 offset, CommandList commandList, GraphicsDevice device);

    /// <summary>
    /// This method is called automatically when this <see cref="DrawOperation"/> is going to be drawn for the first time
    /// </summary>
    protected internal abstract void Start();

#error Implement DrawOperation for Veldrid
}
