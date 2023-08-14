using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// A list that contains <see cref="IRenderTarget{TRenderTargetContext}"/>
/// </summary>
public class RenderTargetList<TRenderTargetContext>
    where TRenderTargetContext : IGraphicsContext
{
    private readonly HashSet<IRenderTarget<TRenderTargetContext>> hashset = new();

    internal RenderTargetList(GraphicsManager owner)
    {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    /// <summary>
    /// The <see cref="GraphicsManager"/> that owns and uses this list
    /// </summary>
    public GraphicsManager Owner { get; }

    /// <summary>
    /// Adds a new <see cref="IRenderTarget{TRenderTargetContext}"/> to this list
    /// </summary>
    /// <param name="item">The item to add</param>
    /// <returns><see langword="true"/> if <paramref name="item"/> was succesfully added, <see langword="false"/> otherwise</returns>
    /// <exception cref="ArgumentException">This exception is thrown if <paramref name="item"/>'s <see cref="IRenderTarget{TRenderTargetContext}.Owner"/> is not the same as this object's <see cref="Owner"/></exception>
    public bool Add(IRenderTarget<TRenderTargetContext> item)
        => item.Owner != Owner ? throw new ArgumentException("The Owner of item is not the same as this list's owner", nameof(item)) : hashset.Add(item);

    /// <summary>
    /// Removes <paramref name="item"/> from this list
    /// </summary>
    /// <param name="item">The item to remove</param>
    /// <returns><see langword="true"/> if <paramref name="item"/> was succesfully remove, <see langword="false"/> otherwise</returns>
    public bool Remove(IRenderTarget<TRenderTargetContext> item)
        => hashset.Remove(item);

    /// <summary>
    /// Clears this list from all <see cref="IRenderTarget{TRenderTargetContext}"/>s
    /// </summary>
    public void Clear()
        => hashset.Clear();

    /// <summary>
    /// The amount of <see cref="IRenderTarget{TRenderTargetContext}"/>s contained in this list
    /// </summary>
    public int Count => hashset.Count;

    /// <summary>
    /// Returns an enumerator that iterates through the <see cref="IRenderTarget{TRenderTargetContext}"/>s in this list
    /// </summary>
    public IEnumerator<IRenderTarget<TRenderTargetContext>> GetEnumerator()
        => hashset.GetEnumerator();
}