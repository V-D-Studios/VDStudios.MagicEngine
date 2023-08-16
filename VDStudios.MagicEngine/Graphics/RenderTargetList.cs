namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// A list that contains <see cref="IRenderTarget{TRenderTargetContext}"/>
/// </summary>
public class RenderTargetList<TGraphicsContext>
    where TGraphicsContext : GraphicsContext<TGraphicsContext>
{
    private readonly HashSet<IRenderTarget<TGraphicsContext>> hashset = new();

    internal RenderTargetList(GraphicsManager<TGraphicsContext> manager)
    {
        Manager = manager ?? throw new ArgumentNullException(nameof(manager));
    }

    /// <summary>
    /// The <see cref="GraphicsManager{TGraphicsContext}"/> that owns and uses this list
    /// </summary>
    public GraphicsManager<TGraphicsContext> Manager { get; }

    /// <summary>
    /// Adds a new <see cref="IRenderTarget{TRenderTargetContext}"/> to this list
    /// </summary>
    /// <param name="item">The item to add</param>
    /// <returns><see langword="true"/> if <paramref name="item"/> was succesfully added, <see langword="false"/> otherwise</returns>
    /// <exception cref="ArgumentException">This exception is thrown if <paramref name="item"/>'s <see cref="IRenderTarget{TRenderTargetContext}.Manager"/> is not the same as this object's <see cref="Manager"/></exception>
    public bool Add(IRenderTarget<TGraphicsContext> item)
        => item.Manager != Manager ? throw new ArgumentException("The Manager of item is not the same as this list's manager", nameof(item)) : hashset.Add(item);

    /// <summary>
    /// Removes <paramref name="item"/> from this list
    /// </summary>
    /// <param name="item">The item to remove</param>
    /// <returns><see langword="true"/> if <paramref name="item"/> was succesfully remove, <see langword="false"/> otherwise</returns>
    public bool Remove(IRenderTarget<TGraphicsContext> item)
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
    public IEnumerator<IRenderTarget<TGraphicsContext>> GetEnumerator()
        => hashset.GetEnumerator();
}