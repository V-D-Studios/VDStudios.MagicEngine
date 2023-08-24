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
    public Vector2 Position { get; }
}
