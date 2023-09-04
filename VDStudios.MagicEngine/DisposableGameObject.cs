namespace VDStudios.MagicEngine;

/// <summary>
/// Represents an object of the <see cref="Game"/> that is disposable
/// </summary>
/// <remarks>
/// This class cannot be inherited directly by user code
/// </remarks>
public abstract class DisposableGameObject : GameObject, IDisposable
{
    /// <inheritdoc/>
    protected DisposableGameObject(Game game, string facility, string area) : base(game, facility, area)
    {
    }

    #region IDisposable

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> if this object is disposed at the time of this method being called
    /// </summary>
    /// <exception cref="ObjectDisposedException"></exception>
    protected internal void ThrowIfDisposed()
    {
        if (IsDisposed)
            throw new ObjectDisposedException(GetType().Name);
    }

    /// <summary>
    /// Disposes of this <see cref="DisposableGameObject"/> and any of its resources
    /// </summary>
    /// <remarks>
    /// Child classes looking to override this method should instead refer to <see cref="Dispose(bool)"/>
    /// </remarks>
    public void Dispose()
    {
        InternalDispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Always call base.Dispose AFTER your own disposal code
    /// </summary>
    /// <param name="disposing"></param>
    internal virtual void InternalDispose(bool disposing)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        try
        {
            if (disposing)
                AboutToDispose?.Invoke(this, Game.TotalTime);
        }
        finally
        {
            Dispose(disposing);
            IsDisposed = true;
        }
    }

    /// <inheritdoc/>
    ~DisposableGameObject()
    {
        InternalDispose(false);
    }

    /// <summary>
    /// Runs when the object is being disposed. Don't call this! It'll be called automatically! Call <see cref="Dispose()"/> instead
    /// </summary>
    /// <param name="disposing">Whether this method was called through <see cref="IDisposable.Dispose"/> or by the GC calling this object's finalizer</param>
    protected virtual void Dispose(bool disposing) { }

    /// <summary>
    /// Fired right before this <see cref="GameObject"/> is disposed
    /// </summary>
    /// <remarks>
    /// While .NET allows fire-and-forget async methods in these events (<c><see langword="async void"/></c>), this is *NOT* recommended, as it's almost guaranteed the <see cref="DisposableGameObject"/> will be fully disposed before the async portion of your code gets a chance to run
    /// </remarks>
    public event GameObjectEvent<GameObject>? AboutToDispose;

    #endregion
}
