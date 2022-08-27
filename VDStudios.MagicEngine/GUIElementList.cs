using System.Collections;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a list of elements in a <see cref="GUIElement"/> or <see cref="GraphicsManager"/>
/// </summary>
/// <remarks>
/// This class cannot be inherited. This class cannot be instanced by user code. ALL reads into this <see cref="GUIElementList"/> are locked and thread safe.
/// </remarks>
public sealed class GUIElementList : IReadOnlyCollection<GUIElement>
{
    private readonly object sync = new();

    internal readonly LinkedList<GUIElement> elements;

    internal GUIElementList()
    {
        elements = new();
    }
    
    /// <summary>
    /// The amount of <see cref="GUIElement"/>s currently registered in this <see cref="GUIElementList"/>
    /// </summary>
    public int Count => elements.Count;

    /// <summary>
    /// Enumerates the <see cref="GUIElement"/>s in this <see cref="GUIElementList"/>
    /// </summary>
    /// <remarks>
    /// This does not include child <see cref="GUIElement"/>s. Adquiring an enumerator locks the collection and the owner <see cref="GUIElement"/>
    /// </remarks>
    public IEnumerator<GUIElement> GetEnumerator()
    {
        var node = elements.First;
        while (node is not null)
        {
            var value = node.Value;
            if (!value.SkipInEnumeration)
                yield return value;
            node = node.Next;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Returns an <see cref="IEnumerable{T}"/> that enumerates through ALL the <see cref="GUIElement"/>s accessible from this list. 
    /// </summary>
    /// <remarks>
    /// This includes the entire node tree starting from this point: Every <see cref="GUIElement"/>'s children, and their children as well. Since <see cref="GUIElement"/>'s are protected against circular references, this <see cref="IEnumerable"/> will eventually finish. How long that takes is your responsibility.
    /// </remarks>
    public IEnumerable<GUIElement> Flatten() => elements.SelectMany(x => x.SubElements);

    internal void Remove(GUIElement el)
    {
        lock (sync)
            elements.Remove(el);
    }

    internal void Remove(LinkedListNode<GUIElement> el)
    {
        lock (sync)
            elements.Remove(el);
    }

    /// <summary>
    /// Adding to the collection locks the collection and the owner <see cref="GUIElement"/>
    /// </summary>
    /// <param name="item"></param>
    /// <returns>The given Id for the node</returns>
    internal LinkedListNode<GUIElement> Add(GUIElement item)
    {
        lock (sync)
            return elements.AddLast(item);
    }

    /// <summary>
    /// This method does NOT notify nodes of their detachment, nor does it detach them, for that matter
    /// </summary>
    internal void Clear()
        => elements.Clear();
}