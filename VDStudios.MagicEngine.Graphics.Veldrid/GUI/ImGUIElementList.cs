using System.Collections;

namespace VDStudios.MagicEngine.Graphics.Veldrid.GUI;

/// <summary>
/// Represents a list of elements in a <see cref="GraphicsManager{TGraphicsContext}"/>
/// </summary>
/// <remarks>
/// This class cannot be inherited. This class cannot be instanced by user code. ALL reads into this <see cref="ImGUIElementList"/> are locked and thread safe.
/// </remarks>
public sealed class ImGUIElementList : IReadOnlyCollection<ImGUIElement>
{
    internal readonly object sync = new();

    private readonly HashSet<ImGUIElement> elements;

    internal ImGUIElementList()
    {
        elements = new();
    }

    /// <summary>
    /// The amount of <see cref="ImGUIElement"/>s currently registered in this <see cref="ImGUIElementList"/>
    /// </summary>
    public int Count => elements.Count;

    /// <summary>
    /// Enumerates the <see cref="ImGUIElement"/>s in this <see cref="ImGUIElementList"/>
    /// </summary>
    /// <remarks>
    /// This does not include child <see cref="ImGUIElement"/>s. Adquiring an enumerator locks the collection and the owner <see cref="ImGUIElement"/>
    /// </remarks>
    public IEnumerator<ImGUIElement> GetEnumerator()
        => elements.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Removes <paramref name="el"/> from the list
    /// </summary>
    /// <returns><see langword="true"/> if the element was found and removed, <see langword="false"/> otherwise</returns>
    public bool Remove(ImGUIElement el)
    {
        lock (sync)
            return elements.Remove(el);
    }

    /// <summary>
    /// Adding to the collection locks the collection and the owner <see cref="ImGUIElement"/>
    /// </summary>
    /// <param name="item"></param>
    /// <returns><see langword="true"/> if the element was not already presentd and added, <see langword="false"/> otherwise</returns>
    public bool Add(ImGUIElement item)
    {
        lock (sync)
            return elements.Add(item);
    }

    /// <summary>
    /// This method does NOT notify nodes of their detachment, nor does it detach them, for that matter
    /// </summary>
    public void Clear()
        => elements.Clear();
}