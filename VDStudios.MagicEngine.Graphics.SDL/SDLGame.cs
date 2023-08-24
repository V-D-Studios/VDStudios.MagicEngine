using SDL2.Bindings;
using SDL2.NET;

namespace VDStudios.MagicEngine.Graphics.SDL;

/// <summary>
/// Implements <see cref="Game"/> for usage with SDL
/// </summary>
public class SDLGame : Game
{
    /// <inheritdoc/>
    public SDLGame()
    {
        ConfigureEnvironment();
    }

    /// <summary>
    /// Configures the environment surrounding this game
    /// </summary>
    /// <remarks>
    /// The base implementation of this method sets SDL hints and default window flags
    /// </remarks>
    protected virtual void ConfigureEnvironment()
    {
        if (OperatingSystem.IsWindows())
            Hints.DisableThreadNaming.IsEnabled = true;

        WindowConfig.Default.OpenGL(true).Vulkan(false).HighDPI(true);
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