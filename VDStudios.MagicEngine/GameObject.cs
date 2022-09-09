using Serilog;
using Serilog.Events;
using System.Runtime.CompilerServices;
using VDStudios.MagicEngine.Logging;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents an object of the <see cref="Game"/>
/// </summary>
/// <remarks>
/// This class cannot be inherited directly by user code
/// </remarks>
public abstract class GameObject
{
    private readonly Lazy<ILogger> logSync;
    internal readonly string Facility;
    internal readonly string Area;

    /// <summary>
    /// An optional name for debugging purposes
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Instances a new GameObject
    /// </summary>
    internal GameObject(string facility, string area)
    {
        logSync = new(InternalCreateLogger, LazyThreadSafetyMode.ExecutionAndPublication);
        Game = Game.Instance;
        Facility = facility;
        Area = area;
    }

    /// <summary>
    /// A Logger that belongs to this <see cref="GameObject"/> and is attached to <see cref="Game.Logger"/>
    /// </summary>
    protected ILogger Log => logSync.Value;

    internal ILogger? InternalLog
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
#if FEATURE_INTERNAL_LOGGING
            lock (logSync)
                return __inlog ??= new GameLogger(Game.InternalLogger, Area, Facility, Name, GetType());
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
        => new GameLogger(gameLogger, area, facility, Name, GetType());

    /// <summary>
    /// The <see cref="MagicEngine.Game"/> this <see cref="GameObject"/> belongs to. Same as<see cref="SDL2.NET.SDLApplication{TApp}.Instance"/>
    /// </summary>
    protected Game Game { get; }

    private ILogger InternalCreateLogger()
        => CreateLogger(Game.Logger, Area, Facility);
}
