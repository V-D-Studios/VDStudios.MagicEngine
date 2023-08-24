using System.Collections.Concurrent;
using System.Diagnostics;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using VDStudios.MagicEngine.Exceptions;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Internal;
using VDStudios.MagicEngine.Logging;

namespace VDStudios.MagicEngine;

/// <summary>
/// The class that contains the most important aspects of MagicEngine's functionality
/// </summary>
/// <remarks>
/// This class should take control of most, if not all, of any underlying graphics library. And should be allowed to do so. Follow the docs on how to initialize your <see cref="Game"/>
/// </remarks>
public abstract class Game : IGameObject
{
    #region (Standalone) Fields

    private readonly object _lock = new();

    private IGameLifetime? lifetime;
    private bool isStarted;
    internal PriorityQueue<Scene, int> scenesAwaitingSetup = new(5, InvertedIntComparer.Comparer);
    internal ConcurrentQueue<GraphicsManager> graphicsManagersAwaitingSetup = new();

    /// <summary>
    /// Instances a new <see cref="Game"/>
    /// </summary>
    protected Game()
    {
        Logger = ConfigureLogger(new LoggerConfiguration()).CreateLogger();
#if FEATURE_INTERNAL_LOGGING
        InternalLogger = ConfigureInternalLogger(new LoggerConfiguration()).CreateLogger();
        InternalLog = new GameLogger(InternalLogger, "Game", "Global", "Game Object", GetType());
#endif
        Log = new GameLogger(Logger, "Game", "Global", "Game Object", GetType());
        ActiveGraphicsManagers = new();
        UpdateFrameThrottle = TimeSpan.FromMilliseconds(5);
        Random = CreateRNG();
        DeferredCallSchedule = new DeferredExecutionSchedule(out DeferredExecutionScheduleUpdater);

#if FEATURE_INTERNAL_LOGGING
        Log.Debug("Compiled with \"FEATURE_INTERNAL_LOGGING\" enabled");
#endif
#if VALIDATE_USAGE
        Log.Debug("Compiled with \"VALIDATE_USAGE\" enabled");
#endif
#if FORCE_GM_NOPARALLEL
        Log.Debug("Compiled with \"FORCE_GM_NOPARALLEL\" enabled");
#endif
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

    private readonly Stopwatch runtimewatch = new();
    /// <summary>
    /// Gets the total amount of time that has elapsed since the Game started
    /// </summary>
    public TimeSpan TotalTime => runtimewatch.Elapsed;

    private GraphicsManager? mgm;
    /// <summary>
    /// Represents the Main <see cref="GraphicsManager"/> used by the game
    /// </summary>
    public GraphicsManager MainGraphicsManager
    {
        get => mgm ?? throw new InvalidOperationException("Cannot obtain this Game's MainGraphicsManager before its set");
        private set => mgm = value;
    }

    /// <summary>
    /// The Game's <see cref="MagicEngine.DeferredExecutionSchedule"/>, can be used to defer calls in the update thread
    /// </summary>
    /// <remarks>
    /// This schedule is updated every game frame, as such, it's subject to update rate drops and its maximum time resolution is <see cref="UpdateFrameThrottle"/>
    /// </remarks>
    public DeferredExecutionSchedule DeferredCallSchedule { get; }
    private readonly Func<ValueTask> DeferredExecutionScheduleUpdater;

    /// <summary>
    /// Represents all <see cref="GraphicsManager"/>s the <see cref="Game"/> currently has available
    /// </summary>
    /// <remarks>
    /// <see cref="MainGraphicsManager"/> can also be found here
    /// </remarks>
    public GraphicsManagerList ActiveGraphicsManagers { get; }

    #region Dependency Injection

    /// <summary>
    /// The <see cref="ServiceCollection"/> for the entire game
    /// </summary>
    public ServiceCollection GameServices { get; } = new(null);

    #endregion

    /// <summary>
    /// A RandomNumberGenerator for this Game
    /// </summary>
    public Random Random { get; }

    /// <summary>
    /// This method is called automatically when the <see cref="Game"/> is going to instantiate an object for <see cref="Random"/>
    /// </summary>
    /// <remarks>
    /// This method is called from the Game's constructor, do not assume anything else has been initialized.
    /// </remarks>
    protected virtual Random CreateRNG() => Random.Shared;

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
    /// The Game's current lifetime. Invalid after it ends and before <see cref="StartGame"/> is called
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
        GameMismatchException.ThrowIfMismatch(this, newScene);
        if (isStarted is false)
            throw new InvalidOperationException("The game has not been started; set the scene with Game.Start");

        nextScene = newScene;
    }
    private Scene? nextScene;

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
    /// <remarks>
    /// This method is called once when the game is starting
    /// </remarks>
    protected abstract GraphicsManager CreateGraphicsManager();

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
    /// <remarks>
    /// Called after most of everything's been done, just before <see cref="Run(IGameLifetime)"/>
    /// </remarks>
    /// <returns>The configured <see cref="IGameLifetime"/></returns>
    protected abstract IGameLifetime ConfigureGameLifetime();

    /// <summary>
    /// Executes custom logic when starting the game
    /// </summary>
    /// <param name="firstScene">The first scene of the game</param>
    /// <remarks>
    /// Don't call this manually, place here code that should run when starting the game. Is called after <see cref="Load"/>
    /// </remarks>
    protected virtual void Start(Scene firstScene) { }

    /// <summary>
    /// Performs custom game updating logic
    /// </summary>
    /// <remarks>
    /// Use this method with care. Most of the game's functionality is already done by <see cref="Run(IGameLifetime)"/>. This method is called after every scene related task is done and <see cref="DeferredCallSchedule"/> is updated. But before the next delta is measured
    /// </remarks>
    /// <param name="delta">The amount of time the last frame took to complete</param>
    protected virtual ValueTask Updating(TimeSpan delta) => ValueTask.CompletedTask;

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
    /// Delays execution by the specified amount
    /// </summary>
    /// <remarks>
    /// <paramref name="millisecondsDelay"/> will, much more often than not, be very small, in the range of 0-10 milliseconds on average. The function here needs to be both performant, as it'll be called every update frame, and have a very high resolution. For example, this could be replaced with SDL's internal delay function
    /// </remarks>
    /// <param name="millisecondsDelay">The amount of milliseconds to delay for</param>
    protected abstract void Delay(uint millisecondsDelay);

    private Task ProcessPendingGraphicsManagersTask;
    private async Task ProcessPendingGraphicsManagers()
    {
        while (true)
        {
            if (graphicsManagersAwaitingSetup.TryDequeue(out var gm))
                gm.InternalStart();
            await Task.Delay(100);
        }
    }

    private async ValueTask AwaitPendingGraphicsManagersTaskIfFinished()
    {
        if (ProcessPendingGraphicsManagersTask.IsCompleted)
        {
            await ProcessPendingGraphicsManagersTask;
            //throw new GameException("ProcessPendingGraphicsManagersTask stopped unexpectedly. This is likely a library error");
        }
    }

    /// <summary>
    /// Initiates the process of starting the game. Launches the main Renderer and Window if not already created. This method will not return until the <see cref="Game"/>'s <see cref="IGameLifetime"/> ends
    /// </summary>
    /// <remarks>
    /// This method forces concurrency by locking, and will throw if called twice before calling the game is stopped. Still a good idea to call it from the thread that initialized SDL
    /// </remarks>
    public async Task StartGame(SceneFactory sceneFactory)
    {
        ArgumentNullException.ThrowIfNull(sceneFactory);

        lock (_lock)
        {
            if (isStarted)
                throw new InvalidOperationException("Can't start a game that is already running");
            isStarted = true;
        }

        //

        InternalLog?.Information("Loading the Game");
        GameLoading?.Invoke(this, TotalTime);

        Load(Preload());

        InternalLog?.Information("Loaded the game");
        GameLoaded?.Invoke(this, TotalTime);

        var firstScene = sceneFactory(this);
        currentScene = firstScene;

        InternalLog?.Debug("Setting up all scenes");
        SetupScenes?.Invoke();

        //

        {
            InternalLog?.Information("Launching PendingGraphicsManagersTask");
            ProcessPendingGraphicsManagersTask = Task.Run(ProcessPendingGraphicsManagers);

            InternalLog?.Information("Creating MainGraphicsManager");
            MainGraphicsManager = CreateGraphicsManager();
            
            while (true)
            {
                if (await MainGraphicsManager.WaitForInitAsync(100))
                    break;
                await AwaitPendingGraphicsManagersTaskIfFinished();
            }

            InternalLog?.Information("Created MainGraphicsManager");
            MainGraphicsManagerCreated?.Invoke(this, TotalTime, MainGraphicsManager);
        }

        //

        InternalLog?.Information("Starting Game");
        GameStarting?.Invoke(this, TotalTime);

        Start(firstScene);

        InternalLog?.Information("Started Game");
        GameStarted?.Invoke(this, TotalTime);

        //

        InternalLog?.Debug("Configuring Game lifetime");
        lifetime = ConfigureGameLifetime();

        LifetimeAttached?.Invoke(this, TotalTime, lifetime);

        //

        InternalLog?.Information("Running Game");
        runtimewatch.Restart();
        await Run(lifetime).ConfigureAwait(false);
        runtimewatch.Stop();

        //

        StopScenes?.Invoke();

        InternalLog?.Information("Unloading the Game");
        GameUnloading?.Invoke(this, TotalTime);

        Unload();

        InternalLog?.Information("Unloaded the Game");
        GameUnloaded?.Invoke(this, TotalTime);

        //

        GameStopping?.Invoke(this, TotalTime);
        InternalLog?.Information("Stopping the Game");

        Stop();
        isStarted = false;

        InternalLog?.Information("Stopped the Game");
        GameStopped?.Invoke(this, TotalTime);
    }

    #endregion

    #region Run

    /// <summary>
    /// Creates a new <see cref="GameFrameTimer"/> with <paramref name="frameInterval"/> as its lapse
    /// </summary>
    public GameFrameTimer GetFrameTimer(uint frameInterval)
        => new(this, frameInterval);

    internal ulong FrameCount { get; private set; }

    /// <summary>
    /// Runs fundamental game tasks, such as progressing, managing and updating Scenes, GraphicsManagers, keeping track of FPS and deltas, etc.
    /// </summary>
    /// <remarks>
    /// Do *NOT* override this method unless you have a VERY good reason to. Most often than not, you'll need to copy most of the code over from the original source code. Consider overriding <see cref="Updating(TimeSpan)"/> instead
    /// </remarks>
    protected virtual async Task Run(IGameLifetime lifetime)
    {
        var sw = new Stopwatch();
        TimeSpan delta = default;
        uint remaining = default;
        Scene scene;

        var sceneSetupList = new ValueTask[10];

        Log.Information("Entering Main Update Loop");
        while (lifetime.ShouldRun)
        {
            await AwaitPendingGraphicsManagersTaskIfFinished();

            if (scenesAwaitingSetup.Count > 0)
            {
                int scenes = 0;
                lock (scenesAwaitingSetup)
                {
                    if (scenesAwaitingSetup.Count > sceneSetupList.Length)
                        Array.Resize(ref sceneSetupList, int.Max(scenesAwaitingSetup.Count, sceneSetupList.Length * 2));

                    while (scenesAwaitingSetup.TryDequeue(out var sc, out var pr) && scenes < sceneSetupList.Length)
                        sceneSetupList[scenes++] = sc.InternalConfigure().Preserve();
                }
                for (int i = 0; i < scenes; i++)
                    await sceneSetupList[i].ConfigureAwait(false);
                Array.Clear(sceneSetupList);
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

            if (scene.IsBegun is false)
                await scene.Begin().ConfigureAwait(false);

            if (FrameCount++ % 100 == 0)
                foreach (var manager in ActiveGraphicsManagers)
                    await manager.AwaitIfFaulted();
#if FEATURE_INTERNAL_LOGGING
            if (FrameCount % 16 == 0)
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
            await DeferredExecutionScheduleUpdater();

            await Updating(delta);

            {
                var c = (UpdateFrameThrottle - sw.Elapsed).TotalMilliseconds;
                remaining = c > 0.1 ? (uint)c : 0;
            }
            if (remaining > 0)
                Delay(remaining);

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
    /// Fired when the main <see cref="GraphicsManager"/> is created, or found by the <see cref="Game"/>. This will fire before <see cref="GameStarting"/> and after <see cref="GameLoaded"/>
    /// </summary>
    public event GameGraphicsManagerCreatedEvent? MainGraphicsManagerCreated;

    #region IGameObject

    Game IGameObject.Game => this;
    bool IGameObject.IsDisposed => false;
    string? IGameObject.Name => GameTitle;

    #endregion

    #endregion
}
