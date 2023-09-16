using System.Buffers;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using VDStudios.MagicEngine.Geometry;
using VDStudios.MagicEngine.Graphics.Veldrid.Generators;
using VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes;
using VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes.Interfaces;
using VDStudios.MagicEngine.Graphics.Veldrid.Properties;
using Veldrid;
using Veldrid.SPIRV;

namespace VDStudios.MagicEngine.Graphics.Veldrid.DrawOperations;

/// <summary>
/// An operation that renders a texture on top of a <see cref="ShapeDefinition2D"/>, using <see cref="VertexTextureColor2D"/> and <see cref="TextureVector2Viewport"/>
/// </summary>
public class TexturedShape2DRenderer : TexturedShape2DRenderer<VertexColor2D, TextureCoordinate2D>
{
    /// <summary>
    /// Creates a new object of type <see cref="Shape2DRenderer{TVertex}"/>
    /// </summary>
    /// <param name="shape"></param>
    /// <param name="game"></param>
    /// <param name="textureFactory"></param>
    /// <param name="samplerFactory"></param>
    /// <param name="viewFactory"></param>
    /// <param name="vertexGenerator">The vertex generator for this instance. If <see langword="null"/>, <see cref="Texture2DFillVertexGenerator.Default"/> will be used</param>
    /// <param name="vertexSkip"></param>
    /// <param name="textureCoordinateGenerator"></param>
    public TexturedShape2DRenderer(
        ShapeDefinition2D shape,
        Game game,
        GraphicsResourceFactory<Texture> textureFactory,
        GraphicsResourceFactory<Sampler> samplerFactory,
        GraphicsResourceFactory<Texture, TextureView> viewFactory,
        IVertexGenerator<Vector2, VertexColor2D>? vertexGenerator = null,
        IVertexGenerator<Vector2, TextureCoordinate2D>? textureCoordinateGenerator = null,
        ElementSkip vertexSkip = default
    ) : base(
        shape, 
        game, 
        textureFactory, 
        samplerFactory, 
        viewFactory, 
        vertexGenerator ?? VertexColor2D.DefaultGenerator, 
        textureCoordinateGenerator ?? TextureCoordinate2D.DefaultGenerator, 
        vertexSkip
    ) { }

    /// <summary>
    /// Fetches or registers (and then fetches) the default shader set for <see cref="Shape2DRenderer"/>
    /// </summary>
    public static Shader[] GetDefaultShaders(IVeldridGraphicsContextResources resources)
        => resources.ShaderCache.GetOrAddResource<TexturedShape2DRenderer>(
            static c => c.ResourceFactory.CreateFromSpirv(
                vertexShaderDescription: new ShaderDescription(
                    ShaderStages.Vertex,
                    Encoding.UTF8.GetBytes(DefaultShaders.DefaultTexturedShape2DRendererVertexShader),
                    "main",
                    true
                ),
                fragmentShaderDescription: new ShaderDescription(
                    ShaderStages.Fragment,
                    Encoding.UTF8.GetBytes(DefaultShaders.DefaultTexturedShapeRendererFragmentShader),
                    "main",
                    true
                )
            ));
}

/// <summary>
/// An operation that renders a texture on top of a <see cref="ShapeDefinition2D"/>
/// </summary>
public class TexturedShape2DRenderer<TVertex, TTextureCoordinate> : Shape2DRenderer<TVertex>
    where TVertex : unmanaged, IVertexType<TVertex>
    where TTextureCoordinate : unmanaged, IVertexType<TTextureCoordinate>
{
    /// <summary>
    /// Creates a new object of type <see cref="Shape2DRenderer{TVertex}"/>
    /// </summary>
    public TexturedShape2DRenderer(
        ShapeDefinition2D shape,
        Game game,
        GraphicsResourceFactory<Texture> textureFactory,
        GraphicsResourceFactory<Sampler> samplerFactory,
        GraphicsResourceFactory<Texture, TextureView> viewFactory,
        IVertexGenerator<Vector2, TVertex>? vertexGenerator,
        IVertexGenerator<Vector2, TTextureCoordinate>? textureCoordinateGenerator,
        ElementSkip vertexSkip = default
    )
        : base(shape, game, vertexGenerator, vertexSkip, true)
    {
        if (textureCoordinateGenerator is not null)
            TextureCoordinateGenerator = textureCoordinateGenerator;
        else if (typeof(TTextureCoordinate).IsAssignableTo(typeof(IDefaultVertexGenerator<Vector2, TTextureCoordinate>)))
        {
            TextureCoordinateGenerator = (IVertexGenerator<Vector2, TTextureCoordinate>)(typeof(TTextureCoordinate)
                .GetProperty("DefaultGenerator")!
                .GetValue(null, null))!;

            Debug.Assert(TextureCoordinateGenerator is not null);
        }
        else
            throw new InvalidOperationException("If a type does not implement IDefaultVertexGenerator, then it VertexGenerator must not be null");

        ArgumentNullException.ThrowIfNull(textureFactory);
        ArgumentNullException.ThrowIfNull(samplerFactory);
        ArgumentNullException.ThrowIfNull(viewFactory);

        ViewFactory = viewFactory;
        TextureFactory = textureFactory;
        SamplerFactory = samplerFactory;
    }

    /// <summary>
    /// The <see cref="IVertexGenerator{TInputVertex, TTextureCoordinate}"/> for this <see cref="TexturedShape2DRenderer{TVertex, TTextureCoordinate}"/>. It will be used when generating the texture vertex buffer info
    /// </summary>
    /// <remarks>
    /// Changing this property will not result in vertices being re-generated, see <see cref="Shape2DRenderer{TVertex}.NotifyPendingVertexRegeneration"/>
    /// </remarks>
    [MemberNotNull(nameof(_vtx))]
    public IVertexGenerator<Vector2, TTextureCoordinate> TextureCoordinateGenerator
    {
        get
        {
            Debug.Assert(_vtx is not null);
            return _vtx;
        }

        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _vtx = value;
        }
    }
    private IVertexGenerator<Vector2, TTextureCoordinate> _vtx;

    /// <summary>
    /// The amount of available texture vertex sets
    /// </summary>
    public uint AvailableTextureCoordinateSets { get; private set; }

    /// <summary>
    /// The index of the texture vertex set to use
    /// </summary>
    public uint TextureCoordinateSet
    {
        get => currentTextureCoordinateSet;
        set
        {
            if (value >= AvailableTextureCoordinateSets)
                throw new ArgumentException("Cannot set the current TextureCoordinate Set to a value above the currently available sets", nameof(value));
            currentTextureCoordinateSet = value;
        }
    }
    private uint currentTextureCoordinateSet;
    private uint textureCoordinateSetSize;

    /// <summary>
    /// The offset that should be used when binding the texture coordinate buffer, to use only the currently selected coordinates
    /// </summary>
    public uint TextureCoordinateSetOffset => currentTextureCoordinateSet * textureCoordinateSetSize;

    private DeviceBuffer? TextureCoordinateBuffer;

    private Texture? Texture;
    private Sampler? Sampler;
    private TextureView? TextureView;

    private readonly GraphicsResourceFactory<Texture> TextureFactory;
    private readonly GraphicsResourceFactory<Sampler> SamplerFactory;
    private readonly GraphicsResourceFactory<Texture, TextureView> ViewFactory;

    private ResourceLayout? TextureLayout;
    private ResourceSet? TextureSet;

    /// <inheritdoc/>
    protected override void VerticesRegenerated(VeldridGraphicsContext context)
    {
        var vertices = Shape.AsSpan();

        var gen = TextureCoordinateGenerator;
        Debug.Assert(gen is not null, "TextureCoordinateGenerator was unexpectedly null at the time of rendering");

        uint vertexSetAmount;
        int totalVertexCount;
        checked
        {
            vertexSetAmount = gen.GetOutputSetAmount(vertices);
            if (textureCoordinateSetSize == 0) throw new InvalidOperationException("The current TextureCoordinateGenerator returned 0 for vertex set amount, there must be at least one available set");
            totalVertexCount = vertices.Length * (int)vertexSetAmount;
        }

        var vertexBufferSizeBytes = (uint)(TTextureCoordinate.Size * totalVertexCount);

        if (TextureCoordinateBuffer is not null && vertexBufferSizeBytes > TextureCoordinateBuffer.SizeInBytes)
        {
            TextureCoordinateBuffer.Dispose();
            TextureCoordinateBuffer = null;
        }

        TextureCoordinateBuffer ??= context.ResourceFactory.CreateBuffer(new BufferDescription()
        {
            SizeInBytes = vertexBufferSizeBytes,
            Usage = BufferUsage.VertexBuffer
        });

        TTextureCoordinate[]? rented = null;
        Span<TTextureCoordinate> graphicVertices = TTextureCoordinate.Size * totalVertexCount > (1024 * 1024)
            ? (rented = ArrayPool<TTextureCoordinate>.Shared.Rent(totalVertexCount)).AsSpan(0, totalVertexCount)
            : (stackalloc TTextureCoordinate[totalVertexCount]);

        try
        {
            gen.Generate(vertices, graphicVertices);
            context.CommandList.UpdateBuffer(TextureCoordinateBuffer, 0, graphicVertices);

            AvailableTextureCoordinateSets = vertexSetAmount;
            if (currentTextureCoordinateSet >= AvailableTextureCoordinateSets)
                currentTextureCoordinateSet = 0;
            textureCoordinateSetSize = (uint)(TTextureCoordinate.Size * vertices.Length);
        }
        finally
        {
            if (rented is not null)
                ArrayPool<TTextureCoordinate>.Shared.Return(rented);
        }
    }

    /// <inheritdoc/>
    protected override void CreateGPUResources(VeldridGraphicsContext context)
    {
        base.CreateGPUResources(context); // The base class was constructed with SkipResourceCreation set to true, this is for the grandbase's (VeldridDrawOperation) resources

        Texture = TextureFactory(context) ?? throw new InvalidOperationException("TextureFactory unexpectedly produced a null result");
        Sampler = SamplerFactory(context) ?? throw new InvalidOperationException("SamplerFactory unexpectedly produced a null result");
        TextureView = ViewFactory(context, Texture) ?? throw new InvalidOperationException("ViewFactory unexpectedly produced a null result");

        Shader[]? shaders;

        if (context.TryGetResourceLayout<TexturedShape2DRenderer<TVertex, TTextureCoordinate>>(out TextureLayout) is false)
            context.RegisterResourceLayout<TexturedShape2DRenderer<TVertex, TTextureCoordinate>>(
                TextureLayout = context.ResourceFactory.CreateResourceLayout(
                    new ResourceLayoutDescription(
                        new ResourceLayoutElementDescription("Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                        new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment),
                        new ResourceLayoutElementDescription("Viewport", ResourceKind.UniformBuffer, ShaderStages.Fragment)
                    )
                )
            , out _);

        TextureSet = context.ResourceFactory.CreateResourceSet(new ResourceSetDescription(TextureLayout, TextureView, Sampler));

        if (this is TexturedShape2DRenderer)
            shaders = TexturedShape2DRenderer.GetDefaultShaders(context);
        else if (context.ShaderCache.TryGetResource<TexturedShape2DRenderer<TVertex, TTextureCoordinate>>(out shaders) is false)
            throw new InvalidOperationException($"Could not find a Shader set for {Helper.BuildTypeNameAsCSharpTypeExpression(typeof(TexturedShape2DRenderer<TVertex, TTextureCoordinate>))}");

        if (context.ContainsPipeline<TexturedShape2DRenderer<TVertex, TTextureCoordinate>>() is false)
            context.RegisterPipeline<TexturedShape2DRenderer<TVertex, TTextureCoordinate>>(context.ResourceFactory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                blendState: BlendStateDescription.SingleAlphaBlend,
                depthStencilStateDescription: new DepthStencilStateDescription(
                    depthTestEnabled: true,
                    depthWriteEnabled: true,
                    comparisonKind: ComparisonKind.LessEqual
                ),
                rasterizerState: new RasterizerStateDescription
                (
                    cullMode: FaceCullMode.Back,
                    fillMode: PolygonFillMode.Solid,
                    frontFace: FrontFace.Clockwise,
                    depthClipEnabled: true,
                    scissorTestEnabled: false
                ),
                primitiveTopology: PrimitiveTopology.TriangleList,
                shaderSet: new ShaderSetDescription(
                    new VertexLayoutDescription[]
                    {
                        TVertex.GetDescription(),

                    },
                    shaders
                ),
                resourceLayouts: new ResourceLayout[]
                {
                    context.FrameReportLayout,
                    context.GetResourceLayout<VeldridRenderTarget>(),
                    context.GetResourceLayout<VeldridDrawOperation>(),
                    TextureLayout
                },
                outputs: context.GraphicsDevice.SwapchainFramebuffer.OutputDescription,
                resourceBindingModel: ResourceBindingModel.Improved
            )), out _);
    }

    /// <inheritdoc/>
    protected override void Draw(TimeSpan delta, VeldridGraphicsContext context, RenderTarget<VeldridGraphicsContext> t)
    {
        Debug.Assert(VertexBuffer is not null, "VertexBuffer was unexpectedly null at the time of drawing");
        Debug.Assert(TextureCoordinateBuffer is not null, "TextureCoordinateBuffer was unexpectedly null at the time of drawing");
        Debug.Assert(IndexBuffer is not null, "IndexBuffer was unexpectedly null at the time of drawing");
        Debug.Assert(t is VeldridRenderTarget, "target is not of type VeldridRenderTarget");
        Debug.Assert(DrawOperationResourceSet is not null, "DrawOperationResourceSet was unexpectedly null at the time of drawing");
        Debug.Assert(TextureSet is not null, "TextureSet was unexpectedly null at the time of drawing");

        var target = (VeldridRenderTarget)t;
        var cl = target.CommandList;

        cl.SetFramebuffer(target.GetFramebuffer(context));
        cl.SetVertexBuffer(0, VertexBuffer, VertexSetOffset);
        cl.SetVertexBuffer(1, TextureCoordinateBuffer, TextureCoordinateSetOffset);
        cl.SetIndexBuffer(IndexBuffer, IndexFormat.UInt16, 0);
        cl.SetPipeline(context.GetPipeline<TexturedShape2DRenderer<TVertex, TTextureCoordinate>>(PipelineIndex));

        cl.SetGraphicsResourceSet(0, context.FrameReportSet);
        cl.SetGraphicsResourceSet(1, target.TransformationSet);
        cl.SetGraphicsResourceSet(2, DrawOperationResourceSet);
        cl.SetGraphicsResourceSet(3, TextureSet);

        cl.DrawIndexed(IndexCount, 1, 0, 0, 0);
    }
}
