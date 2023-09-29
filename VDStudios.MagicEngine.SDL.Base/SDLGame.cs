using SDL2.Bindings;
using VDStudios.MagicEngine.Graphics;

namespace VDStudios.MagicEngine.SDL.Base;

/// <summary>
/// Implements <see cref="Game"/> for usage with SDL
/// </summary>
/// <remarks>
/// If inheriting from this class is not an option, consider using <see cref="SDLGameHelpers"/>
/// </remarks>
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
    /// The base implementation of this method calls <see cref="SDLGameHelpers.ConfigureEnvironment"/>
    /// </remarks>
    protected virtual void ConfigureEnvironment()
        => SDLGameHelpers.ConfigureEnvironment();

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