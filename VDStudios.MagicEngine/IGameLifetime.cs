using SDL2.NET;

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
}

/// <summary>
/// Represents the default lifetime of a game
/// </summary>
public sealed class DefaultGameLifetime : IGameLifetime
{
    /// <inheritdoc/>
    public bool ShouldRun { get; set; }

    /// <inheritdoc/>
    public bool TryStop()
    {
        ShouldRun = false;
        return true;
    }

    /// <summary>
    /// Represents a game lifestime that lasts until <see cref="SDLApplication{TApp}.MainWindow"/> is closed, or <see cref="ShouldRun"/> is set to false
    /// </summary>
    public static IGameLifetime OnWindowClose { get; }
#error not made
}