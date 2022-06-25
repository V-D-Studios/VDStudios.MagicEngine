using Serilog;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents an object of the <see cref="Game"/>
/// </summary>
/// <remarks>
/// This class cannot be inherited directly by user code
/// </remarks>
public class GameObject
{
    /// <summary>
    /// Instances a new GameObject
    /// </summary>
    internal GameObject()
    {
        Game = Game.Instance;
    }

    /// <summary>
    /// A Logger that belongs to this <see cref="GameObject"/> and is attached to <see cref="Game.Log"/>
    /// </summary>
    public ILogger Log => Game.Log;

    /// <summary>
    /// The <see cref="MagicEngine.Game"/> this <see cref="GameObject"/> belongs to. Same as<see cref="SDL2.NET.SDLApplication{TApp}.Instance"/>
    /// </summary>
    protected Game Game { get; }
}