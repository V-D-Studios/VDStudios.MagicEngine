using SDL2.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.ImageSharp;
using Veldrid.SPIRV;

namespace VDStudios.MagicEngine.DrawLibrary;

/// <summary>
/// An operation that renders a Texture at a given position
/// </summary>
public class TextureDrawing : DrawOperation
{
    #region Private Resources

    private DeviceBuffer _shiftBuffer;
    private DeviceBuffer _vertexBuffer;
    private DeviceBuffer _indexBuffer;
    private Shader _computeShader;
    private ResourceLayout _computeLayout;
    private Pipeline _computePipeline;
    private ResourceSet _computeResourceSet;
    private Pipeline _graphicsPipeline;
    private ResourceSet _graphicsResourceSet;

    private Texture _computeTargetTexture;
    private TextureView _computeTargetTextureView;
    private ResourceLayout _graphicsLayout;
    private float _ticks;
    private uint _computeTexSize = 512;

    #endregion

    #region Properties

    /// <summary>
    /// The actual position at which to draw this <see cref="TextureDrawing"/>
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// The texture to draw
    /// </summary>
    /// <remarks>
    /// If a <see cref="TextureFactory"/> was passed to the constructor, this property will be null until <see cref="GraphicsObject.Manager"/> calls <see cref="CreateResources(GraphicsDevice, ResourceFactory)"/>
    /// </remarks>
    public Texture Texture { get; private set; }
    private TextureFactory? txfactory;

    /// <summary>
    /// Whether this <see cref="TextureDrawing"/> solely owns <see cref="Texture"/>
    /// </summary>
    /// <remarks>
    /// If <c>true</c>, this object will assume all responsibility for <see cref="Texture"/>. If this assumption is broken, unexpected behaviour and/or exceptions may occur. Thus, if <c>true</c>, it's better to copy the <see cref="Texture"/> into another object before manipulating it
    /// </remarks>
    public bool OwnsTexture { get; }

    #endregion

    #region Construction

    /// <summary>
    /// Instances a new <see cref="TextureDrawing"/> object
    /// </summary>
    /// <param name="texture">The texture to draw</param>
    /// <param name="ownsTexture">Whether or not this <see cref="TextureDrawing"/> is the sole owner of <paramref name="texture"/>. If <c>true</c>, this object will assume that it's solely responsible for <paramref name="texture"/>; including but not limited to disposing of it when this object is disposed</param>
    /// <param name="relativePosition"></param>
    public TextureDrawing(Texture texture, bool ownsTexture = false, DataDependency<Vector2>? relativePosition = null)
    {
        ArgumentNullException.ThrowIfNull(texture);
        Texture = texture;
    }

    /// <summary>
    /// Instances a new <see cref="TextureDrawing"/> object
    /// </summary>
    /// <param name="textureFactory">A delegated method that instantiates or retrieves the <see cref="global::Veldrid.Texture"/> to draw</param>
    /// <param name="ownsTexture">Whether or not this <see cref="TextureDrawing"/> is the sole owner of the <see cref="global::Veldrid.Texture"/> produced by <paramref name="textureFactory"/>. If <c>true</c>, this object will assume that it's solely responsible for <paramref name="Texture"/>; including but not limited to disposing of it when this object is disposed</param>
    /// <param name="relativePosition"></param>
    public TextureDrawing(TextureFactory textureFactory, bool ownsTexture = false, DataDependency<Vector2>? relativePosition = null)
    {
        ArgumentNullException.ThrowIfNull(textureFactory);
        txfactory = textureFactory;
    }

    /// <summary>
    /// Instances a new <see cref="TextureDrawing"/> object
    /// </summary>
    /// <param name="texture">An <see cref="ImageSharpTexture"/> ready to be bound to this <see cref="TextureDrawing"/>'s <see cref="GraphicsDevice"/></param>
    /// <param name="ownsTexture">Whether or not this <see cref="TextureDrawing"/> is the sole owner of the <see cref="global::Veldrid.Texture"/> produced by <paramref name="textureFactory"/>. If <c>true</c>, this object will assume that it's solely responsible for <paramref name="Texture"/>; including but not limited to disposing of it when this object is disposed</param>
    /// <param name="relativePosition"></param>
    public TextureDrawing(ImageSharpTexture texture, bool ownsTexture = false, DataDependency<Vector2>? relativePosition = null)
    {
        ArgumentNullException.ThrowIfNull(texture);
        txfactory = texture.CreateDeviceTexture;
    }

    #endregion

    #region Drawing

    /// <inheritdoc/>
    protected override ValueTask CreateWindowSizedResources(GraphicsDevice device, ResourceFactory factory, DeviceBuffer screenSizeBuffer)
    {
        WinSize = Manager!.WindowSize;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    protected override ValueTask CreateResources(GraphicsDevice device, ResourceFactory factory)
    {
        if (Texture is null)
            Texture = txfactory is not null
                ? txfactory(device, factory)
                : throw new NullReferenceException("Both Texture and the Texture Factory in this TextureDrawing are null: This is an invalid state that should be unreachable");

        return ValueTask.CompletedTask;
    }

    private Size WinSize;

    /// <inheritdoc/>
    protected override ValueTask Draw(TimeSpan delta, CommandList cl, GraphicsDevice device, Framebuffer mainBuffer, DeviceBuffer screenSizeBuffer)
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    protected override ValueTask UpdateGPUState(GraphicsDevice device, CommandList cl, DeviceBuffer screenSizeBuffer)
    {
        return ValueTask.CompletedTask;
    }

    #endregion
}
