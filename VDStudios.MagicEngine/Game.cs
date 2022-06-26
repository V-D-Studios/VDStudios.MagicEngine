﻿using Microsoft.Extensions.DependencyInjection;
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
using VDStudios.MagicEngine.Internal;
using System.Numerics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using Veldrid;
using VeldridPixelFormat = Veldrid.PixelFormat;

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
    
    internal IServiceProvider services;
    private IGameLifetime? lifetime;
    private bool isStarted;
    private bool isSDLStarted;
    internal ConcurrentQueue<Scene> scenesAwaitingSetup = new();
    internal ConcurrentQueue<GraphicsManager> graphicsManagersAwaitingSetup = new();

    internal IServiceScope NewScope()
        => services.CreateScope();

    static Game()
    {
        SDLAppBuilder.CreateInstance<Game>();
    }

    /// <summary>
    /// Fetches the singleton instance of this <see cref="Game"/>
    /// </summary>
    new static public Game Instance => SDLApplication<Game>.Instance;

    /// <summary>
    /// Instances a new <see cref="Game"/>
    /// </summary>
    /// <remarks>
    /// This method calls <see cref="ConfigureServices(IServiceCollection)"/>, <see cref="SetupSDL"/>, <see cref="ConfigureLogger(LoggerConfiguration)"/> and <see cref="CreateServiceCollection"/>
    /// </remarks>
    public Game()
    {
        if (!isSDLStarted)
        {
            SetupSDL();
            isSDLStarted = true;
        }

        Log = ConfigureLogger(new LoggerConfiguration()).CreateLogger();
        var serv = CreateServiceCollection();
        ConfigureServices(serv);
        // Put here any default services
        services = serv.BuildServiceProvider(true);
    }

    #endregion

    #region Properties

    /// <summary>
    /// Represents the Main <see cref="GraphicsManager"/> used by the game
    /// </summary>
    public GraphicsManager MainGraphicsManager { get; private set; }

    /// <summary>
    /// The current title of the game
    /// </summary>
    /// <remarks>
    /// Defaults to "Magic Engine Game"
    /// </remarks>
    public string GameTitle
    {
        get => gameTitle;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            var prev = gameTitle;
            gameTitle = value;
            GameTitleChanged?.Invoke(this, TotalTime, value, prev);
        }
    }
    private string gameTitle = "Magic Engine Game";

    /// <summary>
    /// A Logger that belongs to this <see cref="Game"/>
    /// </summary>
    public ILogger Log { get; }

    /// <summary>
    /// Represents the current Updates-per-second value calculated while the game is running
    /// </summary>
    /// <remarks>
    /// This value does not represent the <see cref="Game"/>'s FPS, as that is the amount of frames the game outputs per second. This value is only updated while the game is running, also not during <see cref="Load(Progress{float}, IServiceProvider)"/> or any of the other methods
    /// </remarks>
    public float UPS => _ups;
    private float _ups;

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
    public IServiceProvider Services => isStarted ? services : throw new InvalidOperationException("The game has not been started");

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
    /// Creates and returns the <see cref="GraphicsManager"/> to be used as the <see cref="MainGraphicsManager"/>
    /// </summary>
    /// <param name="scene">The first scene for the game</param>
    protected virtual GraphicsManager CreateGraphicsManager(Scene scene) => new GameGraphicsManager(scene);

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
    /// Configures the lifetime of a game
    /// </summary>
    /// <returns>The configured <see cref="IGameLifetime"/></returns>
    /// <remarks>
    /// This method is called right before the <see cref="Game"/> starts running. Theoretically, when everything is already set up. Defaults to <see cref="GameLifeTimeOnWindowCloses"/>
    /// </remarks>
    protected virtual IGameLifetime ConfigureGameLifetime()
        => new GameLifeTimeOnWindowCloses(MainWindow);

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
    /// <remarks>
    /// Don't call this manually, place here code that should run when starting the game. Is called after <see cref="Load"/>
    /// </remarks>
    protected virtual void Start(Scene firstScene) { }

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
        lock (_lock)
        {
            if (isStarted)
                throw new InvalidOperationException("Can't start a game that is already running");
            isStarted = true;

        }

        //

        GameLoading?.Invoke(this, TotalTime);

        Load(Preload(), Services);
        
        GameLoaded?.Invoke(this, TotalTime);

        var firstScene = new TScene();
        currentScene = firstScene;

        SetupScenes?.Invoke();

        //

        {
            MainGraphicsManager = CreateGraphicsManager(firstScene);
            WindowObtained?.Invoke(this, TotalTime, MainGraphicsManager.Window, MainGraphicsManager.Device);
        }

        //

        GameStarting?.Invoke(this, TotalTime);

        _ups = 0;
        Start(firstScene);
        _ups = 0;

        GameStarted?.Invoke(this, TotalTime);

        //

        lifetime = ConfigureGameLifetime();

        LifetimeAttached?.Invoke(this, TotalTime, lifetime);

        //

        await Run(lifetime).ConfigureAwait(true);

        //

        StopScenes?.Invoke();

        GameUnloading?.Invoke(this, TotalTime);

        IServiceScope unloadScope = services.CreateScope();
        Unload(unloadScope.ServiceProvider);
        unloadScope.Dispose();

        GameUnloaded?.Invoke(this, TotalTime);

        //

        GameStopping?.Invoke(this, TotalTime);

        Stop();

        GameStopped?.Invoke(this, TotalTime);
    }

    private async Task Run(IGameLifetime lifetime)
    {
        var sw = new Stopwatch();
        Scene scene;

        var sceneSetupList = new List<ValueTask>(10);

        while (lifetime.ShouldRun)
        {
            if (!graphicsManagersAwaitingSetup.IsEmpty)
                while (graphicsManagersAwaitingSetup.TryDequeue(out var manager))
                    manager.InternalStart();

            if (!scenesAwaitingSetup.IsEmpty)
            {
                int scenes = 0;
                while (scenesAwaitingSetup.TryDequeue(out var sc))
                {
                    sceneSetupList.Add(sc.ConfigureScene());
                    scenes++;
                }
                for (int i = 0; i < scenes; i++)
                    await sceneSetupList[i];
                sceneSetupList.Clear();
            }

            sw.Restart();
            UpdateEvents();

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

            scene = CurrentScene;

            await scene.Update(sw.Elapsed).ConfigureAwait(true);

            _ups = 1000 / (sw.ElapsedMilliseconds + 0.0000001f);
        }

        await CurrentScene.End();
    }

#endregion

    #region Events

    internal Action? SetupScenes;
    internal Action? StopScenes;

    /// <summary>
    /// Fired when <see cref="Game.GameTitle"/> changes
    /// </summary>
    public event GameTitleChangedEvent? GameTitleChanged;

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
    public event GameLifetimeChangedEvent? LifetimeAttached;

    /// <summary>
    /// Fired when the main <see cref="Window"/> and <see cref="Renderer"/> are created, or found by the <see cref="Game"/>. This will fire before <see cref="GameStarting"/> and after <see cref="GameLoaded"/>
    /// </summary>
    /// <remarks>
    /// The <see cref="Game"/> checks if the <see cref="Window"/> and <see cref="Renderer"/> are created when calling <see cref="StartGame{TScene}"/>, and if so, fires this event. If not, creates them first and then fires this event
    /// </remarks>
    public event GameMainWindowCreatedEvent? WindowObtained;

#endregion
}
