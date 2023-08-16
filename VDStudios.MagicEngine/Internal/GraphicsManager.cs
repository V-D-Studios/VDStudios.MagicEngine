using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.Internal;

/// <summary>
/// Represents an abstract GraphicsManager that is not tied to a specific GraphicsContext.
/// </summary>
/// <remarks>
/// This class cannot be instanced outside this library, as it is not meant to be used outside of this library.
/// </remarks>
public abstract class GraphicsManager : GameObject
{
    internal GraphicsManager() : base("Graphics & Input", "Rendering")
    {

    }
}
