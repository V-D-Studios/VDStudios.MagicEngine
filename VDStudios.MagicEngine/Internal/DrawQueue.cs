using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.Internal;

internal sealed class DrawQueue<T> : IDrawQueue<T> where T : InternalGraphicalOperation
{
    #region Fields

    private readonly PriorityQueue<T, float> _queue = new(20, new PriorityComparer());
    internal readonly AsyncLock _lock = new();

    #endregion

    internal T Dequeue() => _queue.Dequeue();

    public int Count => _queue.Count;

    public async Task EnqueueAsync(T drawing, float priority)
    {
        using (await _lock.LockAsync())
            _queue.Enqueue(drawing, priority);
    }

    public void Enqueue(T drawing, float priority)
    {
        using (_lock.Lock())
            _queue.Enqueue(drawing, priority);
    }

    public void EnsureCapacity(int capacity)
    {
        using (_lock.Lock())
            _queue.EnsureCapacity(capacity);
    }

    public async Task EnsureCapacityAsync(int capacity)
    {
        using (await _lock.LockAsync())
            _queue.EnsureCapacity(capacity);
    }

    public void EnsureFreeSpace(int freeSpace)
    {
        using (_lock.Lock())
            _queue.EnsureCapacity(_queue.Count + freeSpace);
    }

    public async Task EnsureFreeSpaceAsync(int freeSpace)
    {
        using (await _lock.LockAsync())
            _queue.EnsureCapacity(_queue.Count + freeSpace);
    }

    public void EnqueueCollection(IReadOnlyCollection<(T drawing, float priority)> items)
    {
        using (_lock.Lock())
        {
            _queue.EnsureCapacity(_queue.Count + items.Count);
            _queue.EnqueueRange(items);
        }
    }

    public async Task EnqueueCollectionAsync(IReadOnlyCollection<(T drawing, float priority)> items)
    {
        using (await _lock.LockAsync())
        {
            _queue.EnsureCapacity(_queue.Count + items.Count);
            _queue.EnqueueRange(items);
        }
    }

    public void EnqueueRange(IEnumerable<(T drawing, float priority)> items)
    {
        using (_lock.Lock())
            _queue.EnqueueRange(items);
    }

    public async Task EnqueueRangeAsync(IEnumerable<(T drawing, float priority)> items)
    {
        using (await _lock.LockAsync())
            _queue.EnqueueRange(items);
    }

    public async Task EnqueueAsyncRange(IAsyncEnumerable<(T drawing, float priority)> items)
    {
        await foreach (var (drawing, priority) in items)
            using(await _lock.LockAsync())
                _queue.Enqueue(drawing, priority);
    }

    public void EnqueueCollection(IReadOnlyCollection<T> items, float priority)
    {
        using (_lock.Lock())
        {
            _queue.EnsureCapacity(_queue.Count + items.Count);
            _queue.EnqueueRange(items, priority);
        }
    }

    public async Task EnqueueCollectionAsync(IReadOnlyCollection<T> items, float priority)
    {
        using (await _lock.LockAsync())
        {
            _queue.EnsureCapacity(_queue.Count + items.Count);
            _queue.EnqueueRange(items, priority);
        }
    }

    public void EnqueueRange(IEnumerable<T> items, float priority)
    {
        using (_lock.Lock())
            _queue.EnqueueRange(items, priority);
    }

    public async Task EnqueueRangeAsync(IEnumerable<T> items, float priority)
    {
        using (await _lock.LockAsync())
            _queue.EnqueueRange(items, priority);
    }

    public async Task EnqueueAsyncRange(IAsyncEnumerable<T> items, float priority)
    {
        await foreach (var drawing in items)
            using (await _lock.LockAsync())
                _queue.Enqueue(drawing, priority);
    }

    #region Comparer

    private class PriorityComparer : IComparer<float>
    {
        public int Compare(float x, float y) => x > y ? 1 : -1;
    }

    #endregion
}
