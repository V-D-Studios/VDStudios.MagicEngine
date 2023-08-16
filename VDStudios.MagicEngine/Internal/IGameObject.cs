namespace VDStudios.MagicEngine.Internal;

/// <summary>
/// Represents an object of the <see cref="Game"/>
/// </summary>
/// <remarks>
/// This interface should not be implemented by user code and is meant to use internally only
/// </remarks>
public interface IGameObject : IDisposable
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
    public string? Name { get; init; }

    /// <summary>
    /// Fired right before this <see cref="GameObject"/> is disposed
    /// </summary>
    /// <remarks>
    /// While .NET allows fire-and-forget async methods in these events (<c><see langword="async void"/></c>), this is *NOT* recommended, as it's almost guaranteed the <see cref="GameObject"/> will be fully disposed before the async portion of your code gets a chance to run
    /// </remarks>
    event GeneralGameEvent<GameObject>? AboutToDispose;
}