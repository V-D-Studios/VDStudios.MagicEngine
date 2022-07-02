using SDL2.NET;
using System.Numerics;
using VDStudios.MagicEngine.Internal;
using Veldrid;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents an operation that is ready to be drawn into the GUI. This object is automatically disposed of when deregistered, and must have a reference to it held by the owning <see cref="INodeGUI"/>. Otherwise, this object will be collected and disposed of
/// </summary>
/// <remarks>
/// Try to keep an object created from this class cached somewhere in a node, as they incur a number of allocations that should be avoided in a HotPath like the rendering sequence
/// </remarks>
public abstract class GUIDrawOperation : InternalGraphicalOperation, IDisposable
{
    /// <summary>
    /// The owner <see cref="INodeGUI"/> of this <see cref="GUIDrawOperation"/>
    /// </summary>
    /// <remarks>
    /// Will be null if this <see cref="GUIDrawOperation"/> is not registered
    /// </remarks>
    public INodeGUI Owner { get; private set; }

    /// <summary>
    /// This <see cref="GUIDrawOperation"/>'s unique identifier, generated automatically
    /// </summary>
    public Guid Identifier { get; } = Guid.NewGuid();

    #region Registration

    #region Properties

    /// <summary>
    /// The <see cref="GraphicsManager"/> this <see cref="GUIDrawOperation"/> is registered onto
    /// </summary>
    /// <remarks>
    /// Will be null if this <see cref="GUIDrawOperation"/> is not registered
    /// </remarks>
    public GraphicsManager? Manager { get; private set; }

    #endregion

    #region Internal

    internal void Register(INodeGUI owner, GraphicsManager manager)
    {
        ThrowIfDisposed();

        Registering(owner, manager);

        Owner = owner;
        Manager = manager;

        Registered();
    }

    #endregion

    #region Reaction Methods

    /// <summary>
    /// This method is called automatically when this <see cref="GUIDrawOperation"/> is being registered onto <paramref name="manager"/>
    /// </summary>
    /// <param name="owner">The <see cref="Node"/> that registered this <see cref="GUIDrawOperation"/></param>
    /// <param name="manager">The <see cref="GraphicsManager"/> this <see cref="GUIDrawOperation"/> is being registered onto</param>
    protected virtual void Registering(INodeGUI owner, GraphicsManager manager) { }

    /// <summary>
    /// This method is called automatically when this <see cref="GUIDrawOperation"/> has been registered
    /// </summary>
    protected virtual void Registered() { }

    #endregion

    #endregion

    #region Drawing

    #region Internal

    internal void InternalDraw(TimeSpan delta)
    {
        ThrowIfDisposed();
        Draw(delta);
    }

    #endregion

    #region Reaction Methods

    /// <summary>
    /// The method that will be used to draw the component
    /// </summary>
    /// <remarks>
    /// Unfinished calls and general uncarefulness with ImGUI WILL bleed into other <see cref="GUIDrawOperation"/>s
    /// </remarks>
    /// <param name="delta">The amount of time that has passed since the last draw sequence</param>
    protected abstract void Draw(TimeSpan delta);

    #endregion

    #endregion

    #region Disposal

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> if this <see cref="GUIDrawOperation"/> has already been disposed
    /// </summary>
    /// <exception cref="ObjectDisposedException"></exception>
    protected void ThrowIfDisposed()
    {
        if (disposedValue)
            throw new ObjectDisposedException(GetType().FullName);
    }

    internal bool disposedValue;

    /// <summary>
    /// Disposes of this <see cref="GUIDrawOperation"/>'s resources
    /// </summary>
    /// <remarks>
    /// Dispose of any additional resources your subtype allocates
    /// </remarks>
    protected virtual void Dispose(bool disposing) { }

    private void InternalDispose(bool disposing)
    {
        var @lock = Manager!.LockManager();
        try
        {
            Dispose(disposing);
        }
        finally
        {
            Manager = null;
            @lock.Dispose();
        }
    }

    /// <inheritdoc/>
    ~GUIDrawOperation()
    {
        InternalDispose(disposing: false);
    }

    /// <summary>
    /// Disposes of this <see cref="GUIDrawOperation"/>'s resources
    /// </summary>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        InternalDispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
