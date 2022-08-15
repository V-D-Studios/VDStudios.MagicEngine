using VDStudios.MagicEngine.DrawLibrary.Geometry;
using Veldrid;

namespace VDStudios.MagicEngine.DrawLibrary;

/// <summary>
/// Represents a description to configure
/// </summary>
public readonly struct TexturedShapeRenderDescription
{
    /// <summary>
    /// The Description for the backing <see cref="ShapeRenderer{TVertex}"/>
    /// </summary>
    public ShapeRendererDescription ShapeRenderer { get; }

    /// <summary>
    /// The description for the Sampler
    /// </summary>
    public SamplerDescription Sampler { get; }
}
