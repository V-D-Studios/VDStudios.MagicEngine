using System.Numerics;
using VDStudios.MagicEngine.DrawLibrary.Geometry;
using VDStudios.MagicEngine.Geometry;
using Veldrid;
using Veldrid.ImageSharp;
using Veldrid.SPIRV;

namespace VDStudios.MagicEngine.DrawLibrary;

/// <summary>
/// Represents a vertex for a Textured Shape
/// </summary>
public readonly struct TextureVertex<TVertex> where TVertex : unmanaged
{
    /// <summary>
    /// The position of the actual vertex in relation to the Texture
    /// </summary>
    public Vector2 TextureCoordinate { get; }

    /// <summary>
    /// The vertex data encapsulated in this <see cref="TexturedShapeRenderer{TVertex}"/>
    /// </summary>
    public TVertex Vertex { get; }

    /// <summary>
    /// Creates a new instance of <see cref="TextureVertex{TVertex}"/>
    /// </summary>
    /// <param name="textureCoordinate">The texture coordinate the vertex represents</param>
    /// <param name="vertex">The actual vertex data</param>
    public TextureVertex(Vector2 textureCoordinate, TVertex vertex)
    {
        TextureCoordinate = textureCoordinate;
        Vertex = vertex;
    }
}

/// <summary>
/// A draw operation that renders a Texture onto the screen
/// </summary>
public class TexturedShapeRenderer<TVertex> : ShapeRenderer<TextureVertex<TVertex>> where TVertex : unmanaged
{
    #region Construction

    /// <summary>
    /// Creates a new <see cref="TexturedShapeRenderer{TVertex}"/> object
    /// </summary>
    /// <param name="imgSharpTexture">Represents an ImageSharpTexture that will create a DeviceTexture for use with this <see cref="TexturedShapeRenderer{TVertex}"/>, taking precedence for the one set in the description, if any</param>
    /// <param name="shapes">The shapes to fill this list with</param>
    /// <param name="description">Provides data for the configuration of this <see cref="TexturedShapeRenderer{TVertex}"/></param>
    /// <param name="vertexGenerator">The <see cref="IShape2DRendererVertexGenerator{TVertex}"/> object that will generate the vertices for all shapes in the buffer</param>
    public TexturedShapeRenderer(ImageSharpTexture imgSharpTexture, IEnumerable<ShapeDefinition2D> shapes, TexturedShapeRenderDescription description, IShape2DRendererVertexGenerator<TextureVertex<TVertex>> vertexGenerator)
        : base(shapes, description.ShapeRendererDescription, vertexGenerator)
    {
        TextureRendererDescription = description;
        ArgumentNullException.ThrowIfNull(imgSharpTexture);
        TextureFactory = imgSharpTexture.CreateDeviceTexture;
    }

    /// <summary>
    /// Creates a new <see cref="TexturedShapeRenderer{TVertex}"/> object
    /// </summary>
    /// <param name="textureFactory">Represents a method that will create a DeviceTexture for use with this <see cref="TexturedShapeRenderer{TVertex}"/>, taking precedence for the one set in the description, if any</param>
    /// <param name="shapes">The shapes to fill this list with</param>
    /// <param name="description">Provides data for the configuration of this <see cref="TexturedShapeRenderer{TVertex}"/></param>
    /// <param name="vertexGenerator">The <see cref="IShape2DRendererVertexGenerator{TVertex}"/> object that will generate the vertices for all shapes in the buffer</param>
    public TexturedShapeRenderer(TextureFactory textureFactory, IEnumerable<ShapeDefinition2D> shapes, TexturedShapeRenderDescription description, IShape2DRendererVertexGenerator<TextureVertex<TVertex>> vertexGenerator)
        : base(shapes, description.ShapeRendererDescription, vertexGenerator)
    {
        TextureRendererDescription = description;
        ArgumentNullException.ThrowIfNull(textureFactory);
        TextureFactory = textureFactory;
    }

    /// <summary>
    /// Creates a new <see cref="TexturedShapeRenderer{TVertex}"/> object
    /// </summary>
    /// <param name="shapes">The shapes to fill this list with</param>
    /// <param name="description">Provides data for the configuration of this <see cref="TexturedShapeRenderer{TVertex}"/></param>
    /// <param name="vertexGenerator">The <see cref="IShape2DRendererVertexGenerator{TVertex}"/> object that will generate the vertices for all shapes in the buffer</param>
    public TexturedShapeRenderer(IEnumerable<ShapeDefinition2D> shapes, TexturedShapeRenderDescription description, IShape2DRendererVertexGenerator<TextureVertex<TVertex>> vertexGenerator) 
        : base(shapes, description.ShapeRendererDescription, vertexGenerator)
    {
        TextureRendererDescription = description;
    }

    #endregion

    #region Resources

    /// <summary>
    /// Be careful when modifying this -- And know that most changes won't have any effect after <see cref="CreateResources(GraphicsDevice, ResourceFactory, ResourceSet[], ResourceLayout[])"/> is called
    /// </summary>
    protected TexturedShapeRenderDescription TextureRendererDescription;

    /// <summary>
    /// The Sampler used by this <see cref="TexturedShapeRenderer{TVertex}"/>
    /// </summary>
    /// <remarks>
    /// Will be <c>null</c> until <see cref="DrawOperation.IsReady"/> is <c>true</c>
    /// </remarks>
    public Sampler Sampler { get; private set; }

    /// <summary>
    /// The texture that this <see cref="TexturedShapeRenderer{TVertex}"/> is in charge of rendering.
    /// </summary>
    /// <remarks>
    /// Will be <c>null</c> until <see cref="DrawOperation.IsReady"/> is <c>true</c>
    /// </remarks>
    public TextureView Texture { get; private set; }
    private TextureFactory? TextureFactory;

    #endregion

    #region DrawOperation

    /// <inheritdoc/>
    protected override async ValueTask CreateResourceSets(GraphicsDevice device, ResourceSetBuilder builder, ResourceFactory factory)
    {
        await base.CreateResourceSets(device, builder, factory);

        Sampler = TextureRendererDescription.Sampler ?? factory.CreateSampler(TextureRendererDescription.SamplerDescription);
        Texture = TextureRendererDescription.TextureView ??
            (TextureFactory is TextureFactory txtf ?
                factory.CreateTextureView(TextureRendererDescription.TextureViewDescription with
                {
                    Target = txtf.Invoke(device, factory)
                }) :
                factory.CreateTextureView(TextureRendererDescription.TextureViewDescription));

        var layout = builder.InsertFirst(out _);
        layout.InsertFirst(new ResourceLayoutElementDescription(
                "Tex",
                ResourceKind.TextureReadOnly,
                ShaderStages.Fragment
            ),
            Texture
        );
        layout.InsertFirst(new ResourceLayoutElementDescription(
                "TSamp",
                ResourceKind.Sampler,
                ShaderStages.Fragment
            ),
            Sampler
        );
        TextureFactory = null;
    }

    /// <summary>
    /// Configures the base
    /// </summary>
    public static void ConfigureDescription(GraphicsManager manager, ResourceFactory factory, ref ShapeRendererDescription description)
    {
        Shader[] shaders = description.Shaders is null
            ? description.VertexShaderSpirv is null && description.FragmentShaderSpirv is null
                ? manager.DefaultResourceCache.DefaultTexturedShapeRendererShaders
                : description.VertexShaderSpirv is null || description.FragmentShaderSpirv is null
                    ? throw new InvalidOperationException("Cannot have only one shader description set. Either they must both be set, or they must both be null")
                    : factory.CreateFromSpirv(
                                    (ShaderDescription)description.VertexShaderSpirv,
                                    (ShaderDescription)description.FragmentShaderSpirv
                                )
            : description.Shaders;

        description.Shaders = shaders;
        description.VertexLayout ??= manager.DefaultResourceCache.DefaultTexturedShapeRendererLayout;
    }

    /// <inheritdoc/>
    protected override ValueTask CreateResources(GraphicsDevice device, ResourceFactory factory, ResourceSet[]? sets, ResourceLayout[]? layouts)
    {
        ConfigureDescription(Manager!, factory, ref ShapeRendererDescription);
        return base.CreateResources(device, factory, sets, layouts);
    }

    #endregion
}
