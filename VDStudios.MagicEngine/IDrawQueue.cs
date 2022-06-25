using System.Collections.Concurrent;

namespace VDStudios.MagicEngine;

/// <summary>
/// The engine's Draw Queue
/// </summary>
public interface IDrawQueue
{
    /// <summary>
    /// The current amount of items in the Draw Queue
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Enqueues a ready-to-draw object into the Draw Queue asynchronously
    /// </summary>
    /// <param name="drawing">The object that is ready to draw</param>
    /// <param name="priority">The priority of the object. Higher numbers will draw first and will appear below lower numbers</param>
    public Task EnqueueAsync(IDrawOperation drawing, float priority);

    /// <summary>
    /// Asynchronously enqueues a ready-to-draw object into the Draw Queue
    /// </summary>
    /// <param name="drawing">The object that is ready to draw</param>
    /// <param name="priority">The priority of the object. Higher numbers will draw first and will appear below lower numbers</param>
    public void Enqueue(IDrawOperation drawing, float priority);

    /// <summary>
    /// Enqueues a collection of ready-to-draw objects into the Draw Queue
    /// </summary>
    /// <param name="items">The object that are ready to draw and their priority</param>
    public void EnqueueCollection(IReadOnlyCollection<(IDrawOperation drawing, float priority)> items);

    /// <summary>
    /// Enqueues a collection of ready-to-draw objects into the Draw Queue, all with the same priority
    /// </summary>
    /// <param name="items">The object that are ready to draw and their priority</param>
    /// <param name="priority">The priority of all items</param>
    public void EnqueueCollection(IReadOnlyCollection<IDrawOperation> items, float priority);

    /// <summary>
    /// Asynchronously enqueues a collection of ready-to-draw objects into the Draw Queue
    /// </summary>
    /// <param name="items">The objects that are ready to draw and their priority</param>
    public Task EnqueueCollectionAsync(IReadOnlyCollection<(IDrawOperation drawing, float priority)> items);

    /// <summary>
    /// Asynchronously enqueues a collection of ready-to-draw objects into the Draw Queue, all with the same priority
    /// </summary>
    /// <param name="items">The objects that are ready to draw and their priority</param>
    /// <param name="priority">The priority of all items</param>
    public Task EnqueueCollectionAsync(IReadOnlyCollection<IDrawOperation> items, float priority);

    /// <summary>
    /// Enqueues a set of ready-to-draw objects into the Draw Queue
    /// </summary>
    /// <param name="items">The objects that are ready to draw and their priority</param>
    public void EnqueueRange(IEnumerable<(IDrawOperation drawing, float priority)> items);

    /// <summary>
    /// Enqueues a set of ready-to-draw objects into the Draw Queue, all with the same priority
    /// </summary>
    /// <param name="items">The objects that are ready to draw and their priority</param>
    /// <param name="priority">The priority of all items</param>
    public void EnqueueRange(IEnumerable<IDrawOperation> items, float priority);

    /// <summary>
    /// Asynchronously enqueues a set of ready-to-draw objects into the Draw Queue
    /// </summary>
    /// <param name="items">The objects that are ready to draw and their priority</param>
    public Task EnqueueRangeAsync(IEnumerable<(IDrawOperation drawing, float priority)> items);

    /// <summary>
    /// Asynchronously enqueues a set of ready-to-draw objects into the Draw Queue, all with the same priority
    /// </summary>
    /// <param name="items">The objects that are ready to draw and their priority</param>
    /// <param name="priority">The priority of all items</param>
    public Task EnqueueRangeAsync(IEnumerable<IDrawOperation> items, float priority);

    /// <summary>
    /// Asynchronously enqueues a set of ready-to-draw objects into the Draw Queue
    /// </summary>
    /// <param name="items">The objects that are ready to draw and their priority</param>
    public Task EnqueueAsyncRange(IAsyncEnumerable<(IDrawOperation drawing, float priority)> items);

    /// <summary>
    /// Asynchronously enqueues a set of ready-to-draw objects into the Draw Queue, all with the same priority
    /// </summary>
    /// <param name="items">The objects that are ready to draw and their priority</param>
    /// <param name="priority">The priority of all items</param>
    public Task EnqueueAsyncRange(IAsyncEnumerable<IDrawOperation> items, float priority);

    /// <summary>
    /// Ensures that the Draw Queue has the necessary capacity to host incoming operations
    /// </summary>
    /// <remarks>
    /// Sets the capacity of the Queue to <paramref name="capacity"/>, if it's more than the current capacity. Calling this is usually unnecessary, and would only be useful if you're to add a big collection of objects. For that, <see cref="EnsureFreeSpace"/> is recommended
    /// </remarks>
    /// <param name="capacity"></param>
    /// <returns></returns>
    public void EnsureCapacity(int capacity);

    /// <summary>
    /// Asynchronously ensures that the Draw Queue has the necessary capacity to host incoming operations
    /// </summary>
    /// <remarks>
    /// Sets the capacity of the Queue to <paramref name="capacity"/>, if it's more than the current capacity. Calling this is usually unnecessary, and would only be useful if you're to add a big collection of objects. For that, <see cref="EnsureFreeSpace"/> is recommended
    /// </remarks>
    /// <param name="capacity"></param>
    /// <returns></returns>
    public Task EnsureCapacityAsync(int capacity);

    /// <summary>
    /// Ensures that the Draw Queue has the necessary free space to host incoming operations
    /// </summary>
    /// <remarks>
    /// Sets the capacity of the Queue to <c><paramref name="freeSpace"/> + <see cref="Count"/></c>. Calling this method is only useful if you're going to add a batch of new items and know the amount beforehand. Consider using one of the Enqueue methods accepting collections instead
    /// </remarks>
    /// <param name="freeSpace">The amount of free space the queue needs to have</param>
    /// <returns></returns>
    public void EnsureFreeSpace(int freeSpace);

    /// <summary>
    /// Asynchronously ensures that the Draw Queue has the necessary free space to host incoming operations
    /// </summary>
    /// <remarks>
    /// Sets the capacity of the Queue to <c><paramref name="freeSpace"/> + <see cref="Count"/></c>. Calling this method is only useful if you're going to add a batch of new items and know the amount beforehand. Consider using one of the Enqueue methods accepting collections instead
    /// </remarks>
    /// <param name="freeSpace">The amount of free space the queue needs to have</param>
    /// <returns></returns>
    public Task EnsureFreeSpaceAsync(int freeSpace);
}
