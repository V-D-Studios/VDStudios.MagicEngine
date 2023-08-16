using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// Represents a list of <see cref="DrawOperation{TGraphicsContext}"/>s in a <see cref="Game"/>
/// </summary>
/// <remarks>
/// This class cannot be inherited. This class cannot be instanced by user code. <see cref="DrawOperationList{TGraphicsContext}"/> is not very performant, and should not be used in hot paths
/// </remarks>
public sealed class DrawOperationList<TGraphicsContext> : IReadOnlyCollection<DrawOperation<TGraphicsContext>>
    where TGraphicsContext : GraphicsContext<TGraphicsContext>
{
    private readonly Dictionary<Guid, DrawOperation<TGraphicsContext>> Ops = new();

    #region Public

    /// <summary>
    /// Queries this list for any <see cref="DrawOperation{TGraphicsContext}"/>s with an id of <paramref name="id"/>
    /// </summary>
    /// <param name="id">The id to query</param>
    /// <param name="drawOp">The <see cref="DrawOperation{TGraphicsContext}"/> that was found, or <c>null</c></param>
    /// <returns><c>true</c> if a <see cref="DrawOperation{TGraphicsContext}"/> by <paramref name="id"/> was found, <c>false</c> otherwise</returns>
    public bool TryFind(Guid id, [NotNullWhen(true)] out DrawOperation<TGraphicsContext>? drawOp)
    {
        lock (Ops)
            return Ops.TryGetValue(id, out drawOp);
    }

    /// <summary>
    /// Gets the current amount of <see cref="DrawOperation{TGraphicsContext}"/> held in this list
    /// </summary>
    public int Count => Ops.Count;

    /// <inheritdoc/>
    public IEnumerator<DrawOperation<TGraphicsContext>> GetEnumerator() => Ops.Values.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

    #region Internal

    internal void Remove(DrawOperation<TGraphicsContext> dop)
    {
        lock (Ops)
            Ops.Remove(dop.Identifier);
    }

    internal void Add(DrawOperation<TGraphicsContext> dop)
    {
        lock (Ops)
            Ops.Add(dop.Identifier, dop);
    }

    #endregion
}
