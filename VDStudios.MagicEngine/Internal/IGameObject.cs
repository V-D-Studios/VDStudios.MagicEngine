namespace VDStudios.MagicEngine.Internal;

/// <summary>
/// Represents an object of the <see cref="Game"/>
/// </summary>
/// <remarks>
/// This interface should not be implemented by user code and is meant to use internally only
/// </remarks>
public interface IGameObject
{
    /// <summary>
    /// The <see cref="MagicEngine.Game"/> this <see cref="GameObject"/> belongs to.
    /// </summary>
    public Game Game { get; }

    /// <summary>
    /// <see langword="true"/> if this <see cref="GameObject"/> has already been disposed of
    /// </summary>
    public bool IsDisposed { get; }

    /// <summary>
    /// An optional name for debugging purposes
    /// </summary>
    public string? Name { get; }
}