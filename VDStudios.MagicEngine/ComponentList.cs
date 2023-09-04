using System.Collections;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a list of nodes in a <see cref="FunctionalComponent"/> or <see cref="Scene"/>
/// </summary>
/// <remarks>
/// This class cannot be inherited. This class cannot be instanced by user code. ALL reads into this <see cref="ComponentList"/> are locked and thread safe. Reads into this <see cref="ComponentList"/> also lock its owner's <see cref="ComponentList"/> related library-defined methods and properties
/// </remarks>
public sealed class ComponentList : IReadOnlyList<FunctionalComponent>
{
    internal static readonly ComponentList Empty = new();

    private int NextId => _nid++;
    private int _nid;

    private readonly object sync = new();

    private readonly Dictionary<int, FunctionalComponent> components;

    private ComponentList()
    {
        components = new();
        sync = null!;
    }

    private ComponentList(Dictionary<int, FunctionalComponent> nodes) => this.components = nodes;

    /// <summary>
    /// Gets the <see cref="FunctionalComponent"/> currently located at <paramref name="index"/>
    /// </summary>
    /// <remarks>Indexing the FunctionalComponent list locks the collection and the owner <see cref="FunctionalComponent"/></remarks>
    /// <param name="index">The <see cref="FunctionalComponent.ComponentId"/> in question</param>
    /// <returns>The <see cref="FunctionalComponent"/> located at <paramref name="index"/></returns>
    public FunctionalComponent this[int index]
    {
        get
        {
            ThrowIfEmptyList();
            lock (sync)
                return components[index];
        }
    }

    internal FunctionalComponent Get(int index) => components[index];

    /// <summary>
    /// The amount of <see cref="FunctionalComponent"/>s currently registered in this <see cref="ComponentList"/>
    /// </summary>
    public int Count => components.Count;

    /// <summary>
    /// Enumerates the <see cref="FunctionalComponent"/>s in this <see cref="ComponentList"/>
    /// </summary>
    /// <remarks>
    /// This does not include child <see cref="FunctionalComponent"/>s. Adquiring an enumerator locks the collection and the owner <see cref="FunctionalComponent"/>
    /// </remarks>
    public IEnumerator<FunctionalComponent> GetEnumerator()
    {
        lock (sync)
            return components.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal void Remove(int id)
        => components.Remove(id);

    /// <summary>
    /// Adding to the collection locks the collection and the owner <see cref="FunctionalComponent"/>
    /// </summary>
    /// <param name="item"></param>
    /// <returns>The given Id for the node</returns>
    internal int Add(FunctionalComponent item)
    {
        lock (sync)
        {
            var id = NextId;
            components.Add(id, item);
            return id;
        }
    }

    /// <summary>
    /// This method does NOT notify nodes of their detachment, nor does it detach them, for that matter
    /// </summary>
    internal void Clear()
        => components.Clear();

    /// <summary>
    /// Cloning the collection locks the collection and the owner <see cref="FunctionalComponent"/>
    /// </summary>
    /// <returns></returns>
    internal ComponentList Clone()
    {
        Dictionary<int, FunctionalComponent> nl;
        if (sync is null)
            nl = new Dictionary<int, FunctionalComponent>(1);
        else
            lock (sync)
            {
                nl = new Dictionary<int, FunctionalComponent>(components.Count + 2);
                foreach (var (k, v) in components)
                    nl.Add(k, v);
            }
        return new(nl);
    }

    private void ThrowIfEmptyList()
    {
        if (sync is null)
            throw new InvalidOperationException("Cannot mutate the Empty ComponentList. This is likely to be a library bug");
    }
}
