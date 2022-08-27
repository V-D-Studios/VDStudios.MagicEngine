using System.Numerics;
using VDStudios.MagicEngine.DrawLibrary;
using Veldrid;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents an operation that is ready to be drawn. This object is automatically disposed of when deregistered, and must have a reference to it held by the owning <see cref="IDrawableNode"/>. Otherwise, this object will be collected and disposed of
/// </summary>
/// <remarks>
/// Try to keep an object created from this class cached somewhere in a node, as they incur a number of allocations that should be avoided in a HotPath like the rendering sequence
/// </remarks>
public abstract class DrawOperation : GraphicsObject, IDisposable
{
    private readonly SemaphoreSlim sync = new(1, 1);

    internal CommandList? Commands;
    internal GraphicsDevice? Device;

    private static ResourceLayoutBuilder ResourceLayoutFactory() => ResourceLayoutBuilderPool.Rent();
    private static void ResourceLayoutCleaner(ResourceLayoutBuilder resource) => ResourceLayoutBuilderPool.Return(resource);

    private static readonly ObjectPool<ResourceSetBuilder> ResourceSetBuilderPool = new(x => new ResourceSetBuilder(ResourceLayoutFactory, ResourceLayoutCleaner), 2, 5);
    private static readonly ObjectPool<ResourceLayoutBuilder> ResourceLayoutBuilderPool = new(x => new ResourceLayoutBuilder(), 2, 5);

    /// <summary>
    /// Instances a new object of type <see cref="DrawOperation"/>
    /// </summary>
    public DrawOperation() : base("Drawing")
    {
    }

    /// <summary>
    /// Represents this <see cref="DrawOperation"/>'s preferred priority. May or may not be honored depending on the <see cref="DrawOperationManager"/>
    /// </summary>
    public float PreferredPriority { get; set; }

    /// <summary>
    /// Represents the current reference to <see cref="DrawParameters"/> this <see cref="DrawOperation"/> has
    /// </summary>
    /// <remarks>
    /// Rather than change this manually, it's better to let the owner of this <see cref="DrawOperation"/> assign it in the next cascade assignment
    /// </remarks>
    public DrawParameters? ReferenceParameters { get; set; }

    /// <summary>
    /// Returns either <see cref="ReferenceParameters"/> or <see cref="GraphicsManager.DrawParameters"/> if the former is <c>null</c>
    /// </summary>
    public DrawParameters Parameters => ReferenceParameters ?? Manager!.DrawParameters;

    /// <summary>
    /// The owner <see cref="DrawOperationManager"/> of this <see cref="DrawOperation"/>
    /// </summary>
    /// <remarks>
    /// Will throw if this <see cref="DrawOperation"/> is not registered
    /// </remarks>
    public DrawOperationManager Owner => _owner ?? throw new InvalidOperationException("Cannot query the Owner of an unregistered DrawOperation");
    private DrawOperationManager? _owner;
    internal void SetOwner(DrawOperationManager owner)
    {
        lock (sync)
        {
            if (_owner is not null)
                throw new InvalidOperationException("This DrawOperation already has an owner.");
            _owner = owner;
        }
    }

    #region Registration

    #region Internal

    internal async ValueTask Register(GraphicsManager manager)
    {
        ThrowIfDisposed();

        Registering(manager);
        
        var device = manager.Device;
        var factory = device.ResourceFactory;
        Device = device;
        Manager = manager;

        var resb = ResourceSetBuilderPool.Rent();
        try
        {
            await CreateResourceSets(device, resb, factory);
            resb.Build(out var sets, out var layouts, factory);
            await CreateResources(device, factory, sets, layouts);
            await InternalCreateWindowSizedResources(manager.ScreenSizeBuffer!);
        }
        finally
        {
            ResourceSetBuilderPool.Return(resb);
        }

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
    protected virtual void Registering(GraphicsManager manager) { }

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
    /// Flags this <see cref="DrawOperation"/> as needing to update GPU data before the next <see cref="Draw(TimeSpan, CommandList, GraphicsDevice, Framebuffer, DeviceBuffer)"/> call
    /// </summary>
    /// <remarks>
    /// Multiple calls to this method will not result in <see cref="UpdateGPUState(GraphicsDevice, CommandList, DeviceBuffer)"/> being called multiple times
    /// </remarks>
    protected void NotifyPendingGPUUpdate() => pendingGpuUpdate = true;

    internal async ValueTask<CommandList> InternalDraw(TimeSpan delta)
    {
        ThrowIfDisposed();
        sync.Wait();
        try
        {
            var commands = Commands!;
            var device = Device!;
            var ssb = Manager!.ScreenSizeBuffer!;

            commands.Begin();
            try
            {
                if (pendingGpuUpdate)
                {
                    pendingGpuUpdate = false;
                    await UpdateGPUState(device, commands, ssb);
                }
                await Draw(delta, commands, device, device.SwapchainFramebuffer, ssb).ConfigureAwait(false);
            }
            finally
            {
                commands.End();
            }
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
    /// Creates the necessary resource sets for this <see cref="DrawOperation"/>
    /// </summary>
    /// <remarks>
    /// This method is called before <see cref="CreateResources(GraphicsDevice, ResourceFactory, ResourceSet[], ResourceLayout[])"/>
    /// </remarks>
    /// <param name="factory"><paramref name="device"/>'s <see cref="ResourceFactory"/></param>
    /// <param name="builder">The collection of descriptions that will be used to build the resource sets for this <see cref="DrawOperation"/>. This object is borrowed from a pool and will be cleared and returned after this method returns</param>
    /// <param name="device">The Veldrid <see cref="GraphicsDevice"/> attached to the <see cref="GraphicsManager"/> this <see cref="DrawOperation"/> is registered on</param>
    protected virtual ValueTask CreateResourceSets(GraphicsDevice device, ResourceSetBuilder builder, ResourceFactory factory) => ValueTask.CompletedTask;

    /// <summary>
    /// Creates the necessary resources for this <see cref="DrawOperation"/>
    /// </summary>
    /// <remarks>
    /// This method is called after <see cref="CreateResourceSets(GraphicsDevice, ResourceSetBuilder, ResourceFactory)"/>
    /// </remarks>
    /// <param name="device">The Veldrid <see cref="GraphicsDevice"/> attached to the <see cref="GraphicsManager"/> this <see cref="DrawOperation"/> is registered on</param>
    /// <param name="factory"><paramref name="device"/>'s <see cref="ResourceFactory"/></param>
    /// <param name="resourceSets">The resource sets generated by <see cref="CreateResourceSets(GraphicsDevice, ResourceSetBuilder, ResourceFactory)"/></param>
    /// <param name="resourceLayouts">The resource layouts generated by <see cref="CreateResourceSets(GraphicsDevice, ResourceSetBuilder, ResourceFactory)"/></param>
    protected abstract ValueTask CreateResources(GraphicsDevice device, ResourceFactory factory, ResourceSet[]? resourceSets, ResourceLayout[]? resourceLayouts);

    /// <summary>
    /// The method that will be used to draw the component
    /// </summary>
    /// <remarks>
    /// Calling <see cref="ThrowIfDisposed()"/> or <see cref="Dispose(bool)"/> from this method WILL ALWAYS cause a deadlock! Remember that <paramref name="commandList"/> is *NOT* thread-safe, but it is owned solely by this <see cref="DrawOperation"/>; and <see cref="GraphicsManager"/> will not use it until this method returns.
    /// </remarks>
    /// <param name="delta">The amount of time that has passed since the last draw sequence</param>
    /// <param name="device">The Veldrid <see cref="GraphicsDevice"/> attached to the <see cref="GraphicsManager"/> this <see cref="DrawOperation"/> is registered on</param>
    /// <param name="commandList">The <see cref="CommandList"/> opened specifically for this call. <see cref="CommandList.End"/> will be called AFTER this method returns, so don't call it yourself</param>
    /// <param name="mainBuffer">The <see cref="GraphicsDevice"/> owned by this <see cref="GraphicsManager"/>'s main <see cref="Framebuffer"/>, to use with <see cref="CommandList.SetFramebuffer(Framebuffer)"/></param>
    /// <param name="screenSizeBuffer">A <see cref="DeviceBuffer"/> filled with a <see cref="Vector4"/> containing <see cref="GraphicsManager.Window"/>'s size in the form of <c>Vector4(x: Width, y: Height, 0, 0)</c></param>
    protected abstract ValueTask Draw(TimeSpan delta, CommandList commandList, GraphicsDevice device, Framebuffer mainBuffer, DeviceBuffer screenSizeBuffer);

    /// <summary>
    /// This method is called automatically when this <see cref="DrawOperation"/> is going to be drawn for the first time, and after <see cref="NotifyPendingGPUUpdate"/> is called. Whenever applicable, this method ALWAYS goes before <see cref="Draw(TimeSpan, CommandList, GraphicsDevice, Framebuffer, DeviceBuffer)"/>
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
    /// Calling this method from <see cref="Draw(TimeSpan, CommandList, GraphicsDevice, Framebuffer, DeviceBuffer)"/>, <see cref="UpdateGPUState"/> or <see cref="Dispose(bool)"/> WILL ALWAYS cause a deadlock!
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
        AboutToDispose?.Invoke(this, Game.TotalTime);
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
                _owner = null!;
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
    /// Calling this method from <see cref="Draw(TimeSpan, CommandList, GraphicsDevice, Framebuffer, DeviceBuffer)"/>, <see cref="UpdateGPUState"/> or <see cref="Dispose(bool)"/> WILL ALWAYS cause a deadlock!
    /// </remarks>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        InternalDispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Fired right before this <see cref="DrawOperation"/> is disposed
    /// </summary>
    /// <remarks>
    /// While .NET allows fire-and-forget async methods in these events (<c>async void</c>), this is *NOT* recommended, as it's almost guaranteed the <see cref="DrawOperation"/> will be fully disposed before the async portion of your code gets a chance to run
    /// </remarks>
    public event GeneralGameEvent<DrawOperation>? AboutToDispose;

    #endregion
}
