using System.Runtime.CompilerServices;

namespace VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes;

/// <summary>
/// Represents a type that is used for graphics, accelerated computing, or other GPU-related tasks
/// </summary>
public interface IGPUType<T> where T : unmanaged
{
    /// <summary>
    /// The size of the defined type
    /// </summary>
    public static abstract int Size { get; }
}
