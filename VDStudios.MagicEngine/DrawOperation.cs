using SDL2.NET;
using System.Numerics;
using Veldrid;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents an operation that is ready to be drawn. This object is automatically disposed of when deregistered, and must have a reference to it held by the owning <see cref="IDrawableNode"/>. Otherwise, this object will be collected and disposed of
/// </summary>
/// <remarks>
/// Try to keep an object created from this class cached somewhere in a node, as they incur a number of allocations that should be avoided in a HotPath like the rendering sequence
/// </remarks>
public abstract class DrawOperation : IDisposable
{
    private readonly object sync = new();
    internal CommandList? Commands;
    internal GraphicsDevice? Device;

    /// <summary>
    /// The owner <see cref="IDrawableNode"/> of this <see cref="DrawOperation"/>
    /// </summary>
    /// <remarks>
    /// Will be null if this <see cref="DrawOperation"/> is not registered
    /// </remarks>
    public IDrawableNode Owner { get; private set; }

    /// <summary>
    /// This <see cref="DrawOperation"/>'s unique identifier, generated automatically
    /// </summary>
    public Guid Identifier { get; } = Guid.NewGuid();

    #region Registration

    #region Properties

    /// <summary>
    /// The <see cref="GraphicsManager"/> this <see cref="DrawOperation"/> is registered onto
    /// </summary>
    /// <remarks>
    /// Will be null if this <see cref="DrawOperation"/> is not registered
    /// </remarks>
    public GraphicsManager? RegisteredOnto { get; private set; }

    #endregion

    #region Internal

    internal void Register(IDrawableNode owner, GraphicsManager manager)
    {
        ThrowIfDisposed();

        Registering(owner, manager);

        var device = manager.Device;
        CreateResources(device, device.ResourceFactory, out var commands);

        Device = device;
        Commands = commands;
        Owner = owner;
        RegisteredOnto = manager;
        
        Registered();
    }

    #endregion

    #region Reaction Methods

    /// <summary>
    /// Creates the necessary resources for this <see cref="DrawOperation"/>
    /// </summary>
    /// <param name="device">The device of the attached <see cref="GraphicsManager"/></param>
    /// <param name="factory"><paramref name="device"/>'s <see cref="ResourceFactory"/></param>
    /// <param name="commands">A <see cref="CommandList"/> created inside this method, to be set as this <see cref="DrawOperation"/>'s designated <see cref="CommandList"/></param>
    protected virtual void CreateResources(GraphicsDevice device, ResourceFactory factory, out CommandList commands)
    {
        commands = factory.CreateCommandList();
    }

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

    private bool isStarted = false;

    internal async ValueTask InternalDraw(Vector2 offset)
    {
        ThrowIfDisposed();
        if (!isStarted)
        {
            isStarted = true;
            Start();
        }
        Commands!.Begin();
        await Draw(offset, Commands, Device!);
        Commands.End();
    }

    #endregion

    #region Reaction Methods

    /// <summary>
    /// The method that will be used to draw the component
    /// </summary>
    /// <remarks>
    /// Remember that <paramref name="commandList"/> is *NOT* thread-safe, but it is owned solely by this <see cref="DrawOperation"/>; and <see cref="GraphicsManager"/> will not use it until this method returns.
    /// </remarks>
    /// <param name="offset">The translation offset of the drawing operation</param>
    /// <param name="device">The Veldrid <see cref="GraphicsDevice"/></param>
    /// <param name="commandList">The <see cref="CommandList"/> opened specifically for this call. <see cref="CommandList.End"/> will be called AFTER this method returns, so don't call it yourself</param>
    protected abstract ValueTask Draw(Vector2 offset, CommandList commandList, GraphicsDevice device);

    /// <summary>
    /// This method is called automatically when this <see cref="DrawOperation"/> is going to be drawn for the first time
    /// </summary>
    protected internal abstract void Start();

    #endregion

    #endregion

    #region Disposal

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> if this <see cref="DrawOperation"/> has already been disposed
    /// </summary>
    /// <exception cref="ObjectDisposedException"></exception>
    protected void ThrowIfDisposed()
    {
        lock (sync)
            if (disposedValue)
                throw new ObjectDisposedException(GetType().FullName);
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
        lock (sync)
        {
            if (disposedValue)
                return;
            disposedValue = true;
        }

        var @lock = RegisteredOnto!.LockManager();
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
            RegisteredOnto = null;
            @lock.Dispose();
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
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        InternalDispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
