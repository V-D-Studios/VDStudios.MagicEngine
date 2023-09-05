using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Serilog;
using VDStudios.MagicEngine.Internal;
using VDStudios.MagicEngine.Logging;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents an object of the <see cref="Game"/>
/// </summary>
/// <remarks>
/// This class cannot be inherited directly by user code
/// </remarks>
public abstract class GameObject : IGameObject
{
    private object? ____sync;
    private readonly Lazy<ILogger> logSync;

    /// <inheritdoc/>
    public string Facility { get; }
    
    /// <inheritdoc/>
    public string Area { get; }

    /// <inheritdoc/>
    public GameObjectId Id { get; } = GameObjectId.NewId();

    /// <inheritdoc/>
    public string IdString => idstr ??= Id.ToString()!;
    private string? idstr;

    /// <inheritdoc/>
    public override string ToString()
        => tostr ??= $"{GetGameObjectName()} {IdString}";
    private string? tostr;

    /// <inheritdoc/>
    public string GetGameObjectName()
        => ggonstr ??= $"{(Name is null ? "" : $"{Name}/")}{IGameObject.CreateGameObjectName(this)}";
    private string? ggonstr;

    /// <inheritdoc/>
    public override int GetHashCode()
        => Id.Raw;

    /// <summary>
    /// The synchronization object that belongs to this <see cref="GameObject"/>
    /// </summary>
    /// <remarks>
    /// Lock on this object with care; it may produce deadlocks. More often than not, it's preferrable you use your own private object.
    /// </remarks>
    protected internal object Sync => ____sync ??= new();

    /// <inheritdoc/>
    public string? Name { get; init; }

    /// <summary>
    /// Instances a new GameObject
    /// </summary>
    /// <param name="area">Logging information. The area the GameObject belongs to</param>
    /// <param name="facility">Logging information. The facility the GameObject operates for</param>
    /// <param name="game">The game this <see cref="GameObject"/> belongs to</param>
    public GameObject(Game game, string facility, string area)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentException.ThrowIfNullOrEmpty(area);
        ArgumentException.ThrowIfNullOrEmpty(facility);
        logSync = new(InternalCreateLogger, LazyThreadSafetyMode.ExecutionAndPublication);
        Facility = facility;
        Game = game;
        Area = area;
        GameDeferredCallSchedule = Game.DeferredCallSchedule;
    }

    /// <summary>
    /// A Logger that belongs to this <see cref="GameObject"/> and is attached to <see cref="Game.Logger"/>
    /// </summary>
    protected ILogger Log => logSync.Value;

    /// <summary>
    /// The RandomNumberGenerator for this <see cref="GameObject"/>
    /// </summary>
    /// <remarks>
    /// The very same one as <see cref="Game.Random"/>
    /// </remarks>
    protected Random Random => Game.Random;

    /// <summary>
    /// The Game's <see cref="MagicEngine.DeferredExecutionSchedule"/>, can be used to defer calls in the update thread. Points to the same object as <see cref="Game.DeferredCallSchedule"/>
    /// </summary>
    /// <remarks>
    /// This schedule is updated every game frame, as such, it's subject to update rate drops and its maximum time resolution is <see cref="Game.UpdateFrameThrottle"/>
    /// </remarks>
    protected DeferredExecutionSchedule GameDeferredCallSchedule { get; }

    internal ILogger? InternalLog
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
#if FEATURE_INTERNAL_LOGGING
            lock (logSync)
                return __inlog ??= new GameLogger(Game.InternalLogger, this);
#else
            return null;
#endif
        }
    }

#if FEATURE_INTERNAL_LOGGING
    private GameLogger? __inlog;
#endif

    /// <summary>
    /// Creates a new <see cref="ILogger"/> for this <see cref="GameObject"/>
    /// </summary>
    /// <remarks>
    /// This method is used internally to lazyly initialize <see cref="Log"/>
    /// </remarks>
    /// <param name="gameLogger"></param>
    /// <param name="area"></param>
    /// <param name="facility"></param>
    /// <returns></returns>
    protected virtual ILogger CreateLogger(ILogger gameLogger, string area, string facility)
        => new GameLogger(gameLogger, this);

    /// <inheritdoc/>
    public Game Game { get; }

    private ILogger InternalCreateLogger()
        => CreateLogger(Game.Logger, Area, Facility);

    /// <inheritdoc/>
    public bool IsDisposed { get; internal set; }
}
