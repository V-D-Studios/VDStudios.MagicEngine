using System.Collections;

namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// Represents a list of <see cref="GraphicsManager{TGraphicsContext}"/>s in a <see cref="Game"/>
/// </summary>
/// <remarks>
/// This class cannot be inherited. This class cannot be instanced by user code. <see cref="GraphicsManagerList{TGraphicsContext}"/> is not very performant, and should not be used in hot paths
/// </remarks>
public sealed class GraphicsManagerList<TGraphicsContext> : IReadOnlyList<GraphicsManager<TGraphicsContext>>
    where TGraphicsContext : IGraphicsContext
{
    private readonly LinkedList<GraphicsManager<TGraphicsContext>> Managers = new();

    #region Public

    /// <inheritdoc/>
    public GraphicsManager<TGraphicsContext> this[int index]
    {
        get
        {
            lock (Managers)
                return Managers.ElementAt(index);
        }
    }

    /// <summary>
    /// Gets the current amount of <see cref="GraphicsManager{TGraphicsContext}"/> held in this list
    /// </summary>
    public int Count => Managers.Count;

    /// <inheritdoc/>
    public IEnumerator<GraphicsManager<TGraphicsContext>> GetEnumerator()
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

    internal void Remove(GraphicsManager<TGraphicsContext> manager)
    {
        lock (Managers)
            Managers.Remove(manager);
    }

    internal void RemoveAt(int index)
    {
        lock (Managers)
            Managers.Remove(Managers.ElementAt(index));
    }

    internal void Add(GraphicsManager<TGraphicsContext> manager)
    {
        lock (Managers)
            Managers.AddLast(manager);
    }

    #endregion
}
