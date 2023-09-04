using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace VDStudios.MagicEngine.Utility;

/// <summary>
/// Represents a set of preconfigured shared object pools
/// </summary>
public static class SharedObjectPools
{
    private static readonly Lazy<ObjectPool<StringBuilder>> lazy_stringBuilderPool = new(
        () => new ObjectPool<StringBuilder>(static x => new(50), static x => x.Clear(), 3, 5),
        LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// A shared pool of <see cref="StringBuilder"/> objects
    /// </summary>
    /// <remarks>
    /// Each <see cref="StringBuilder"/> is instanced to a starting capacity of 50, they're always cleared on return, have a growth factor of 3 and a starting pool of 5
    /// </remarks>
    public static ObjectPool<StringBuilder> StringBuilderPool => lazy_stringBuilderPool.Value;
}

/// <summary>
/// Represents a pool of reusable objects
/// </summary>
/// <typeparam name="T"></typeparam>
public class ObjectPool<T> : IDisposable where T : class
{
    private readonly ConcurrentStack<T> Pool = new();
    private readonly Action<T> Cleaner;
    private readonly Func<ObjectPool<T>, T> Factory;
    private readonly int Growth;

    /// <summary>
    /// Creates a new <see cref="ObjectPool{T}"/> with the desired mechanisms
    /// </summary>
    /// <param name="factory">Represents the method that will be used to construct new objects for the pool</param>
    /// <param name="preload">A number that signals how many objects should be constructed at the time of the pool's instancing</param>
    /// <param name="cleaner">Represents the method that will be used to clear, or otherwise cleanup the pooled object after use</param>
    /// <param name="growthFactor">Represents how much will the pool grow when its emptied -- For example, if the pool has 3 objects, and 3 of them are requested, it will instance <paramref name="growthFactor"/> more objects. If this parameter is 0, the pool will not grow automatically</param>
    public ObjectPool(Func<ObjectPool<T>, T> factory, Action<T> cleaner, int growthFactor = 1, int preload = 0)
    {
        if (growthFactor < 0)
            throw new ArgumentOutOfRangeException(nameof(growthFactor), growthFactor, "Argument cannot be less than 0");
        if (preload < 0)
            throw new ArgumentOutOfRangeException(nameof(preload), preload, "Argument cannot be less than 0");
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(cleaner);

        Cleaner = cleaner;
        Factory = factory;
        Growth = growthFactor;

        while (preload-- > 0)
            Pool.Push(factory(this));
    }

    /// <summary>
    /// Tries to rent an object from the pool; will throw if the pool is exhausted and has a growht factor of 0
    /// </summary>
    /// <remarks>
    /// This method will throw an <see cref="InvalidOperationException"/> if the pool is exhausted and the growht is set to 0
    /// </remarks>
    /// <returns>A wrapper around the rented object</returns>
    public ObjectPoolDisposalWrapper<T> Rent()
    {
        GrowIfEmpty();
        return new(!Pool.TryPop(out var result) ? throw new InvalidOperationException("This ObjectPool is exhausted") : result, this);
    }

    /// <summary>
    /// Tries to rent an object from the pool; will throw if the pool is exhausted and has a growht factor of 0
    /// </summary>
    /// <remarks>
    /// This method will throw an <see cref="InvalidOperationException"/> if the pool is exhausted and the growht is set to 0
    /// </remarks>
    /// <returns>The rented object</returns>
    public ObjectPoolDisposalWrapper<T> Rent(out T obj)
    {
        GrowIfEmpty();
        return new ObjectPoolDisposalWrapper<T>(!Pool.TryPop(out var result) ? throw new InvalidOperationException("This ObjectPool is exhausted") : result, this).GetItem(out obj);
    }

    /// <summary>
    /// Tries to rent an object from the pool; will fail if the pool is exhausted and has a growht factor of 0
    /// </summary>
    /// <param name="obj">The rented object, or <c>null</c></param>
    /// <returns>Whether the object was succesfully rented or not</returns>
    public bool TryRent([NotNullWhen(true)] out ObjectPoolDisposalWrapper<T> obj)
    {
        GrowIfEmpty();
        if (Pool.TryPop(out var o))
        {
            obj = new(o, this);
            return true;
        }

        obj = default;
        return false;
    }

    /// <summary>
    /// Returns a previously rented object into the pool
    /// </summary>
    /// <param name="obj">The object to return</param>
    /// <param name="clear"><c>true</c> if the cleaner method should be called prior to the object being added back into the pool</param>
    public void Return(T obj, bool clear = true)
    {
        if (clear)
            Cleaner(obj);
        Pool.Push(obj);
    }

    /// <summary>
    /// Expands the pool and fills it with <paramref name="amount"/> new objects
    /// </summary>
    /// <param name="amount">The amount of objects to add into the pool, and how much more space to give it</param>
    public void Add(int amount)
    {
        lock (Pool)
            while (amount-- > 0)
                Pool.Push(Factory(this));
    }

    private void GrowIfEmpty()
    {
        if (Pool.IsEmpty)
        {
            int g = Growth;
            lock (Pool)
                if (Pool.IsEmpty)
                    while (g-- > 0)
                        Pool.Push(Factory(this));
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
