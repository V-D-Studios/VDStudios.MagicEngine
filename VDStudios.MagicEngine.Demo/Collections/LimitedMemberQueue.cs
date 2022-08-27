using System.Collections;

namespace VDStudios.MagicEngine.Demo.Collections;
public class LimitedMemberQueue<T> : IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>, ICollection
{
    private readonly Queue<T> _q;

    public LimitedMemberQueue(int limit)
    {
        if (limit <= 0)
            throw new ArgumentException("limit must be larger than 0", nameof(limit));
        _q = new(limit);
        Limit = limit;
    }
    
    public LimitedMemberQueue(IEnumerable<T> collection, int limit)
    {
        if (limit <= 0)
            throw new ArgumentException("limit must be larger than 0", nameof(limit));
        _q = new(collection);
        _q.EnsureCapacity(limit);
        Limit = limit;
    }

    public void CopyTo(Array array, int index)
    {
        ((ICollection)_q).CopyTo(array, index);
    }

    public T Dequeue() => _q.Dequeue();

    public void Enqueue(T item)
    {
        while (Count >= Limit)
            _q.Dequeue();
        _q.Enqueue(item);
    }

    public int Limit { get; }

    public int Count => _q.Count;

    public bool IsSynchronized => false;

    public object SyncRoot => ((ICollection)_q).SyncRoot;

    public IEnumerator GetEnumerator()
        => _q.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
        => _q.GetEnumerator();
}
