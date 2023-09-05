using System.Numerics;

namespace VDStudios.MagicEngine.World2D;

/// <summary>
/// Represents a world object with a 2D Position
/// </summary>
public interface IWorldObject2D
{
    /// <summary>
    /// The position at which the object currently resides
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// The size of the world object
    /// </summary>
    public Vector2 Size { get; set; }

    /// <summary>
    /// The direction that this <see cref="IWorldObject2D"/> is facing
    /// </summary>
    public Vector2 Direction { get; set; }
}
