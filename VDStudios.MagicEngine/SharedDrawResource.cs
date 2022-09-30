using SDL2.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;
using Vulkan;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a resource that can be shared across multiple <see cref="DrawOperation"/>s, and as such should be updated independently
/// </summary>
public abstract class SharedDrawResource : GraphicsObject
{
    private readonly object sync = new();

    /// <summary>
    /// Instances a new <see cref="SharedDrawResource"/> object
    /// </summary>
    public SharedDrawResource() : base("resources") 
    {
    }

    /// <summary>
    /// Flags this <see cref="SharedDrawResource"/> as needing to update GPU data the next frame
    /// </summary>
    /// <remarks>
    /// Multiple calls to this method will *not* result in this <see cref="SharedDrawResource"/> being updated multiple times
    /// </remarks>
    public void NotifyPendingUpdate() => PendingGpuUpdate = true;
    internal bool PendingGpuUpdate { get; private set; }

    /// <summary>
    /// Updates the data relevant to this <see cref="SharedDrawResource"/>
    /// </summary>
    /// <remarks>
    /// This method is called automatically after <see cref="GraphicsObject.Manager"/> queries for changes, which happens if <see cref="NotifyPendingUpdate"/> has been called before that frame
    /// </remarks>
    /// <param name="manager"><see cref="GraphicsObject.Manager"/></param>
    /// <param name="device">The <see cref="GraphicsDevice"/> controlled by <paramref name="manager"/></param>
    /// <param name="commandList">The <see cref="CommandList"/> assigned to this SharedDrawResource</param>
    public abstract ValueTask Update(GraphicsManager manager, GraphicsDevice device, CommandList commandList);

    internal ValueTask InternalUpdate(CommandList cl)
    {
        ThrowIfDisposed();

        if (PendingGpuUpdate)
            lock (sync)
                if (PendingGpuUpdate)
                    PendingGpuUpdate = false;
                else return ValueTask.CompletedTask;

        return Update(Manager!, Manager!.Device!, cl);
    }

    #region Registration

    internal void ThrowIfAlreadyRegistered()
    {
        lock (sync)
            if (Manager is not null)
                throw new InvalidOperationException("This SharedDrawResource is already registered on a GraphicsManager");
    }

    internal async ValueTask Register(GraphicsManager manager)
    {
        ThrowIfDisposed();
        VerifyManager(manager);
        Registering(manager);

        var device = manager.Device;
        var factory = device.ResourceFactory;

        await CreateResources(device, factory);

        Registered();
        NotifyIsReady();
    }

    /// <summary>
    /// Creates the necessary resources for this <see cref="SharedDrawResource"/>
    /// </summary>
    /// <param name="device">The Veldrid <see cref="GraphicsDevice"/> attached to the <see cref="GraphicsManager"/> this <see cref="DrawOperation"/> is registered on</param>
    /// <param name="factory"><paramref name="device"/>'s <see cref="ResourceFactory"/></param>
    protected abstract ValueTask CreateResources(GraphicsDevice device, ResourceFactory factory);

    /// <summary>
    /// This method is called automatically when this <see cref="DrawOperation"/> is being registered onto <paramref name="manager"/>
    /// </summary>
    /// <param name="manager">The <see cref="GraphicsManager"/> this <see cref="DrawOperation"/> is being registered onto</param>
    protected virtual void Registering(GraphicsManager manager) { }

    /// <summary>
    /// This method is called automatically when this <see cref="DrawOperation"/> has been registered
    /// </summary>
    protected virtual void Registered() { }

    #endregion

    #region Disposal

    internal bool disposedValue;

    /// <summary>
    /// Disposes of this <see cref="DrawOperation"/>'s resources
    /// </summary>
    /// <remarks>
    /// Dispose of any additional resources your subtype allocates. Consider that this method may be called even if <see cref="GraphicsObject.Manager"/> is not set
    /// </remarks>
    protected virtual void Dispose(bool disposing) { }

    private void InternalDispose(bool disposing)
    {
        AboutToDispose?.Invoke(this, Game.TotalTime);
        lock (sync) 
            if (Manager is GraphicsManager manager)
            {
                var @lock = manager.LockManagerDrawing();
                try
                {
                    Dispose(disposing);
                }
                finally
                {
                    @lock.Dispose();
                }
            }
    }

    /// <summary>
    /// Throws a new <see cref="ObjectDisposedException"/> if this <see cref="SharedDrawResource"/> is already disposed
    /// </summary>
    protected void ThrowIfDisposed()
    {
        if (disposedValue)
            throw new ObjectDisposedException(GetType().FullName);
    }

    /// <inheritdoc/>
    ~SharedDrawResource()
    {
        InternalDispose(disposing: false);
    }

    /// <summary>
    /// Disposes of this <see cref="SharedDrawResource"/>'s resources
    /// </summary>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        InternalDispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Fired right before this <see cref="SharedDrawResource"/> is disposed
    /// </summary>
    /// <remarks>
    /// While .NET allows fire-and-forget async methods in these events (<c>async void</c>), this is *NOT* recommended, as it's almost guaranteed the <see cref="SharedDrawResource"/> will be fully disposed before the async portion of your code gets a chance to run
    /// </remarks>
    public event GeneralGameEvent<SharedDrawResource>? AboutToDispose;

    #endregion
}
