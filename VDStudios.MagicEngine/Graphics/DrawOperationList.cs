using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// Represents a list of <see cref="DrawOperation{TGraphicsContext}"/>s in a <see cref="Game"/>
/// </summary>
/// <remarks>
/// This class cannot be inherited. This class cannot be instanced by user code.
/// </remarks>
public sealed class DrawOperationList<TGraphicsContext> 
    where TGraphicsContext : GraphicsContext<TGraphicsContext>
{
    private readonly Dictionary<uint, HashSet<DrawOperation<TGraphicsContext>>> Ops = new();
    private readonly Dictionary<DrawOperation<TGraphicsContext>, uint> renderLevels = new(); 

    #region Public

    /// <summary>
    /// The <see cref="GraphicsManager{TGraphicsContext}"/> that owns this <see cref="DrawOperationList{TGraphicsContext}"/>
    /// </summary>
    public GraphicsManager<TGraphicsContext> Manager { get; }

    internal DrawOperationList(GraphicsManager<TGraphicsContext> owner)
    {
        Manager = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    /// <summary>
    /// Gets the current amount of <see cref="DrawOperation{TGraphicsContext}"/> held in this list
    /// </summary>
    public int Count => Ops.Count;

    /// <inheritdoc/>
    public IEnumerable<DrawOperation<TGraphicsContext>> Enumerate(uint renderLevel)
    {
        Manager.ThrowIfRenderLevelNotRegistered(renderLevel);
        return GetHashSet(renderLevel);
    }

    #endregion

    #region Internal

    private HashSet<DrawOperation<TGraphicsContext>> GetHashSet(uint renderLevel)
    {
        HashSet<DrawOperation<TGraphicsContext>> hs;
        if (Ops.ContainsKey(renderLevel) is false)
        {
            Ops.Add(renderLevel, hs = new HashSet<DrawOperation<TGraphicsContext>>());
            return hs;
        }
        return Ops[renderLevel];
    }

    internal void Add(DrawOperation<TGraphicsContext> dop, uint renderLevel)
    {
        lock (Ops)
        {
            dop.VerifyManager(Manager);
            Manager.ThrowIfRenderLevelNotRegistered(renderLevel);
            GetHashSet(renderLevel).Add(dop);
            renderLevels.Add(dop, renderLevel);
        }
    }

    internal void Remove(DrawOperation<TGraphicsContext> dop)
    {
        lock (Ops)
            if (renderLevels.TryGetValue(dop, out uint renderLevel))
            {
                GetHashSet(renderLevel).Remove(dop);
                renderLevels.Remove(dop);
            }
    }

    #endregion
}
