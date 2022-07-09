using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a list of <see cref="DrawOperation"/>s in a <see cref="Game"/>
/// </summary>
/// <remarks>
/// This class cannot be inherited. This class cannot be instanced by user code. <see cref="DrawOperationList"/> is not very performant, and should not be used in hot paths
/// </remarks>
public sealed class DrawOperationList : IReadOnlyCollection<DrawOperation>
{
    private readonly Dictionary<Guid, DrawOperation> Ops = new();
    internal readonly List<DrawOperation> RegistrationBuffer = new();

    #region Public

    /// <summary>
    /// Queries this list for any <see cref="DrawOperation"/>s with an id of <paramref name="id"/>
    /// </summary>
    /// <param name="id">The id to query</param>
    /// <param name="drawOp">The <see cref="DrawOperation"/> that was found, or <c>null</c></param>
    /// <returns><c>true</c> if a <see cref="DrawOperation"/> by <paramref name="id"/> was found, <c>false</c> otherwise</returns>
    public bool TryFind(Guid id, [NotNullWhen(true)] out DrawOperation? drawOp)
    {
        lock (Ops)
            return Ops.TryGetValue(id, out drawOp);
    }

    /// <summary>
    /// Gets the current amount of <see cref="DrawOperation"/> held in this list
    /// </summary>
    public int Count => Ops.Count;

    /// <inheritdoc/>
    public IEnumerator<DrawOperation> GetEnumerator() => Ops.Values.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

    #region Internal

    internal void Remove(DrawOperation dop)
    {
        lock (Ops)
            Ops.Remove(dop.Identifier);
    }

    internal void Add(DrawOperation dop)
    {
        lock (Ops)
            Ops.Add(dop.Identifier, dop);
    }

    #endregion
}
