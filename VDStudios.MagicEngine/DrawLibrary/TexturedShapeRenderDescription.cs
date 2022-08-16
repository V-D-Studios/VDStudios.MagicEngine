using VDStudios.MagicEngine.DrawLibrary.Geometry;
using Veldrid;

namespace VDStudios.MagicEngine.DrawLibrary;

/// <summary>
/// Represents a description to configure
/// </summary>
public struct TexturedShapeRenderDescription
{
    /// <summary>
    /// The Description for the backing <see cref="ShapeRenderer{TVertex}"/>
    /// </summary>
    public ShapeRendererDescription ShapeRenderer;

    /// <summary>
    /// The description for the Sampler
    /// </summary>
    public SamplerDescription Sampler;
}
