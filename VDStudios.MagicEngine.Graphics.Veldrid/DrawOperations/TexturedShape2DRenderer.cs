using System.Numerics;
using VDStudios.MagicEngine.Geometry;
using VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes;
using VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes.Interfaces;
using Veldrid;

namespace VDStudios.MagicEngine.Graphics.Veldrid.DrawOperations;

/// <summary>
/// An operation that renders a texture on top of a <see cref="ShapeDefinition2D"/>
/// </summary>
public class TexturedShape2DRenderer<TVertex> : Shape2DRenderer<TVertex>
    where TVertex : unmanaged, IVertexType<TVertex>
{
    /// <summary>
    /// Creates a new object of type <see cref="Shape2DRenderer{TVertex}"/>
    /// </summary>
    public TexturedShape2DRenderer(
        ShapeDefinition2D shape,
        Game game,
        IVertexGenerator<Vector2, TVertex>? vertexGenerator,
        ElementSkip vertexSkip = default
    )
        : base(shape, game, vertexGenerator, vertexSkip)
    {
    }

    private GraphicsResourceFactory<Texture> textureFactory;
    private GraphicsResourceFactory<Sampler> samplerFactory;
    private GraphicsResourceFactory<TextureView> viewFactory;

    protected override void CreateGPUResources(VeldridGraphicsContext context)
    {
#warning Create the pipeline, don't call `base`
// Textures are cached, capitalize on that
// Samplers are cached, capitalize on that
    }
}
