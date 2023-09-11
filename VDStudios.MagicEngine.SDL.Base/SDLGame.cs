using SDL2.Bindings;
using SDL2.NET;
using VDStudios.MagicEngine.Graphics;

namespace VDStudios.MagicEngine.SDL.Base;

/// <summary>
/// Implements <see cref="Game"/> for usage with SDL
/// </summary>
public class SDLGame : Game
{
    private readonly Func<SDLGame, GraphicsManager> CreateGraphicsManagerFunc;
    private readonly Func<SDLGame, IGameLifetime> ConfigureGameLifetimeFunc;

    /// <inheritdoc/>
    public SDLGame(Func<SDLGame, GraphicsManager> createGraphicsManager, Func<SDLGame, IGameLifetime> configureGameLifetime)
    {
        CreateGraphicsManagerFunc = createGraphicsManager;
        ConfigureGameLifetimeFunc = configureGameLifetime;

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
    }

    /// <inheritdoc/>
    protected override GraphicsManager CreateGraphicsManager()
        => CreateGraphicsManagerFunc(this);

    /// <inheritdoc/>
    protected override IGameLifetime ConfigureGameLifetime()
        => ConfigureGameLifetimeFunc(this);

    /// <inheritdoc/>
    protected override void Delay(uint millisecondsDelay)
        => SDL2.Bindings.SDL.SDL_Delay(millisecondsDelay);
}