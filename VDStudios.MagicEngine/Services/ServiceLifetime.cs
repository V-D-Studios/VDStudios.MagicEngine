using static VDStudios.MagicEngine.Services.ServiceCollection;

namespace VDStudios.MagicEngine.Services;

/// <summary>
/// The lifetime of a Service
/// </summary>
public enum ServiceLifetime
{
    /// <summary>
    /// The Service is created every time it's requested
    /// </summary>
    /// <remarks>
    /// Must be requested only through a <see cref="ServiceScope"/>
    /// </remarks>
    Transient,

    /// <summary>
    /// The Service is created once per scope
    /// </summary>
    /// <remarks>
    /// Must be requested only through a <see cref="ServiceScope"/>
    /// </remarks>
    Scoped,

    /// <summary>
    /// The Service is created once and maintained throughout the lifetime of the <see cref="ServiceCollection"/>
    /// </summary>
    /// <remarks>
    /// Cannot be overriden by cascaded <see cref="ServiceCollection"/>s
    /// </remarks>
    Singleton
}
