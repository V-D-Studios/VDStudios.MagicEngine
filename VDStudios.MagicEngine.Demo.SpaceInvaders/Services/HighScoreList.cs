using System.Collections;
using System.Collections.Immutable;
using System.Collections.Specialized;
using VDStudios.Utilities.Collections;

namespace VDStudios.MagicEngine.Demo.SpaceInvaders.Services;

public class HighScoreList : IList<HighScore>, INotifyCollectionChanged<HighScoreList, HighScore>
{
    private readonly List<HighScore> _scores = new(10);

    public int IndexOf(HighScore item)
    {
        return _scores.IndexOf(item);
    }

    public void Insert(int index, HighScore item)
    {
        _scores.Insert(index, item);
        _scores.Sort((l, r) => l.Score.CompareTo(r.Score));
        CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Add, item, index));
    }

    public void RemoveAt(int index)
    {
        var item = _scores[index];
        _scores.RemoveAt(index);
        CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Remove, item, index));
    }

    public HighScore this[int index]
    {
        get => _scores[index];
        set
        {
            if (_scores[index] is not HighScore replaced)
            {
                _scores[index] = value;
                CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Add, value, index));
                return;
            }

            _scores[index] = value;
            CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Replace, value, replaced, index));
        }
    }

    public void Add(HighScore item)
    {
        var ev = CollectionChanged;
        if(ev is not null)
        {
            _scores.Add(item);
            CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Add, item, _scores.Count - 1));

            var prev = _scores.ToImmutableArray();
        
            _scores.Sort((l, r) => l.Score.CompareTo(r.Score));
            CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Move, _scores.ToImmutableArray(), prev));

            return;
        }
        // If there are no subscribers, we save ourselves the trouble of having to create two whole arrays for no reason

        _scores.Add(item);
        _scores.Sort((l, r) => l.Score.CompareTo(r.Score));
    }

    public void Clear()
    {
        var ev = CollectionChanged;
        if (ev is not null)
        {
            var prev = _scores.ToImmutableArray();
            _scores.Clear();
            CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Reset, default, prev));
            return;
        }
        // If there are no subscribers, we save ourselves the trouble of having to create a new array for no reason

        _scores.Clear();
    }

    public bool Contains(HighScore item)
    {
        return _scores.Contains(item);
    }

    public void CopyTo(HighScore[] array, int arrayIndex)
    {
        _scores.CopyTo(array, arrayIndex);
    }

    public bool Remove(HighScore item)
    {
        var res = _scores.Remove(item);
        if (res)
            CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Remove, item));
        return res;
    }

    public int Count => _scores.Count;

    public bool IsReadOnly => false;

    public IEnumerator<HighScore> GetEnumerator() => _scores.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _scores.GetEnumerator();
    }

    public event CollectionChangedEvent<HighScoreList, HighScore>? CollectionChanged;
}
