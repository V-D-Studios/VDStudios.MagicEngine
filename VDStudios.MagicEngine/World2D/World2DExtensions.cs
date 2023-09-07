using System.Numerics;

namespace VDStudios.MagicEngine.World2D;

/// <summary>
/// Extension methods for <see cref="IWorldMobile2D"/>, <see cref="IWorldObject2D"/> and similar
/// </summary>
public static class World2DExtensions
{
    /// <summary>
    /// Calculates the distance between <paramref name="objectA"/> and <paramref name="objectB"/>
    /// </summary>
    /// <param name="objectA">The other object whose distance from <paramref name="objectB"/> will be evaluated</param>
    /// <param name="objectB">The other object whose distance from <paramref name="objectA"/> will be evaluated</param>
    public static float GetDistance(this IWorldObject2D objectA, IWorldObject2D objectB)
    {
        ArgumentNullException.ThrowIfNull(objectA);
        ArgumentNullException.ThrowIfNull(objectB);
        return Vector2.Distance(objectA.Position, objectB.Position);
    }
}
