using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.Utility;

/// <summary>
/// Represents a pool of reusable objects
/// </summary>
/// <typeparam name="T"></typeparam>
public class ObjectPool<T> : IDisposable where T : class, IPoolableObject
{
    private readonly ConcurrentStack<T> Pool = new();
    private readonly Func<ObjectPool<T>, T> Factory;
    private readonly int Growth;

    /// <summary>
    /// Creates a new <see cref="ObjectPool{T}"/> with the desired mechanisms
    /// </summary>
    /// <param name="factory">Represents the method that will be used to construct new objects for the pool</param>
    /// <param name="preload">A number that signals how many objects should be constructed at the time of the pool's instancing</param>
    /// <param name="growthFactor">Represents how much will the pool grow when its emptied -- For example, if the pool has 3 objects, and 3 of them are requested, it will instance <paramref name="growthFactor"/> more objects. If this parameter is 0, the pool will not grow automatically</param>
    public ObjectPool(Func<ObjectPool<T>, T> factory, int growthFactor = 1, int preload = 0)
    {
        if (growthFactor < 0)
            throw new ArgumentOutOfRangeException(nameof(growthFactor), growthFactor, "Argument cannot be less than 0");
        if (preload < 0)
            throw new ArgumentOutOfRangeException(nameof(preload), preload, "Argument cannot be less than 0");
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
    /// <returns>The rented object</returns>
    public T Rent()
    {
        GrowIfEmpty();
        return !Pool.TryPop(out var result) ? throw new InvalidOperationException("This ObjectPool is exhausted") : result;
    }

    /// <summary>
    /// Tries to rent an object from the pool; will fail if the pool is exhausted and has a growht factor of 0
    /// </summary>
    /// <param name="obj">The rented object, or <c>null</c></param>
    /// <returns>Whether the object was succesfully rented or not</returns>
    public bool TryRent([NotNullWhen(true)] out T? obj)
    {
        GrowIfEmpty();
        return Pool.TryPop(out obj);
    }

    /// <summary>
    /// Returns a previously rented object into the pool
    /// </summary>
    /// <param name="obj">The object to return</param>
    /// <param name="clear"><c>true</c> if <see cref="IPoolableObject.Clear"/> should be called prior to the object being added back into the pool</param>
    public void Return(T obj, bool clear = true)
    {
        if (clear)
            obj.Clear();
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

/// <summary>
/// Provides a mechanism for an object to be returned after it's no longer needed and sent back to its <see cref="ObjectPool{T}"/>
/// </summary>
public interface IPoolableObject
{
    /// <summary>
    /// Clears the object of all data pertaining the state of a specific dependant (i.e. the state of the class or method that last used it)
    /// </summary>
    public void Clear();
}