using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Nito.AsyncEx;
using VDStudios.MagicEngine.Graphics;

namespace VDStudios.MagicEngine.Internal;

internal sealed class DrawQueue<TGraphicsContext> : IDrawQueueEnqueuer<DrawOperation<TGraphicsContext>, TGraphicsContext>
    where TGraphicsContext : GraphicsContext<TGraphicsContext>
{
    #region Fields

    private readonly PriorityQueue<DrawOperation<TGraphicsContext>, float> _queues;
    internal readonly AsyncLock _lock = new();

    #endregion

    public DrawQueue(ImmutableArray<CommandListGroupDefinition> commandListGroups)
    {
        _queues = new PriorityQueue<DrawOperation<TGraphicsContext>, float>[commandListGroups.Length];
        for (int i = 0; i < _queues.Length; i++)
            _queues[i] = new(commandListGroups[i].ExpectedOperations, new PriorityComparer());
    }

    internal DrawOperation<TGraphicsContext> Dequeue(int group) => _queues[group].Dequeue();

    internal bool TryDequeue(int group, [MaybeNullWhen(true)] out DrawOperation<TGraphicsContext>? drawOperation) => _queues[group].TryDequeue(out drawOperation, out _);

    internal PriorityQueue<DrawOperation<TGraphicsContext>, float> GetQueue(int group) => _queues[group];
    internal int QueueCount => _queues.Length;

    /// <summary>
    /// Gets the total amount of <see cref="DrawOperation<TGraphicsContext>"/>s in all the queues in this draw queue
    /// </summary>
    /// <returns></returns>
    public int GetTotalCount()
    {
        int count = 0;
        for (int i = 0; i < _queues.Length; i++)
            count += _queues[i].Count;
        return count;
    }

    public async Task EnqueueAsync(DrawOperation<TGraphicsContext> drawing, float priority)
    {
        using (await _lock.LockAsync())
            _queues[drawing._clga].Enqueue(drawing, priority);
    }

    public void Enqueue(DrawOperation<TGraphicsContext> drawing, float priority)
    {
        using (_lock.Lock())
            _queues[drawing._clga].Enqueue(drawing, priority);
    }

    #region Comparer

    private class PriorityComparer : IComparer<float>
    {
        public int Compare(float x, float y) => x > y ? -1 : 1;
    }

    #endregion
}
