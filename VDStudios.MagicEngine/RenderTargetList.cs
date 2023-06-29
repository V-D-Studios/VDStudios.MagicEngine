using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine;

/// <summary>
/// A list that contains <see cref="IRenderTarget"/>
/// </summary>
public class RenderTargetList
{
    private readonly HashSet<IRenderTarget> hashset = new();

    internal RenderTargetList(GraphicsManager owner)
    {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    /// <summary>
    /// The <see cref="GraphicsManager"/> that owns and uses this list
    /// </summary>
    public GraphicsManager Owner { get; }

    /// <summary>
    /// Adds a new <see cref="IRenderTarget"/> to this list
    /// </summary>
    /// <param name="item">The item to add</param>
    /// <returns><see langword="true"/> if <paramref name="item"/> was succesfully added, <see langword="false"/> otherwise</returns>
    /// <exception cref="ArgumentException">This exception is thrown if <paramref name="item"/>'s <see cref="IRenderTarget.Owner"/> is not the same as this object's <see cref="Owner"/></exception>
    public bool Add(IRenderTarget item) 
        => item.Owner != Owner ? throw new ArgumentException("The Owner of item is not the same as this list's owner", nameof(item)) : hashset.Add(item);

    /// <summary>
    /// Removes <paramref name="item"/> from this list
    /// </summary>
    /// <param name="item">The item to remove</param>
    /// <returns><see langword="true"/> if <paramref name="item"/> was succesfully remove, <see langword="false"/> otherwise</returns>
    public bool Remove(IRenderTarget item)
        => hashset.Remove(item);

    /// <summary>
    /// Clears this list from all <see cref="IRenderTarget"/>s
    /// </summary>
    public void Clear()
        => hashset.Clear();

    /// <summary>
    /// The amount of <see cref="IRenderTarget"/>s contained in this list
    /// </summary>
    public int Count => hashset.Count;

    /// <summary>
    /// Returns an enumerator that iterates through the <see cref="IRenderTarget"/>s in this list
    /// </summary>
    public IEnumerator<IRenderTarget> GetEnumerator()
        => hashset.GetEnumerator();
}