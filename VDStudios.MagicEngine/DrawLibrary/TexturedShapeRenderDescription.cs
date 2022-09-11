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
    public ShapeRendererDescription ShapeRendererDescription;

    /// <summary>
    /// The description for the Sampler
    /// </summary>
    public SamplerDescription SamplerDescription;

    /// <summary>
    /// A pre-created <see cref="Sampler"/>, to use instead of creating a new one with <see cref="SamplerDescription"/>
    /// </summary>
    public Sampler? Sampler;

    /// <summary>
    /// The description for the TextureView
    /// </summary>
    public TextureViewDescription TextureViewDescription;

    /// <summary>
    /// A pre-created <see cref="TextureView"/> to use instead of creating a new one
    /// </summary>
    public TextureView? TextureView;

    /// <summary>
    /// Instances a new <see cref="TexturedShapeRenderDescription"/>
    /// </summary>
    /// <param name="shapeRenderer"></param>
    /// <param name="sampler"></param>
    /// <param name="textureView"></param>
    public TexturedShapeRenderDescription(ShapeRendererDescription shapeRenderer, Sampler sampler, TextureView textureView)
    {
        ShapeRendererDescription = shapeRenderer;

        ArgumentNullException.ThrowIfNull(sampler);
        ArgumentNullException.ThrowIfNull(textureView);
        Sampler = sampler;
        TextureView = textureView;
    }

    /// <summary>
    /// Instances a new <see cref="TexturedShapeRenderDescription"/>
    /// </summary>
    /// <param name="shapeRenderer"></param>
    /// <param name="sampler"></param>
    /// <param name="textureView"></param>
    public TexturedShapeRenderDescription(ShapeRendererDescription shapeRenderer, SamplerDescription sampler, TextureView textureView)
    {
        ShapeRendererDescription = shapeRenderer;
        SamplerDescription = sampler;

        ArgumentNullException.ThrowIfNull(textureView);
        TextureView = textureView;
    }

    /// <summary>
    /// Instances a new <see cref="TexturedShapeRenderDescription"/>
    /// </summary>
    /// <param name="shapeRenderer"></param>
    /// <param name="sampler"></param>
    /// <param name="textureView"></param>
    public TexturedShapeRenderDescription(ShapeRendererDescription shapeRenderer, Sampler sampler, TextureViewDescription textureView)
    {
        ShapeRendererDescription = shapeRenderer;

        ArgumentNullException.ThrowIfNull(sampler);

        Sampler = sampler;
        TextureViewDescription = textureView;
    }

    /// <summary>
    /// Instances a new <see cref="TexturedShapeRenderDescription"/>
    /// </summary>
    /// <param name="shapeRenderer"></param>
    /// <param name="sampler"></param>
    /// <param name="textureView"></param>
    public TexturedShapeRenderDescription(ShapeRendererDescription shapeRenderer, SamplerDescription sampler, TextureViewDescription textureView)
    {
        ShapeRendererDescription = shapeRenderer;
        SamplerDescription = sampler;
        TextureViewDescription = textureView;
    }
}
