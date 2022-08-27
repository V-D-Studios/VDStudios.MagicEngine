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

    /// <summary>
    /// The PixelFormat for the <see cref="TextureView"/>
    /// </summary>
    public PixelFormat? TexturePixelFormat;

    /// <summary>
    /// The BaseMipLevel for the <see cref="TextureView"/>
    /// </summary>
    public uint? TextureBaseMipLevel;

    /// <summary>
    /// The MipLevels for the <see cref="TextureView"/>
    /// </summary>
    public uint? TextureMipLevels;
    
    /// <summary>
    /// The BaseArrayLayer for the <see cref="TextureView"/>
    /// </summary>
    public uint? TextureBaseArrayLayer;
    
    /// <summary>
    /// The ArrayLayers for the <see cref="TextureView"/>
    /// </summary>
    public uint? TextureArrayLayers;

    /// <summary>
    /// Instances a new <see cref="TexturedShapeRenderDescription"/>
    /// </summary>
    /// <param name="shapeRenderer"></param>
    /// <param name="sampler"></param>
    /// <param name="textureArrayLayers"></param>
    /// <param name="textureBaseArrayLayer"></param>
    /// <param name="textureBaseMipLevel"></param>
    /// <param name="textureMipLevels"></param>
    /// <param name="texturePixelFormat"></param>
    public TexturedShapeRenderDescription(ShapeRendererDescription shapeRenderer, SamplerDescription sampler, PixelFormat? texturePixelFormat = null, uint? textureBaseMipLevel = null, uint? textureMipLevels = null, uint? textureBaseArrayLayer = null, uint? textureArrayLayers = null)
    {
        ShapeRenderer = shapeRenderer;
        Sampler = sampler;
        TexturePixelFormat = texturePixelFormat;
        TextureBaseMipLevel = textureBaseMipLevel;
        TextureMipLevels = textureMipLevels;
        TextureBaseArrayLayer = textureBaseArrayLayer;
        TextureArrayLayers = textureArrayLayers;
    }
}
