using SDL2.NET;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using VDStudios.MagicEngine.DrawLibrary.Geometry;
using VDStudios.MagicEngine.Geometry;
using Veldrid;
using Veldrid.ImageSharp;

namespace VDStudios.MagicEngine.DrawLibrary;

/// <summary>
/// Represents a vertex for a Textured Shape
/// </summary>
public struct TextureVertex<TVertex> where TVertex : unmanaged
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
    /// <param name="texture">The Device Texture for this Renderer. Must be created with <see cref="TextureUsage.Sampled"/></param>
    /// <param name="shapes">The shapes to fill this list with</param>
    /// <param name="description">Provides data for the configuration of this <see cref="TexturedShapeRenderer{TVertex}"/></param>
    /// <param name="vertexGenerator">The <see cref="IShapeRendererVertexGenerator{TVertex}"/> object that will generate the vertices for all shapes in the buffer</param>
    public TexturedShapeRenderer(ImageSharpTexture texture, IEnumerable<ShapeDefinition> shapes, TexturedShapeRenderDescription description, IShapeRendererVertexGenerator<TextureVertex<TVertex>> vertexGenerator) 
        : base(shapes, description.ShapeRenderer, vertexGenerator)
    {
        ArgumentNullException.ThrowIfNull(texture);
        TextureFactory = texture.CreateDeviceTexture;
        TextureRendererDescription = description;
    }

    /// <summary>
    /// Creates a new <see cref="TexturedShapeRenderer{TVertex}"/> object
    /// </summary>
    /// <param name="textureFactory">Represents the method that will create the Device Texture for this Renderer. Must be created with <see cref="TextureUsage.Sampled"/></param>
    /// <param name="shapes">The shapes to fill this list with</param>
    /// <param name="description">Provides data for the configuration of this <see cref="TexturedShapeRenderer{TVertex}"/></param>
    /// <param name="vertexGenerator">The <see cref="IShapeRendererVertexGenerator{TVertex}"/> object that will generate the vertices for all shapes in the buffer</param>
    public TexturedShapeRenderer(TextureFactory textureFactory, IEnumerable<ShapeDefinition> shapes, TexturedShapeRenderDescription description, IShapeRendererVertexGenerator<TextureVertex<TVertex>> vertexGenerator)
        : base(shapes, description.ShapeRenderer, vertexGenerator)
    {
        ArgumentNullException.ThrowIfNull(textureFactory);
        TextureFactory = textureFactory;
        TextureRendererDescription = description;
    }

    #endregion

    #region Resources

    /// <summary>
    /// Represents a transformation matrix that transforms stored texture coordinates to display only a portion of the Texture
    /// </summary>
    public Matrix4x4 TextureViewTransform
    {
        get => TextureViewTransform_field;
        set
        {
            lock (TextureViewTransformSync)
            {
                TextureViewTransform_field = value;
                TextureViewTransformChanged = true;
            }
            NotifyPendingGPUUpdate();
        }
    }
    private readonly object TextureViewTransformSync = new();
    private Matrix4x4 TextureViewTransform_field = Matrix4x4.Identity;
    private bool TextureViewTransformChanged;
    private DeviceBuffer TextureViewTransformBuffer;

    /// <summary>
    /// Be careful when modifying this -- And know that most changes won't have any effect after <see cref="CreateResources(GraphicsDevice, ResourceFactory)"/> is called
    /// </summary>
    protected TexturedShapeRenderDescription TextureRendererDescription;
    private Sampler Sampler;
    private TextureFactory TextureFactory;

    /// <summary>
    /// The texture that this <see cref="TexturedShapeRenderer{TVertex}"/> is in charge of rendering. Will become available after <see cref="CreateResources(GraphicsDevice, ResourceFactory)"/> is called
    /// </summary>
    protected TextureView Texture;

    #endregion

    #region DrawOperation

    /// <inheritdoc/>
    protected override ValueTask CreateWindowSizedResources(GraphicsDevice device, ResourceFactory factory, DeviceBuffer screenSizeBuffer)
    {
        return ValueTask.CompletedTask;
    }

    private ShaderDescription vertexDefault = new(ShaderStages.Vertex, BuiltInResources.DefaultTexturePolygonVertexShader.GetUTF8Bytes(), "main");
    private ShaderDescription fragmnDefault = new(ShaderStages.Fragment, BuiltInResources.DefaultTexturePolygonFragmentShader.GetUTF8Bytes(), "main");
    private static readonly VertexLayoutDescription DefaultVector2TexPosLayout
        = new(
              new VertexElementDescription("TexturePosition", VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate),
              new VertexElementDescription("Position", VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate)
          );

    /// <inheritdoc/>
    protected override async ValueTask CreateResourceSets(GraphicsDevice device, ResourceSetBuilder builder, ResourceFactory factory)
    {
        await base.CreateResourceSets(device, builder, factory);

        var texture = TextureFactory.Invoke(device, factory);
        if (texture is null)
        {
            var exc = new InvalidOperationException($"The TextureFactory for TextureRenderer returned null, rather than a Device Texture");
            Log.Fatal(exc, "A TextureRenderer's TextureFactory failed to create a valid Device Texture");
            throw exc;
        }
        Sampler = factory.CreateSampler(TextureRendererDescription.Sampler);
        Texture = factory.CreateTextureView(new TextureViewDescription()
        {
            BaseMipLevel = TextureRendererDescription.TextureBaseMipLevel ?? 0u,
            MipLevels = TextureRendererDescription.TextureMipLevels ?? texture.MipLevels,
            BaseArrayLayer = TextureRendererDescription.TextureBaseArrayLayer ?? 0u,
            ArrayLayers = TextureRendererDescription.TextureArrayLayers ?? texture.ArrayLayers,
            Format = TextureRendererDescription.TexturePixelFormat ?? texture.Format,
            Target = texture
        });

        TextureViewTransformBuffer = factory.CreateBuffer(new(DataStructuring.FitToUniformBuffer<Matrix4x4>(), BufferUsage.UniformBuffer));
        
        device.UpdateBuffer(TextureViewTransformBuffer, 0, ref TextureViewTransform_field);

        var layout = builder.InsertFirst(out _);
        layout.InsertFirst(new ResourceLayoutElementDescription(
                "TextureViewTransformBuffer",
                ResourceKind.UniformBuffer,
                ShaderStages.Vertex
            ),
            TextureViewTransformBuffer
        );
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
    }

    /// <inheritdoc/>
    protected override ValueTask CreateResources(GraphicsDevice device, ResourceFactory factory, ResourceSet[]? sets, ResourceLayout[]? layouts)
    {
        ShapeRendererDescription.VertexShaderSpirv ??= vertexDefault;
        ShapeRendererDescription.FragmentShaderSpirv ??= fragmnDefault;
        ShapeRendererDescription.VertexLayout ??= DefaultVector2TexPosLayout;
        return base.CreateResources(device, factory, sets, layouts);
    }

    /// <inheritdoc/>
    protected override ValueTask UpdateGPUState(GraphicsDevice device, CommandList commandList, DeviceBuffer screenSizeBuffer)
    {
        lock (TextureViewTransformSync)
            if (TextureViewTransformChanged)
            {
                commandList.UpdateBuffer(TextureViewTransformBuffer, 0, ref TextureViewTransform_field);
                TextureViewTransformChanged = false;
            }
        return base.UpdateGPUState(device, commandList, screenSizeBuffer);
    }

    ///// <inheritdoc/>
    //protected override ValueTask Draw(TimeSpan delta, CommandList cl, GraphicsDevice device, Framebuffer mainBuffer, DeviceBuffer screenSizeBuffer)
    //{
    //}

    #endregion
}
