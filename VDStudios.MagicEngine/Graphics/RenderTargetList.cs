namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// A list that contains <see cref="RenderTarget{TRenderTargetContext}"/>
/// </summary>
/// <remarks>
/// This type's mutating methods lock on itself
/// </remarks>
public sealed class RenderTargetList<TGraphicsContext>
    where TGraphicsContext : GraphicsContext<TGraphicsContext>
{
    private readonly HashSet<RenderTarget<TGraphicsContext>> hashset = new();

    internal RenderTargetList(GraphicsManager<TGraphicsContext> manager)
    {
        Manager = manager ?? throw new ArgumentNullException(nameof(manager));
    }

    /// <summary>
    /// The <see cref="GraphicsManager{TGraphicsContext}"/> that owns and uses this list
    /// </summary>
    public GraphicsManager<TGraphicsContext> Manager { get; }

    /// <summary>
    /// Adds a new <see cref="RenderTarget{TRenderTargetContext}"/> to this list
    /// </summary>
    /// <param name="item">The item to add</param>
    /// <returns><see langword="true"/> if <paramref name="item"/> was succesfully added, <see langword="false"/> otherwise</returns>
    /// <exception cref="ArgumentException">This exception is thrown if <paramref name="item"/>'s <see cref="RenderTarget{TRenderTargetContext}.Manager"/> is not the same as this object's <see cref="Manager"/></exception>
    public bool Add(RenderTarget<TGraphicsContext> item)
    {
        lock (this)
            return item.Manager != Manager ? throw new ArgumentException("The Manager of item is not the same as this list's manager", nameof(item)) : hashset.Add(item);
    }

    /// <summary>
    /// Removes <paramref name="item"/> from this list
    /// </summary>
    /// <param name="item">The item to remove</param>
    /// <returns><see langword="true"/> if <paramref name="item"/> was succesfully remove, <see langword="false"/> otherwise</returns>
    public bool Remove(RenderTarget<TGraphicsContext> item)
    {
        lock (this)
            return hashset.Remove(item);
    }

    /// <summary>
    /// Clears this list from all <see cref="RenderTarget{TRenderTargetContext}"/>s
    /// </summary>
    public void Clear()
    {
        lock (this)
            hashset.Clear();
    }

    /// <summary>
    /// The amount of <see cref="RenderTarget{TRenderTargetContext}"/>s contained in this list
    /// </summary>
    public int Count => hashset.Count;

    /// <summary>
    /// Returns an enumerator that iterates through the <see cref="RenderTarget{TRenderTargetContext}"/>s in this list
    /// </summary>
    public IEnumerator<RenderTarget<TGraphicsContext>> GetEnumerator()
        => hashset.GetEnumerator();
}