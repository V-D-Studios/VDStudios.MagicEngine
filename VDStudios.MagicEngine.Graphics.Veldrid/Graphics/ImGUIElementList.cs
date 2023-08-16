using System.Collections;

namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// Represents a list of elements in a <see cref="ImGUIElement{TGraphicsContext}"/> or <see cref="GraphicsManager{TGraphicsContext}"/>
/// </summary>
/// <remarks>
/// This class cannot be inherited. This class cannot be instanced by user code. ALL reads into this <see cref="ImGUIElementList{TGraphicsContext}"/> are locked and thread safe.
/// </remarks>
public sealed class ImGUIElementList<TGraphicsContext> : IReadOnlyCollection<ImGUIElement<TGraphicsContext>>
    where TGraphicsContext : GraphicsContext<TGraphicsContext>
{
    private readonly object sync = new();

    internal readonly LinkedList<ImGUIElement<TGraphicsContext>> elements;

    internal ImGUIElementList()
    {
        elements = new();
    }

    /// <summary>
    /// The amount of <see cref="ImGUIElement{TGraphicsContext}"/>s currently registered in this <see cref="ImGUIElementList{TGraphicsContext}"/>
    /// </summary>
    public int Count => elements.Count;

    /// <summary>
    /// Enumerates the <see cref="ImGUIElement{TGraphicsContext}"/>s in this <see cref="ImGUIElementList{TGraphicsContext}"/>
    /// </summary>
    /// <remarks>
    /// This does not include child <see cref="ImGUIElement{TGraphicsContext}"/>s. Adquiring an enumerator locks the collection and the owner <see cref="ImGUIElement{TGraphicsContext}"/>
    /// </remarks>
    public IEnumerator<ImGUIElement<TGraphicsContext>> GetEnumerator()
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

    internal void Remove(ImGUIElement<TGraphicsContext> el)
    {
        lock (sync)
            elements.Remove(el);
    }

    internal void Remove(LinkedListNode<ImGUIElement<TGraphicsContext>> el)
    {
        lock (sync)
            elements.Remove(el);
    }

    /// <summary>
    /// Adding to the collection locks the collection and the owner <see cref="ImGUIElement{TGraphicsContext}"/>
    /// </summary>
    /// <param name="item"></param>
    /// <returns>The given Id for the node</returns>
    internal LinkedListNode<ImGUIElement<TGraphicsContext>> Add(ImGUIElement<TGraphicsContext> item)
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