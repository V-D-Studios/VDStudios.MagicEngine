using Nito.AsyncEx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a list of nodes in a <see cref="Node"/> or <see cref="Scene"/>
/// </summary>
/// <remarks>
/// This class cannot be inherited. This class cannot be instanced by user code. ALL reads into this <see cref="NodeList"/> are locked and thread safe. Reads into this <see cref="NodeList"/> also lock its owner's <see cref="NodeList"/> related library-defined methods and properties
/// </remarks>
public sealed class NodeList : IReadOnlyList<Node>
{
    internal static readonly NodeList Empty = new();

    private int NextId => _nid++;
    private int _nid;

    private readonly object sync = new();

    private readonly Dictionary<int, Node> nodes;

    private NodeList()
    {
        nodes = new();
        sync = null!;
    }

    private NodeList(Dictionary<int, Node> nodes) => this.nodes = nodes;

    /// <summary>
    /// Gets the <see cref="Node"/> currently located at <paramref name="index"/>
    /// </summary>
    /// <remarks>Indexing the Node list locks the collection and the owner <see cref="Node"/></remarks>
    /// <param name="index">The <see cref="Node.Id"/> in question</param>
    /// <returns>The <see cref="Node"/> located at <paramref name="index"/></returns>
    public Node this[int index]
    {
        get
        {
            ThrowIfEmptyList();
            lock (sync)
                return nodes[index];
        }
    }

    internal Node Get(int index) => nodes[index];

    /// <summary>
    /// The amount of <see cref="Node"/>s currently registered in this <see cref="NodeList"/>
    /// </summary>
    public int Count => nodes.Count;

    /// <summary>
    /// Enumerates the <see cref="Node"/>s in this <see cref="NodeList"/>
    /// </summary>
    /// <remarks>
    /// This does not include child <see cref="Node"/>s. Adquiring an enumerator locks the collection and the owner <see cref="Node"/>
    /// </remarks>
    public IEnumerator<Node> GetEnumerator()
    {
        lock (sync)
            return nodes.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Returns an <see cref="IEnumerable{T}"/> that enumerates through ALL the <see cref="Node"/>s accessible from this list. 
    /// </summary>
    /// <remarks>
    /// This includes the entire node tree starting from this point: Every <see cref="Node"/>'s children, and their children as well. Since <see cref="Node"/>'s are protected against circular references, this <see cref="IEnumerable"/> will eventually finish. How long that takes is your responsibility.
    /// </remarks>
    public IEnumerable<Node> Flatten() => nodes.SelectMany(x => x.Value.Children);

    internal void Remove(int id)
        => nodes.Remove(id);

    /// <summary>
    /// Adding to the collection locks the collection and the owner <see cref="Node"/>
    /// </summary>
    /// <param name="item"></param>
    /// <returns>The given Id for the node</returns>
    internal int Add(Node item)
    {
        lock (sync)
        {
            var id = NextId;
            nodes.Add(id, item);
            return id;
        }
    }

    /// <summary>
    /// This method does NOT notify nodes of their detachment, nor does it detach them, for that matter
    /// </summary>
    internal void Clear()
        => nodes.Clear();

    /// <summary>
    /// Cloning the collection locks the collection and the owner <see cref="Node"/>
    /// </summary>
    /// <returns></returns>
    internal NodeList Clone()
    {
        Dictionary<int, Node> nl;
        if (sync is null)
            nl = new Dictionary<int, Node>(1);
        else
            lock (sync)
            {
                nl = new Dictionary<int, Node>(nodes.Count + 2);
                foreach (var (k, v) in nodes)
                    nl.Add(k, v);
            }
        return new(nl);
    }

    private void ThrowIfEmptyList()
    {
        if (sync is null)
            throw new InvalidOperationException("Cannot mutate the Empty NodeList. This is likely to be a library bug");
    }
}
