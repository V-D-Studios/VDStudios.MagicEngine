using Veldrid;

namespace VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes.Interfaces;

/// <summary>
/// Represents a type that is used to store a Polygon's vertex information
/// </summary>
public interface IVertexType<T> : IGPUType<T> where T : unmanaged
{
    /// <summary>
    /// Obtains a <see cref="VertexLayoutDescription"/> for this type
    /// </summary>
    /// <returns></returns>
    public static abstract VertexLayoutDescription GetDescription();
}
