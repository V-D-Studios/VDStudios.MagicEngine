using SDL2.NET;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System.Collections.Concurrent;
using System.Diagnostics;
using VDStudios.MagicEngine.Exceptions;
using VDStudios.MagicEngine.Logging;

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
    
    internal readonly record struct WindowActionCache(Window Window, WindowAction Action);

    private IGameLifetime? lifetime;
    private bool isStarted;
    private readonly bool isSDLStarted;
    internal ConcurrentQueue<Scene> scenesAwaitingSetup = new();
    internal ConcurrentQueue<GraphicsManager> graphicsManagersAwaitingSetup = new();
    internal ConcurrentQueue<GraphicsManager> graphicsManagersAwaitingDestruction = new();

    static Game()
    {
        SDLAppBuilder.CreateInstance<Game>();
    }

    /// <summary>
    /// Fetches the singleton instance of this <see cref="Game"/>
    /// </summary>
    public static new Game Instance => SDLApplication<Game>.Instance;

    /// <summary>
    /// Instances a new <see cref="Game"/>
    /// </summary>
    public Game()
    {
        Logger = ConfigureLogger(new LoggerConfiguration()).CreateLogger();
#if FEATURE_INTERNAL_LOGGING
        InternalLogger = ConfigureInternalLogger(new LoggerConfiguration()).CreateLogger();
        InternalLog = new GameLogger(InternalLogger, "Game", "Global", "Game Object", GetType());
#endif
        Log = new GameLogger(Logger, "Game", "Global", "Game Object", GetType());
        ActiveGraphicsManagers = new();
        VideoThread = new(VideoRun);
        UpdateFrameThrottle = TimeSpan.FromMilliseconds(5);
    }

    #endregion

    #region Properties

    /// <summary>
    /// Represents the minimum amount of time an update frame can take to complete. If the frame completes in less time, the game will wait until the amount of time has passed
    /// </summary>
    public TimeSpan UpdateFrameThrottle
    {
        get => _uft;
        set
        {
            if (_uft == value)
                return;
            _uft = UpdateFrameThrottleChanging(_uft, value) ?? value;
            _warningTicks = (value * 1.5).Ticks;
        }
    }
    private TimeSpan _uft;
    private long _warningTicks; // This is the amount of average elapsed ticks that would issue a warning
    private long _lastWarningTicks;
    private int _consecutiveWarnings;

    /// <summary>
    /// Gets the total amount of time that has elapsed from the time SDL2 was initialized
    /// </summary>
    public static new TimeSpan TotalTime => TimeSpan.FromTicks(SDL2.Bindings.SDL.SDL_GetTicks());

    /// <summary>
    /// Represents the Main <see cref="GraphicsManager"/> used by the game
    /// </summary>
    public GraphicsManager MainGraphicsManager { get; private set; }

    /// <summary>
    /// Represents all <see cref="GraphicsManager"/>s the <see cref="Game"/> currently has available
    /// </summary>
    /// <remarks>
    /// <see cref="MainGraphicsManager"/> can also be found here
    /// </remarks>
    public GraphicsManagerList ActiveGraphicsManagers { get; }

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

    internal readonly ILogger Logger;
#if FEATURE_INTERNAL_LOGGING
    internal readonly ILogger InternalLogger;
    internal ILogger InternalLog { get; }
#endif

    /// <summary>
    /// Represents the average time per update value calculated while the game is running
    /// </summary>
    /// <remarks>
    /// This value does not represent the <see cref="Game"/>'s FPS, as that is the amount of frames the game outputs per second. This value is only updated while the game is running, also not during <see cref="Load(Progress{float})"/> or any of the other methods
    /// </remarks>
    public TimeSpan AverageDelta => TimeSpan.FromTicks(_mspup.Average);
    private readonly LongAverageKeeper _mspup = new(16);
    
    /// <summary>
    /// The Game's current lifetime. Invalid after it ends and before <see cref="StartGame{TScene}"/> is called
    /// </summary>
    public IGameLifetime Lifetime => lifetime ?? throw new InvalidOperationException("The game has not had its lifetime attached yet");

    /// <summary>
    /// Whether the game has been started or not
    /// </summary>
    public bool IsStarted => isStarted;

    /// <summary>
    /// The current Scene of the <see cref="Game"/>
    /// </summary>
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

    #region Video Processing Thread

    private VideoThreadException? VideoThreadFault;
    private readonly SemaphoreSlim VideoThreadLock = new(1, 1);
    private readonly Thread VideoThread;
    private readonly ConcurrentQueue<Action> actionsToTake = new();
    internal readonly ConcurrentQueue<WindowActionCache> windowActions = new();

    #region Public Methods

    /// <summary>
    /// Queues an action to be executed in the thread that owns all <see cref="Window"/>s
    /// </summary>
    /// <param name="action">The action to take</param>
    public void ExecuteInVideoThread(Action action)
    {
        actionsToTake.Enqueue(action);
    }

    /// <summary>
    /// Queues an action to be executed in the thread that owns all <see cref="Window"/>s and waits for it to complete
    /// </summary>
    /// <param name="action">The action to take</param>
    public void ExecuteInVideoThreadAndWait(Action action)
    {
        SemaphoreSlim sem = new(1, 1);
        sem.Wait();
        actionsToTake.Enqueue(() =>
        {
            try
            {
                action();
            }
            finally
            {
                sem.Release();
            }
        });
        sem.Wait();
        sem.Release();
    }

    /// <summary>
    /// Queues an action to be executed in the thread that owns all <see cref="Window"/>s and asynchronously waits for it to complete
    /// </summary>
    /// <param name="action">The action to take</param>
    public async Task ExecuteInVideoThreadAndWaitAsync(Action action)
    {
        SemaphoreSlim sem = new(1, 1);
        sem.Wait();
        actionsToTake.Enqueue(() =>
        {
            try
            {
                action();
            }
            finally
            {
                sem.Release();
            }
        });
        await sem.WaitAsync();
        sem.Release();
    }

    #endregion

    #region Internal

    private void VideoRun()
    {
        try
        {
            int sleep = (int)TimeSpan.FromSeconds(1d / 60d).TotalMilliseconds;

            VideoThreadLock.Release();
            while (isStarted) 
            {
                if (!actionsToTake.IsEmpty)
                    while (actionsToTake.TryDequeue(out var act))
                        act();

                if (!graphicsManagersAwaitingSetup.IsEmpty)
                    while (graphicsManagersAwaitingSetup.TryDequeue(out var manager))
                    {
                        manager.InternalStart();
                        ActiveGraphicsManagers.Add(manager);
                    }

                if (!graphicsManagersAwaitingDestruction.IsEmpty)
                    while (graphicsManagersAwaitingDestruction.TryDequeue(out var manager))
                    {
                        ActiveGraphicsManagers.Remove(manager);
                        manager.ActuallyDispose();
                    }

                if (!windowActions.IsEmpty)
                    while (windowActions.TryDequeue(out var winact))
                        winact.Action(winact.Window);

                UpdateEvents();

                Thread.Sleep(sleep);
            }
        }
        catch(Exception e)
        {
            VideoThreadFault = new VideoThreadException(e);
            throw;
        }
    }

    #endregion

    #endregion

    #region Methods

    /// <summary>
    /// This method is called automatically when <see cref="UpdateFrameThrottle"/> changes
    /// </summary>
    /// <param name="prevThrottle">The throttle <see cref="UpdateFrameThrottle"/> previously had</param>
    /// <param name="newThrottle">The throttle <see cref="UpdateFrameThrottle"/> is changing into</param>
    /// <returns>A <see cref="TimeSpan"/> object that overrides the newly set value of <see cref="UpdateFrameThrottle"/>, or <c>null</c> to permit the change untouched</returns>
    protected virtual TimeSpan? UpdateFrameThrottleChanging(TimeSpan prevThrottle, TimeSpan newThrottle) => null;

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
    protected virtual GraphicsManager CreateGraphicsManager() => new GraphicsManager(10) { Name = "Main GM" };

    /// <summary>
    /// Loads any required data for the <see cref="Game"/>, and report back the progress at any point in the method with <paramref name="progressTracker"/>
    /// </summary>
    /// <remarks>
    /// Don't call this manually, place here code that should run when loading the game. Is called after <see cref="Preload"/>
    /// </remarks>
    /// <param name="progressTracker">An object that keeps track of progress during the <see cref="Load"/>. Subscribe to its event to use. 0 = 0% - 0.5 = 50% - 1 = 100%</param>
    protected virtual void Load(Progress<float> progressTracker) { }

    /// <summary>
    /// Configures the lifetime of a game
    /// </summary>
    /// <returns>The configured <see cref="IGameLifetime"/></returns>
    /// <remarks>
    /// This method is called right before the <see cref="Game"/> starts running. Theoretically, when everything is already set up. Defaults to <see cref="GameLifeTimeOnWindowCloses"/>
    /// </remarks>
    protected virtual IGameLifetime ConfigureGameLifetime()
    {
        return new GameLifeTimeOnWindowCloses(MainGraphicsManager.Window);
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
    /// <remarks>
    /// The Game's default log message template can be found at <see cref="GameLogger.Template"/>
    /// </remarks>
    protected virtual LoggerConfiguration ConfigureLogger(LoggerConfiguration config)
    {
#if DEBUG
        return config
            .MinimumLevel.Verbose()
            .WriteTo.Console(LogEventLevel.Debug, GameLogger.Template, theme: AnsiConsoleTheme.Literate);
#else
        return config
            .MinimumLevel.Information()
            .WriteTo.Console(LogEventLevel.Information, GameLogger.Template, theme: AnsiConsoleTheme.Literate);
#endif
    }

#if FEATURE_INTERNAL_LOGGING
    /// <summary>
    /// Configures and initializes Serilog's log for internal library logging
    /// </summary>
    /// <remarks>
    /// By default, this method should be left alone unless you have a good reason to override it. The engine was built with <c>FEATURE_INTERNAL_LOGGING</c> set.
    /// </remarks>
#else
    /// <summary>
    /// Configures and initializes Serilog's log for internal library logging
    /// </summary>
    /// <remarks>
    /// By default, this method should be left alone unless you have a good reason to override it. The engine was not built with <c>FEATURE_INTERNAL_LOGGING</c> set and will not call this method during startup.
    /// </remarks>
#endif
    protected virtual LoggerConfiguration ConfigureInternalLogger(LoggerConfiguration config)
#if DEBUG
        => config.MinimumLevel.Verbose().WriteTo
        .Console(LogEventLevel.Debug, GameLogger.Template, theme: AnsiConsoleTheme.Code);
#else
        => config.MinimumLevel.Information().WriteTo
        .Console(LogEventLevel.Information, GameLogger.Template, theme: AnsiConsoleTheme.Code);
#endif

    /// <summary>
    /// Unloads any data that was previously loaded by <see cref="Load"/> when stopping the <see cref="Game"/>
    /// </summary>
    /// <remarks>
    /// Don't call this manually, place here code that should run when unloading the game.
    /// </remarks>
    protected virtual void Unload() { }

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

        Load(Preload());
        
        GameLoaded?.Invoke(this, TotalTime);

        var firstScene = new TScene();
        currentScene = firstScene;

        SetupScenes?.Invoke();

        VideoThreadLock.Wait();
        VideoThread.Start(); // It should be released in this thread

        VideoThreadLock.Wait();
        VideoThreadLock.Release();

        //

        {
            MainGraphicsManager = CreateGraphicsManager();
            MainGraphicsManager.WaitForInit();
            WindowObtained?.Invoke(this, TotalTime, MainGraphicsManager.Window, MainGraphicsManager.Device);
        }

        //

        GameStarting?.Invoke(this, TotalTime);

        Start(firstScene);

        GameStarted?.Invoke(this, TotalTime);

        //

        lifetime = ConfigureGameLifetime();

        LifetimeAttached?.Invoke(this, TotalTime, lifetime);

        //

        await Run(lifetime).ConfigureAwait(false);

        //

        StopScenes?.Invoke();

        GameUnloading?.Invoke(this, TotalTime);

        Unload();

        GameUnloaded?.Invoke(this, TotalTime);

        //

        GameStopping?.Invoke(this, TotalTime);

        Stop();
        isStarted = false;

        GameStopped?.Invoke(this, TotalTime);
    }

#endregion

#region Run

    private async Task Run(IGameLifetime lifetime)
    {
        var sw = new Stopwatch();
        TimeSpan delta = default;
        uint remaining = default;
        ulong frameCount = 0;
        Scene scene;

        var sceneSetupList = new List<ValueTask>(10);

        Log.Information("Entering Main Update Loop");
        while (lifetime.ShouldRun)
        {
            if (VideoThreadFault is VideoThreadException vtfault)
                throw vtfault;

            if (!scenesAwaitingSetup.IsEmpty)
            {
                int scenes = 0;
                while (scenesAwaitingSetup.TryDequeue(out var sc))
                {
                    sceneSetupList.Add(sc.InternalConfigure().Preserve());
                    scenes++;
                }
                for (int i = 0; i < scenes; i++)
                    await sceneSetupList[i].ConfigureAwait(false);
                sceneSetupList.Clear();
            }

            if (nextScene is not null)
            {
                var prev = currentScene!;

                await prev.End(nextScene).ConfigureAwait(false);
                await nextScene.Begin().ConfigureAwait(false);

                currentScene = nextScene;
                nextScene = null;
                SceneChanged?.Invoke(this, TotalTime, currentScene, prev!);
            }

            scene = CurrentScene;

            if (frameCount++ % 100 == 0)
                foreach (var manager in ActiveGraphicsManagers)
                    await manager.AwaitIfFaulted();
#if FEATURE_INTERNAL_LOGGING
            if (frameCount % 16 == 0)
            {
                if (_mspup.Average > _warningTicks)
                {
                    _lastWarningTicks = _mspup.Average;
                    switch (_consecutiveWarnings++)
                    {
                        case 10:
                            InternalLog.Debug(
                                "Average Updates per second has been lagging behind the target of {targetMs} milliseconds per frame for more than 32 frames. Last 16 frames' average milliseconds: {lastMs}",
                                _uft.TotalMilliseconds,
                                TimeSpan.FromTicks(_lastWarningTicks).TotalMilliseconds
                            );
                            break;
                        default:
                            if (_consecutiveWarnings % 30 == 0)
                                InternalLog.Warning(
                                    "Average Updates per second has been lagging behind the target of {targetMs} milliseconds per frame for more than 480 frames. Last 16 frames' average milliseconds: {lastMs}",
                                    _uft.TotalMilliseconds,
                                    TimeSpan.FromTicks(_lastWarningTicks).TotalMilliseconds
                                );
                            break;
                    }
                }
                else if (_consecutiveWarnings > 1)
                {
                    InternalLog.Information(
                        "Average Updates per second dropped to about {lastMs} milliseconds per frame for several dozen frames; lagging behind the target of {targetMs} milliseconds per frame",
                        TimeSpan.FromTicks(_lastWarningTicks).TotalMilliseconds,
                        _uft.TotalMilliseconds
                    );
                    _consecutiveWarnings = 0;
                }
                else if (_consecutiveWarnings is 1)
                {
                    InternalLog.Information(
                        "Average Updates per second briefly dropped to {lastMs} milliseconds per frame; lagging behind the target of {targetMs} milliseconds per frame",
                        TimeSpan.FromTicks(_lastWarningTicks).TotalMilliseconds,
                        _uft.TotalMilliseconds
                    );
                    _consecutiveWarnings = 0;
                }
                else
                    _consecutiveWarnings = 0;
            }
#endif

            await scene.Update(delta).ConfigureAwait(false);
            await scene.RegisterDrawOperations();

            {
                var c = (UpdateFrameThrottle - sw.Elapsed).TotalMilliseconds;
                remaining = c > 0.1 ? (uint)c : 0;
            }
            if (remaining > 0)
                SDL2.Bindings.SDL.SDL_Delay(remaining);
            
            delta = sw.Elapsed;
            _mspup.Push(delta.Ticks);
            
            sw.Restart();
        }

        Log.Information("Exiting Main Update Loop and ending current scene");
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
