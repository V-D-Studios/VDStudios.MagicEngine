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
        GraphicsResourceFactory<Texture> textureFactory,
        GraphicsResourceFactory<Sampler> samplerFactory,
        GraphicsResourceFactory<TextureView> viewFactory,
        IVertexGenerator<Vector2, TVertex>? vertexGenerator,
        ElementSkip vertexSkip = default
    )
        : base(shape, game, vertexGenerator, vertexSkip)
    {
        ArgumentNullException.ThrowIfNull(textureFactory);
        ArgumentNullException.ThrowIfNull(samplerFactory);
        ArgumentNullException.ThrowIfNull(viewFactory);
        
        TextureFactory = textureFactory;
        SamplerFactory = samplerFactory;
        ViewFactory = viewFactory;
    }

    private readonly GraphicsResourceFactory<Texture> TextureFactory;
    private readonly GraphicsResourceFactory<Sampler> SamplerFactory;
    private readonly GraphicsResourceFactory<Texture, TextureView> ViewFactory;

    /// <inheritdoc/>
    protected override void CreateGPUResources(VeldridGraphicsContext context)
    {
#warning Create the pipeline, don't call `base`
// Textures are cached, capitalize on that
// Samplers are cached, capitalize on that
    }
}
