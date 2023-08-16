namespace VDStudios.MagicEngine;

/// <summary>
/// Represents the lifetime of the game
/// </summary>
public interface IGameLifetime
{
    /// <summary>
    /// As long as this property returns true, the game will update and not stop
    /// </summary>
    /// <remarks>
    /// This property will be queried often, and should be snappy
    /// </remarks>
    public bool ShouldRun { get; }

    /// <summary>
    /// Attempts to end the <see cref="Game"/>'s lifestime
    /// </summary>
    /// <returns>Whether the attempt was succesful or not</returns>
    public bool TryStop();

    /// <summary>
    /// Fired when this <see cref="IGameLifetime"/>'s status changes
    /// </summary>
    public event GameLifetimeEvent? LifetimeChanged;
}

/// <summary>
/// Represents a lifetime of a game that can be manually killed
/// </summary>
public class GameLifetime : IGameLifetime
{
    /// <inheritdoc/>
    public bool ShouldRun
    {
        get => _sr;
        set
        {
            if (_sr == value)
                return;
            _sr = value;

            LifetimeChanged?.Invoke(this, value);
        }
    }
    private bool _sr = true;

    /// <inheritdoc/>
    public bool TryStop()
    {
        ShouldRun = false;
        return true;
    }

    /// <inheritdoc/>
    public event GameLifetimeEvent? LifetimeChanged;
}
