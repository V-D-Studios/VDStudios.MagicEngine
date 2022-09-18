using Nito.AsyncEx;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace VDStudios.MagicEngine.Internal;

internal sealed class DrawQueue : IDrawQueue<DrawOperation>
{
    #region Fields

    private readonly PriorityQueue<DrawOperation, float>[] _queues;
    internal readonly AsyncLock _lock = new();

    #endregion

    public DrawQueue(ImmutableArray<CommandListGroupDefinition> commandListGroups)
    {
        _queues = new PriorityQueue<DrawOperation, float>[commandListGroups.Length];
        for (int i = 0; i < _queues.Length; i++)
            _queues[i] = new(commandListGroups[i].ExpectedOperations, new PriorityComparer());
    }

    internal DrawOperation Dequeue(int group) => _queues[group].Dequeue();

    internal bool TryDequeue(int group, [MaybeNullWhen(true)] out DrawOperation? drawOperation) => _queues[group].TryDequeue(out drawOperation, out _);

    internal PriorityQueue<DrawOperation, float> GetQueue(int group) => _queues[group];
    internal int QueueCount => _queues.Length;

    public async Task EnqueueAsync(DrawOperation drawing, float priority)
    {
        using (await _lock.LockAsync())
            _queues[drawing._clga].Enqueue(drawing, priority);
    }

    public void Enqueue(DrawOperation drawing, float priority)
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
