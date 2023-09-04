namespace VDStudios.MagicEngine.Utility;

/// <summary>
/// Wraps an <see cref="ObjectPool{T}"/> rented object that would usually be handled through a try/catch block so it can be used in a using statement, returning it to the pool upon being disposed
/// </summary>
/// <param name="Item">The item wrapped by this instance</param>
/// <param name="Pool">The pool <see cref="Item"/> belongs to</param>
public readonly record struct ObjectPoolDisposalWrapper<T>(T Item, ObjectPool<T> Pool) : IDisposable
    where T : class
{
    /// <summary>
    /// The item wrapped by this instance
    /// </summary>
    public T Item { get; } = Item ?? throw new ArgumentNullException(nameof(Item));

    /// <summary>
    /// The pool <see cref="Item"/> belongs to
    /// </summary>
    public ObjectPool<T> Pool { get; } = Pool ?? throw new ArgumentNullException(nameof(Pool));

    /// <inheritdoc/>
    public void Dispose()
    {
        Pool.Return(Item);
    }

    /// <summary>
    /// Gets <see cref="Item"/>
    /// </summary>
    /// <remarks>
    /// Intented to be used as: <c><see langword="using"/>(wrapper.GetItem(<see langword="out"/> <typeparamref name="T"/> item))</c>
    /// </remarks>
    public ObjectPoolDisposalWrapper<T> GetItem(out T item)
    {
        item = Item;
        return this;
    }
}
