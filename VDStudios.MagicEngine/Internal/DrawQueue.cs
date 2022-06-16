using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.Internal;

internal sealed class DrawQueue : IDrawQueue
{
    #region Fields

    private readonly PriorityQueue<IDrawing, float> _queue = new(20, new PriorityComparer());
    private readonly AsyncLock _lock = new();

    #endregion

    public int Count { get; }

    public async Task EnqueueAsync(IDrawing drawing, float priority)
    {
        using (await _lock.LockAsync())
            _queue.Enqueue(drawing, priority);
    }

    public void Enqueue(IDrawing drawing, float priority)
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

    public void EnqueueCollection(IReadOnlyCollection<(IDrawing drawing, float priority)> items)
    {
        using (_lock.Lock())
        {
            _queue.EnsureCapacity(_queue.Count + items.Count);
            _queue.EnqueueRange(items);
        }
    }

    public async Task EnqueueCollectionAsync(IReadOnlyCollection<(IDrawing drawing, float priority)> items)
    {
        using (await _lock.LockAsync())
        {
            _queue.EnsureCapacity(_queue.Count + items.Count);
            _queue.EnqueueRange(items);
        }
    }

    public void EnqueueRange(IEnumerable<(IDrawing drawing, float priority)> items)
    {
        using (_lock.Lock())
            _queue.EnqueueRange(items);
    }

    public async Task EnqueueRangeAsync(IEnumerable<(IDrawing drawing, float priority)> items)
    {
        using (await _lock.LockAsync())
            _queue.EnqueueRange(items);
    }

    public async Task EnqueueAsyncRange(IAsyncEnumerable<(IDrawing drawing, float priority)> items)
    {
        await foreach (var (drawing, priority) in items)
            using(await _lock.LockAsync())
                _queue.Enqueue(drawing, priority);
    }

    public void EnqueueCollection(IReadOnlyCollection<IDrawing> items, float priority)
    {
        using (_lock.Lock())
        {
            _queue.EnsureCapacity(_queue.Count + items.Count);
            _queue.EnqueueRange(items, priority);
        }
    }

    public async Task EnqueueCollectionAsync(IReadOnlyCollection<IDrawing> items, float priority)
    {
        using (await _lock.LockAsync())
        {
            _queue.EnsureCapacity(_queue.Count + items.Count);
            _queue.EnqueueRange(items, priority);
        }
    }

    public void EnqueueRange(IEnumerable<IDrawing> items, float priority)
    {
        using (_lock.Lock())
            _queue.EnqueueRange(items, priority);
    }

    public async Task EnqueueRangeAsync(IEnumerable<IDrawing> items, float priority)
    {
        using (await _lock.LockAsync())
            _queue.EnqueueRange(items, priority);
    }

    public async Task EnqueueAsyncRange(IAsyncEnumerable<IDrawing> items, float priority)
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
