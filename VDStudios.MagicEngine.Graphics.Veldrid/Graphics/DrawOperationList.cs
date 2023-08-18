using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace VDStudios.MagicEngine.Graphics.Veldrid.Graphics;

/// <summary>
/// Represents a list of <see cref="VeldridDrawOperation"/>s in a <see cref="Game"/>
/// </summary>
/// <remarks>
/// This class cannot be inherited. This class cannot be instanced by user code. <see cref="DrawOperationList"/> is not very performant, and should not be used in hot paths
/// </remarks>
public sealed class DrawOperationList : IReadOnlyCollection<VeldridDrawOperation>
{
    private readonly Dictionary<Guid, VeldridDrawOperation> Ops = new();
    internal readonly SemaphoreSlim RegistrationSync = new(1, 1);

    #region Public

    /// <summary>
    /// Queries this list for any <see cref="VeldridDrawOperation"/>s with an id of <paramref name="id"/>
    /// </summary>
    /// <param name="id">The id to query</param>
    /// <param name="drawOp">The <see cref="VeldridDrawOperation"/> that was found, or <c>null</c></param>
    /// <returns><c>true</c> if a <see cref="VeldridDrawOperation"/> by <paramref name="id"/> was found, <c>false</c> otherwise</returns>
    public bool TryFind(Guid id, [NotNullWhen(true)] out VeldridDrawOperation? drawOp)
    {
        lock (Ops)
            return Ops.TryGetValue(id, out drawOp);
    }

    /// <summary>
    /// Gets the current amount of <see cref="VeldridDrawOperation"/> held in this list
    /// </summary>
    public int Count => Ops.Count;

    /// <inheritdoc/>
    public IEnumerator<VeldridDrawOperation> GetEnumerator() => Ops.Values.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

    #region Internal

    internal void Remove(VeldridDrawOperation dop)
    {
        lock (Ops)
            Ops.Remove(dop.Identifier);
    }

    internal void Add(VeldridDrawOperation dop)
    {
        lock (Ops)
            Ops.Add(dop.Identifier, dop);
    }

    #endregion
}
