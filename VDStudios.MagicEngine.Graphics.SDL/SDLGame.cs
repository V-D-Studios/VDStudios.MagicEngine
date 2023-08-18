using SDL2.Bindings;
using SDL2.NET;
using VDStudios.MagicEngine.Internal;

namespace VDStudios.MagicEngine.Graphics.SDL;

/// <summary>
/// Implements <see cref="Game"/> for usage with SDL
/// </summary>
public class SDLGame : Game
{
    /// <inheritdoc/>
    public SDLGame()
    {
        if (OperatingSystem.IsWindows())
            Hints.DisableThreadNaming.IsEnabled = true;
    }

    /// <inheritdoc/>
    protected override GraphicsManager CreateGraphicsManager()
        => new SDLGraphicsManager(this);

    /// <inheritdoc/>
    protected override IGameLifetime ConfigureGameLifetime()
        => new GameLifeTimeOnWindowCloses(((SDLGraphicsManager)MainGraphicsManager).Window);

    /// <inheritdoc/>
    protected override void Delay(uint millisecondsDelay)
        => SDL2.Bindings.SDL.SDL_Delay(millisecondsDelay);
}