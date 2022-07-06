﻿using SDL2.NET;
using System.Numerics;
using VDStudios.MagicEngine.Internal;
using Veldrid;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents an operation that is ready to be drawn. This object is automatically disposed of when deregistered, and must have a reference to it held by the owning <see cref="IDrawableNode"/>. Otherwise, this object will be collected and disposed of
/// </summary>
/// <remarks>
/// Try to keep an object created from this class cached somewhere in a node, as they incur a number of allocations that should be avoided in a HotPath like the rendering sequence
/// </remarks>
public abstract class DrawOperation : InternalGraphicalOperation, IDisposable
{
    private readonly SemaphoreSlim sync = new(1, 1);

    internal CommandList? Commands;
    internal GraphicsDevice? Device;

    /// <summary>
    /// The owner <see cref="IDrawableNode"/> of this <see cref="DrawOperation"/>
    /// </summary>
    /// <remarks>
    /// Will be null if this <see cref="DrawOperation"/> is not registered
    /// </remarks>
    public IDrawableNode Owner { get; private set; }

    #region Registration

    #region Internal

    internal async ValueTask Register(IDrawableNode owner, GraphicsManager manager)
    {
        ThrowIfDisposed();

        Registering(owner, manager);
        
        var device = manager.Device;
        Device = device;
        Owner = owner;
        Manager = manager;
        await CreateResources(device, device.ResourceFactory);
        await InternalCreateWindowSizedResources(manager.ScreenSizeBuffer!);

        Commands = CreateCommandList(device, device.ResourceFactory);
        
        Registered();
    }

    internal async ValueTask InternalCreateWindowSizedResources(DeviceBuffer screenSizeBuffer)
    {
        sync.Wait();
        try
        {
            await CreateWindowSizedResources(Device!, Device!.ResourceFactory, screenSizeBuffer);
        }
        finally
        {
            sync.Release();
        }
    }

    #endregion

    #region Reaction Methods

    /// <summary>
    /// This method is called automatically when this <see cref="DrawOperation"/> is being registered onto <paramref name="manager"/>
    /// </summary>
    /// <param name="owner">The <see cref="Node"/> that registered this <see cref="DrawOperation"/></param>
    /// <param name="manager">The <see cref="GraphicsManager"/> this <see cref="DrawOperation"/> is being registered onto</param>
    protected virtual void Registering(IDrawableNode owner, GraphicsManager manager) { }

    /// <summary>
    /// This method is called automatically when this <see cref="DrawOperation"/> has been registered
    /// </summary>
    protected virtual void Registered() { }

    #endregion

    #endregion

    #region Drawing

    #region Internal

    private bool pendingGpuUpdate = true;

    /// <summary>
    /// Flags this <see cref="DrawOperation"/> as needing to update GPU data before the next <see cref="Draw(TimeSpan, Vector2, CommandList, GraphicsDevice, Framebuffer, DeviceBuffer)"/> call
    /// </summary>
    /// <remarks>
    /// Multiple calls to this method will not result in <see cref="UpdateGPUState(GraphicsDevice, CommandList, DeviceBuffer)"/> being called multiple times
    /// </remarks>
    protected void NotifyPendingGPUUpdate() => pendingGpuUpdate = true;

    internal async ValueTask<CommandList> InternalDraw(TimeSpan delta, Vector2 offset)
    {
        ThrowIfDisposed();
        sync.Wait();
        try
        {
            var commands = Commands!;
            var device = Device!;
            var ssb = Manager!.ScreenSizeBuffer!;

            if (pendingGpuUpdate)
            {
                pendingGpuUpdate = false;
                commands.Begin();
                await UpdateGPUState(device, commands, ssb);
                commands.End();
                device.SubmitCommands(commands);
            }
            commands.Begin();
            await Draw(delta, offset, commands, device, device.SwapchainFramebuffer, ssb).ConfigureAwait(false);
            commands.End();
            return commands;
        }
        finally
        {
            sync.Release();
        }
    }

    #endregion

    #region Reaction Methods

    ///// <summary>
    ///// If this property is <c>true</c>, then <see cref="CreateWindowSizedResources(GraphicsDevice, ResourceFactory, Vector4)"/> will be called inmediately upon <see cref="GraphicsManager.Window"/> being resized. This also means that it'll be executed from the VideoThread. Otherwise, if <c>false</c>, then <see cref="CreateWindowSizedResources(GraphicsDevice, ResourceFactory, Vector4)"/> will be called right before the next call to <see cref="Draw(Vector2, CommandList, GraphicsDevice, Framebuffer)"/>, from the owner <see cref="GraphicsManager"/>'s async loop
    ///// </summary>
    ///// <remarks>
    ///// Defaults to <c>true</c>
    ///// </remarks>
    //protected internal bool ReplaceWindowSizeResourcesInmediately { get; init; } = true;

    /// <summary>
    /// Creates the necessary resources for this <see cref="DrawOperation"/> that are buffer (Window) size dependant
    /// </summary>
    /// <param name="device">The Veldrid <see cref="GraphicsDevice"/> attached to the <see cref="GraphicsManager"/> this <see cref="DrawOperation"/> is registered on</param>
    /// <param name="factory"><paramref name="device"/>'s <see cref="ResourceFactory"/></param>
    /// <param name="screenSizeBuffer">A <see cref="DeviceBuffer"/> filled with a <see cref="Vector4"/> containing <see cref="GraphicsManager.Window"/>'s size in the form of <c>Vector4(x: Width, y: Height, 0, 0)</c></param>
    protected abstract ValueTask CreateWindowSizedResources(GraphicsDevice device, ResourceFactory factory, DeviceBuffer screenSizeBuffer);

    /// <summary>
    /// Creates the <see cref="CommandList"/> to be set as this <see cref="DrawOperation"/>'s designated <see cref="CommandList"/>
    /// </summary>
    /// <param name="device">The Veldrid <see cref="GraphicsDevice"/> attached to the <see cref="GraphicsManager"/> this <see cref="DrawOperation"/> is registered on</param>
    /// <param name="factory"><paramref name="device"/>'s <see cref="ResourceFactory"/></param>
    protected virtual CommandList CreateCommandList(GraphicsDevice device, ResourceFactory factory)
        => factory.CreateCommandList();

    /// <summary>
    /// Creates the necessary resources for this <see cref="DrawOperation"/>
    /// </summary>
    /// <param name="device">The Veldrid <see cref="GraphicsDevice"/> attached to the <see cref="GraphicsManager"/> this <see cref="DrawOperation"/> is registered on</param>
    /// <param name="factory"><paramref name="device"/>'s <see cref="ResourceFactory"/></param>
    protected abstract ValueTask CreateResources(GraphicsDevice device, ResourceFactory factory);

    /// <summary>
    /// The method that will be used to draw the component
    /// </summary>
    /// <remarks>
    /// Calling <see cref="ThrowIfDisposed()"/> or <see cref="Dispose(bool)"/> from this method WILL ALWAYS cause a deadlock! Remember that <paramref name="commandList"/> is *NOT* thread-safe, but it is owned solely by this <see cref="DrawOperation"/>; and <see cref="GraphicsManager"/> will not use it until this method returns.
    /// </remarks>
    /// <param name="delta">The amount of time that has passed since the last draw sequence</param>
    /// <param name="offset">The translation offset of the drawing operation</param>
    /// <param name="device">The Veldrid <see cref="GraphicsDevice"/> attached to the <see cref="GraphicsManager"/> this <see cref="DrawOperation"/> is registered on</param>
    /// <param name="commandList">The <see cref="CommandList"/> opened specifically for this call. <see cref="CommandList.End"/> will be called AFTER this method returns, so don't call it yourself</param>
    /// <param name="mainBuffer">The <see cref="GraphicsDevice"/> owned by this <see cref="GraphicsManager"/>'s main <see cref="Framebuffer"/>, to use with <see cref="CommandList.SetFramebuffer(Framebuffer)"/></param>
    /// <param name="screenSizeBuffer">A <see cref="DeviceBuffer"/> filled with a <see cref="Vector4"/> containing <see cref="GraphicsManager.Window"/>'s size in the form of <c>Vector4(x: Width, y: Height, 0, 0)</c></param>
    protected abstract ValueTask Draw(TimeSpan delta, Vector2 offset, CommandList commandList, GraphicsDevice device, Framebuffer mainBuffer, DeviceBuffer screenSizeBuffer);

    /// <summary>
    /// This method is called automatically when this <see cref="DrawOperation"/> is going to be drawn for the first time, and after <see cref="NotifyPendingGPUUpdate"/> is called. Whenever applicable, this method ALWAYS goes before <see cref="Draw(Vector2, CommandList, GraphicsDevice, Framebuffer)"/>
    /// </summary>
    /// <remarks>
    /// Calling <see cref="ThrowIfDisposed()"/> or <see cref="Dispose(bool)"/> from this method WILL ALWAYS cause a deadlock!
    /// </remarks>
    /// <param name="commandList">The <see cref="CommandList"/> opened specifically for this call. <see cref="CommandList.End"/> will be called AFTER this method returns, so don't call it yourself</param>
    /// <param name="device">The Veldrid <see cref="GraphicsDevice"/> attached to the <see cref="GraphicsManager"/> this <see cref="DrawOperation"/> is registered on</param>
    /// <param name="screenSizeBuffer">A <see cref="DeviceBuffer"/> filled with a <see cref="Vector4"/> containing <see cref="GraphicsManager.Window"/>'s size in the form of <c>Vector4(x: Width, y: Height, 0, 0)</c></param>
    protected abstract ValueTask UpdateGPUState(GraphicsDevice device, CommandList commandList, DeviceBuffer screenSizeBuffer);

    #endregion

    #endregion

    #region Disposal

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> if this <see cref="DrawOperation"/> has already been disposed
    /// </summary>
    /// <exception cref="ObjectDisposedException"></exception>
    /// <remarks>
    /// Calling this method from <see cref="Draw(Vector2, CommandList, GraphicsDevice)"/>, <see cref="UpdateGPUState"/> or <see cref="Dispose(bool)"/> WILL ALWAYS cause a deadlock!
    /// </remarks>
    protected void ThrowIfDisposed()
    {
        sync.Wait();
        try
        {
            if (disposedValue)
                throw new ObjectDisposedException(GetType().FullName);
        }
        finally
        {
            sync.Release();
        }
    }

    internal bool disposedValue;

    /// <summary>
    /// Disposes of this <see cref="DrawOperation"/>'s resources
    /// </summary>
    /// <remarks>
    /// Dispose of any additional resources your subtype allocates
    /// </remarks>
    protected virtual void Dispose(bool disposing) { }

    private void InternalDispose(bool disposing)
    {
        sync.Wait();
        try
        {
            lock (sync)
            {
                if (disposedValue)
                    return;
                disposedValue = true;
            }

            var @lock = Manager!.LockManagerDrawing();
            try
            {
                Dispose(disposing);
            }
            finally
            {
                Commands?.Dispose();
                Device = null;
                Commands = null;
                Owner = null;
                Manager = null;
                @lock.Dispose();
            }
        }
        finally
        {
            sync.Release();
        }
    }

    /// <inheritdoc/>
    ~DrawOperation()
    {
        InternalDispose(disposing: false);
    }

    /// <summary>
    /// Disposes of this <see cref="DrawOperation"/>'s resources
    /// </summary>
    /// <remarks>
    /// Calling this method from <see cref="Draw(Vector2, CommandList, GraphicsDevice)"/>, <see cref="UpdateGPUState"/> or <see cref="Dispose(bool)"/> WILL ALWAYS cause a deadlock!
    /// </remarks>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        InternalDispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
