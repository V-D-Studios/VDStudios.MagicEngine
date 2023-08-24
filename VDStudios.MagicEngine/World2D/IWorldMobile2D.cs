using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.World2D;

/// <summary>
/// Represents a mobile object in a 2D world
/// </summary>
/// <remarks>
/// This interface is useful to provide a standard interface to obtain Speed, direction and position of a world object
/// </remarks>
public interface IWorldMobile2D : IWorldObject2D
{
    /// <summary>
    /// The speed at which the object is moving in the direction of <see cref="Direction"/>
    /// </summary>
    /// <remarks>
    /// The interpretation, or the unit of this value, is completely implementation dependent
    /// </remarks>
    public float Speed { get; }

    /// <summary>
    /// The direction in which the object is moving at <see cref="Speed"/>
    /// </summary>
    public Vector2 Direction { get; }
}
