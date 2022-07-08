using Serilog;
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
    private readonly static object logSync = new();
    private readonly string Facility;
    private readonly string Area;

    /// <summary>
    /// Instances a new GameObject
    /// </summary>
    internal GameObject(string facility, string area)
    {
        Game = Game.Instance;
        Facility = facility;
        Area = area;
    }

    /// <summary>
    /// A Logger that belongs to this <see cref="GameObject"/> and is attached to <see cref="Game.Log"/>
    /// </summary>
    public ILogger Log
    {
        get
        {
            if (_log is null)
                lock (logSync)
                    if (_log is null)
                        _log = new GameLogger(Game.Log, Area, Facility, GetType());
            return _log;
        }
    }
    private ILogger? _log;

    /// <summary>
    /// The <see cref="MagicEngine.Game"/> this <see cref="GameObject"/> belongs to. Same as<see cref="SDL2.NET.SDLApplication{TApp}.Instance"/>
    /// </summary>
    protected Game Game { get; }
}