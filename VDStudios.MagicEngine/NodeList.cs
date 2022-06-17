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
/// This class cannot be inherited. This class cannot be instanced by user code
/// </remarks>
public sealed class NodeList : IReadOnlyList<Node>
{
    internal readonly AsyncLock sync = new();
    internal NodeList() { }

    public Node this[int index] { get; }

    public int Count { get; }

    public IEnumerator<Node> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }

    internal void Remove(int index)
    {
        throw new NotImplementedException();
    }

    internal void Add(Node item)
    {
        throw new NotImplementedException();
    }

    internal void Clear()
    {
        throw new NotImplementedException();
    }
}
