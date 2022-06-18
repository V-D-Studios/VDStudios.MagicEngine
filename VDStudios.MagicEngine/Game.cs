using Microsoft.Extensions.DependencyInjection;
using SDL2.NET;
using SDL2.NET.SDLMixer;
using Serilog.Events;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace VDStudios.MagicEngine;

/// <summary>
/// The class that contains the most important aspects of MagicEngine's functionality
/// </summary>
/// <remarks>
/// This class takes control of most of SDL's processes. And should be allowed to do so. Follow the docs on how to initialize your <see cref="Game"/>
/// </remarks>
public class Game : SDLApplication<Game>
{
    #region (Standalone) Fields

    private readonly object _lock = new();
    
    internal IServiceProvider? services;
    private IGameLifetime? lifetime;
    private bool isStarted;
    private bool isSDLStarted;

    #endregion

    #region Properties

    /// <summary>
    /// The Game's current lifetime. Invalid after it ends and before <see cref="StartGame{TScene}"/> is called
    /// </summary>
    public IGameLifetime Lifetime => lifetime ?? throw new InvalidOperationException("The game has not had its lifetime attached yet");

    /// <summary>
    /// Whether the game has been started or not
    /// </summary>
    public bool IsStarted => isStarted;

    /// <summary>
    /// The current service provider for this <see cref="Game"/>
    /// </summary>
    /// <remarks>
    /// Invalid until <see cref="ConfigureServices(IServiceCollection)"/> is called by <see cref="StartGame{TScene}"/>, invalid again after the game stops
    /// </remarks>
    public IServiceProvider Services => isStarted ? services! : throw new InvalidOperationException("The game has not been started");

    /// <summary>
    /// The current Scene of the <see cref="Game"/>
    /// </summary>
    /// <remarks>
    /// Invalid until <see cref="ConfigureServices(IServiceCollection)"/> is called by <see cref="StartGame{TScene}"/>, invalid again after the game stops
    /// </remarks>
    public Scene CurrentScene => isStarted ? currentScene! : throw new InvalidOperationException("The game has not been started");
    private Scene? currentScene;

    /// <summary>
    /// Sets the next scene for the <see cref="Game"/>
    /// </summary>
    /// <remarks>
    /// Remember to properly initialize the scene before setting! The <see cref="Game"/> will be expecting a <see cref="Scene"/> that's ready to roll!
    /// </remarks>
    /// <param name="newScene"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void SetScene(Scene newScene)
    {
        ArgumentNullException.ThrowIfNull(newScene);
        if (isStarted is false)
            throw new InvalidOperationException("The game has not been started; set the scene with Game.Start");

        nextScene = newScene;
    }
    private Scene? nextScene;

    #endregion

    #region Methods

    /// <summary>
    /// Creates a new <see cref="IServiceCollection"/> object for the Game
    /// </summary>
    /// <remarks>
    /// This method is only called once, right before <see cref="ConfigureServices(IServiceCollection)"/>. No service configuration should be done here, this method available to allow you to replace the default <see cref="IServiceCollection"/>: <see cref="ServiceCollection"/>
    /// </remarks>
    /// <returns>The newly instanced <see cref="IServiceCollection"/></returns>
    protected virtual IServiceCollection CreateServiceCollection() => new ServiceCollection();

    /// <summary>
    /// Configures the services that are to be used by the <see cref="Game"/>
    /// </summary>
    /// <remarks>Use this method only to configure services, if any require further loading, consider forwarding that to <see cref="Load"/>. Services that the engine depends on are not set here by default, but instead are installed by the framework with their default implementations if not present already. You can freely override those services simply by adding them in this method! This method should only called once during the lifetime of the application, don't call it again</remarks>
    /// <param name="services">The service collection in which to add the services</param>
    protected virtual void ConfigureServices(IServiceCollection services) { }

    /// <summary>
    /// Loads any data that is required before the actual loading of the game
    /// </summary>
    /// <remarks>
    /// Use this if you need to, for example, query and/or fill any runtime-only variables that also happen to be global to the Game. Or simply return a different object than the default <see cref="Progress{T}"/>
    /// </remarks>
    /// <returns>An object that keeps track of progress during the <see cref="Load"/>. 0 = 0% - 0.5 = 50% - 1 = 100%</returns>
    protected virtual Progress<float> Preload() => new();

    /// <summary>
    /// Loads any required data for the <see cref="Game"/>, and report back the progress at any point in the method with <paramref name="progressTracker"/>
    /// </summary>
    /// <remarks>
    /// Don't call this manually, place here code that should run when loading the game. Is called after <see cref="Preload"/>
    /// </remarks>
    /// <param name="progressTracker">An object that keeps track of progress during the <see cref="Load"/>. Subscribe to its event to use. 0 = 0% - 0.5 = 50% - 1 = 100%</param>
    /// <param name="services">The services for this <see cref="Game"/> scoped for this call alone</param>
    protected virtual void Load(Progress<float> progressTracker, IServiceProvider services) { }

    /// <summary>
    /// Configures the lifestime of a game
    /// </summary>
    /// <returns>The configured <see cref="IGameLifetime"/></returns>
    /// <remarks>
    /// Defaults to <see cref="GameLifetime.OnWindowClose"/>
    /// </remarks>
    protected virtual IGameLifetime ConfigureGameLifetime()
        => GameLifetime.OnWindowClose;

    /// <summary>
    /// Sets up SDL's libraries
    /// </summary>
    /// <remarks>This method, by default, calls: <see cref="SDLApplication{TApp}.InitializeVideo"/>, <see cref="SDLApplication{TApp}.InitializeEvents"/>, <see cref="SDLApplication{TApp}.InitializeAndOpenAudioMixer(MixerInitFlags, int, int, int, ushort?)"/> (passing: <see cref="MixerInitFlags.OGG"/> and <see cref="MixerInitFlags.OPUS"/>), and <see cref="SDLApplication{TApp}.InitializeTTF"/></remarks>
    protected virtual void SetupSDL()
    {
        this.InitializeVideo()
            .InitializeEvents()
            .InitializeAndOpenAudioMixer(MixerInitFlags.OGG | MixerInitFlags.OPUS)
            .InitializeTTF();
    }

    /// <summary>
    /// Executes custom logic when starting the game
    /// </summary>
    /// <param name="firstScene">The first scene of the game</param>
    /// <param name="services">The services for this <see cref="Game"/> scoped for this call alone</param>
    /// <remarks>
    /// Don't call this manually, place here code that should run when starting the game. Is called after <see cref="Load"/>
    /// </remarks>
    protected virtual void Start(Scene firstScene, IServiceProvider services) { }

    /// <summary>
    /// Launches SDL's Window for the <see cref="Game"/>
    /// </summary>
    /// <remarks>
    /// It's better if you don't call base <see cref="WindowLaunch"/>
    /// </remarks>
    protected virtual void WindowLaunch()
    {
        LaunchWindow("MagicEngine Game", 800, 600);
    }

    /// <summary>
    /// Configures and initializes Serilog's log
    /// </summary>
    protected virtual LoggerConfiguration ConfigureLogger(LoggerConfiguration config)
        => config.MinimumLevel.Verbose();

    /// <summary>
    /// Unloads any data that was previously loaded by <see cref="Load"/> when stopping the <see cref="Game"/>
    /// </summary>
    /// <param name="services">The services for this <see cref="Game"/> scoped for this call alone</param>
    /// <remarks>
    /// Don't call this manually, place here code that should run when unloading the game.
    /// </remarks>
    protected virtual void Unload(IServiceProvider services) { }

    /// <summary>
    /// Executes custom logic when stopping the game
    /// </summary>
    /// <remarks>
    /// Don't call this manually, place here code that should run when stopping the game. Is called after <see cref="Unload"/>
    /// </remarks>
    protected virtual void Stop() { }

    /// <summary>
    /// Initiates the process of starting the game. Launches the main Renderer and Window if not already created. This method will not return until the <see cref="Game"/>'s <see cref="IGameLifetime"/> ends
    /// </summary>
    /// <typeparam name="TScene">The first scene of the game. It must have a parameterless constructor, and it must be constructed by the <see cref="Game"/>. Later <see cref="Scene"/>s can be constructed manually after the game has started</typeparam>
    /// <remarks>
    /// This method forces concurrency by locking, and will throw if called twice before calling the game is stopped. Still a good idea to call it from the thread that initialized SDL
    /// </remarks>
    public async Task StartGame<TScene>() where TScene : Scene, new()
    {
        var firstScene = new TScene();

        lock (_lock)
        {
            if (isStarted)
                throw new InvalidOperationException("Can't start a game that is already running");
            isStarted = true;

            if (services is null)
                Log.Logger = ConfigureLogger(new LoggerConfiguration()).CreateLogger();

            if (!isSDLStarted)
            {
                SetupSDL();
                isSDLStarted = true;
            }

            if (services is null)
            {
                var serv = CreateServiceCollection();
                ConfigureServices(serv);
                // Put here any default services
                services = serv.BuildServiceProvider(true);
            }
        }

        IServiceScope scope = services.CreateScope();

        //

        GameLoading?.Invoke(this, TotalTime);

        Load(Preload(), scope.ServiceProvider);
        
        GameLoaded?.Invoke(this, TotalTime);

        //

        WindowLaunch();
        WindowObtained?.Invoke(this, TotalTime, MainWindow, MainRenderer);

        //

        GameStarting?.Invoke(this, TotalTime);

        Start(firstScene, scope.ServiceProvider);

        GameStarted?.Invoke(this, TotalTime);

        //

        lifetime = ConfigureGameLifetime();

        LifetimeAttached?.Invoke(this, TotalTime, lifetime);

        //

        await Run(lifetime).ConfigureAwait(true);

        //

        GameUnloading?.Invoke(this, TotalTime);

        IServiceScope unloadScope = services.CreateScope();
        Unload(unloadScope.ServiceProvider);
        unloadScope.Dispose();

        GameUnloaded?.Invoke(this, TotalTime);

        //

        GameStopping?.Invoke(this, TotalTime);

        Stop();

        GameStopped?.Invoke(this, TotalTime);

        scope.Dispose();
    }

    private async Task Run(IGameLifetime lifetime)
    {
        while (lifetime.ShouldRun)
        {
            if (nextScene is not null)
            {
                var prev = currentScene!;

                await prev.End(nextScene).ConfigureAwait(true);

                var scope = services!.CreateScope();
                await nextScene.Begin().ConfigureAwait(true);
                scope.Dispose();

                currentScene = nextScene;
                nextScene = null;
                SceneChanged?.Invoke(this, TotalTime, currentScene, prev!);
            }
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// Fired when the <see cref="Game"/> <see cref="CurrentScene"/> changes
    /// </summary>
    public event GameSceneChangedEvent? SceneChanged;

    /// <summary>
    /// Fired when the first <see cref="CurrentScene"/> of the <see cref="Game"/> is set
    /// </summary>
    /// <remarks>
    /// If you <see cref="Stop"/> and <see cref="Start"/> the game again, this event will be fired again along with it
    /// </remarks>
    public event GameSceneEvent? FirstSceneSet;

    /// <summary>
    /// Fired when the game is being loaded
    /// </summary>
    /// <remarks>
    /// This event is fired before <see cref="Preload"/> is called
    /// </remarks>
    public event GameEvent? GameLoading;

    /// <summary>
    /// Fired when the game has already been loaded
    /// </summary>
    public event GameEvent? GameLoaded;

    /// <summary>
    /// Fired when the game is starting
    /// </summary>
    public event GameEvent? GameStarting;

    /// <summary>
    /// Fired when the game has already started
    /// </summary>
    public event GameEvent? GameStarted;

    /// <summary>
    /// Fired when the game is being unloaded
    /// </summary>
    public event GameEvent? GameUnloading;

    /// <summary>
    /// Fired when the game has already been unloaded
    /// </summary>
    public event GameEvent? GameUnloaded;

    /// <summary>
    /// Fired when the game is being stopped
    /// </summary>
    public event GameEvent? GameStopping;

    /// <summary>
    /// Fired when the game has already stopped
    /// </summary>
    public event GameEvent? GameStopped;

    /// <summary>
    /// Fired when the game has its <see cref="IGameLifetime"/> attached
    /// </summary>
    public event GameLifetimeEvent? LifetimeAttached;

    /// <summary>
    /// Fired when the main <see cref="Window"/> and <see cref="Renderer"/> are created, or found by the <see cref="Game"/>. This will fire before <see cref="GameStarting"/> and after <see cref="GameLoaded"/>
    /// </summary>
    /// <remarks>
    /// The <see cref="Game"/> checks if the <see cref="Window"/> and <see cref="Renderer"/> are created when calling <see cref="StartGame{TScene}"/>, and if so, fires this event. If not, creates them first and then fires this event
    /// </remarks>
    public event GameMainWindowCreatedEvent? WindowObtained;

    #endregion
}
