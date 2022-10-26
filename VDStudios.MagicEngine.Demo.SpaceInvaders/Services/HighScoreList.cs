using System.Collections;

namespace VDStudios.MagicEngine.Demo.SpaceInvaders.Services;

public class HighScoreList : IList<HighScore>
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
    }

    public void RemoveAt(int index)
    {
        _scores.RemoveAt(index);
    }

    public HighScore this[int index]
    {
        get => _scores[index];
        set => _scores[index] = value;
    }

    public void Add(HighScore item)
    {
        _scores.Add(item);
        _scores.Sort((l, r) => l.Score.CompareTo(r.Score));
    }

    public void Clear()
    {
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
        return _scores.Remove(item);
    }

    public int Count => _scores.Count;

    public bool IsReadOnly => false;

    public IEnumerator<HighScore> GetEnumerator() => _scores.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _scores.GetEnumerator();
    }
}
