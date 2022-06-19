using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.Demo.Collections;
public class LimitedMemberQueue<T> : IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>, ICollection
{
    private readonly Queue<T> _q;

    public LimitedMemberQueue(int limit)
    {
        _q = new(limit);
    }
    
    public LimitedMemberQueue(IEnumerable<T> collection, int limit)
    {
        _q = new(collection);
        _q.EnsureCapacity(limit);
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
