using System.Collections;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a list of <see cref="GraphicsManager"/>s in a <see cref="Game"/>
/// </summary>
/// <remarks>
/// This class cannot be inherited. This class cannot be instanced by user code. <see cref="GraphicsManagerList"/> is not very performant, and should not be used in hot paths
/// </remarks>
public sealed class GraphicsManagerList : IReadOnlyList<GraphicsManager>
{
    private readonly LinkedList<GraphicsManager> Managers = new();

    #region Public

    /// <inheritdoc/>
    public GraphicsManager this[int index]
    {
        get
        {
            lock (Managers)
                return Managers.ElementAt(index);
        }
    }

    /// <summary>
    /// Gets the current amount of <see cref="GraphicsManager"/> held in this list
    /// </summary>
    public int Count => Managers.Count;

    /// <inheritdoc/>
    public IEnumerator<GraphicsManager> GetEnumerator()
    {
        var managers = Managers;
        var current = managers.First;
        while (current is not null)
        {
            yield return current.Value;
            current = current.Next;
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

    #region Internal

    internal void Remove(GraphicsManager manager)
    {
        lock (Managers)
            Managers.Remove(manager);
    }

    internal void RemoveAt(int index)
    {
        lock (Managers)
            Managers.Remove(Managers.ElementAt(index));
    }

    internal void Add(GraphicsManager manager)
    {
        lock (Managers)
            Managers.AddLast(manager);
    }

    #endregion
}
