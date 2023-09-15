using System.Collections.Specialized;
using System.Diagnostics;
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
public class TexturedShape2DRenderer : TexturedShape2DRenderer<VertexTextureColor2D, TextureVector2Viewport>
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
    /// <param name="startingViewport">The starting viewport. Ignored and set to <see cref="Matrix3x2.Identity"/> if <see langword="null"/></param>
    public TexturedShape2DRenderer(
        ShapeDefinition2D shape,
        Game game,
        GraphicsResourceFactory<Texture> textureFactory,
        GraphicsResourceFactory<Sampler> samplerFactory,
        GraphicsResourceFactory<Texture, TextureView> viewFactory,
        IVertexGenerator<Vector2, VertexTextureColor2D>? vertexGenerator = null,
        TextureVector2Viewport? startingViewport = null,
        ElementSkip vertexSkip = default
    ) : base(shape, game, textureFactory, samplerFactory, viewFactory, vertexGenerator ?? Texture2DFillVertexGenerator.Default, startingViewport ?? Matrix3x2.Identity, vertexSkip) { }

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
public class TexturedShape2DRenderer<TVertex, TViewport> : Shape2DRenderer<TVertex>
    where TVertex : unmanaged, IVertexType<TVertex>
    where TViewport : unmanaged, IGPUType<TViewport>
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
        TViewport startingViewport = default,
        ElementSkip vertexSkip = default
    )
        : base(shape, game, vertexGenerator, vertexSkip, true)
    {
        ArgumentNullException.ThrowIfNull(textureFactory);
        ArgumentNullException.ThrowIfNull(samplerFactory);
        ArgumentNullException.ThrowIfNull(viewFactory);

        CurrentView = startingViewport;
        ViewFactory = viewFactory;
        TextureFactory = textureFactory;
        SamplerFactory = samplerFactory;
    }

    /// <summary>
    /// The current viewport of the <see cref="TexturedShape2DRenderer{TVertex, TViewport}"/>
    /// </summary>
    public TViewport CurrentView
    {
        get => viewport;
        set
        {
            if (viewport.Equals(value)) return;
            viewport = value;
            viewportChanged = true;
            NotifyPendingGPUUpdate();
        }
    }
    private TViewport viewport;
    private bool viewportChanged;

    private Texture? Texture;
    private Sampler? Sampler;
    private TextureView? TextureView;
    private DeviceBuffer? ViewportBuffer;

    private readonly GraphicsResourceFactory<Texture> TextureFactory;
    private readonly GraphicsResourceFactory<Sampler> SamplerFactory;
    private readonly GraphicsResourceFactory<Texture, TextureView> ViewFactory;

    private ResourceLayout? TextureLayout;
    private ResourceSet? TextureSet;

    /// <inheritdoc/>
    protected override void UpdateGPUState(VeldridGraphicsContext context)
    {
        base.UpdateGPUState(context);

        if (viewportChanged)
        {
            Debug.Assert(ViewportBuffer is not null, "ViewportBuffer was unexpectedly null at the time of updating the GPU's state");
            context.CommandList.UpdateBuffer(ViewportBuffer, 0, viewport);
            viewportChanged = false;
        }
    }

    /// <inheritdoc/>
    protected override void CreateGPUResources(VeldridGraphicsContext context)
    {
        base.CreateGPUResources(context); // The base class was constructed with SkipResourceCreation set to true, this is for the grandbase's (VeldridDrawOperation) resources

        Texture = TextureFactory(context) ?? throw new InvalidOperationException("TextureFactory unexpectedly produced a null result");
        Sampler = SamplerFactory(context) ?? throw new InvalidOperationException("SamplerFactory unexpectedly produced a null result");
        TextureView = ViewFactory(context, Texture) ?? throw new InvalidOperationException("ViewFactory unexpectedly produced a null result");

        ViewportBuffer = context.ResourceFactory.CreateBuffer(new BufferDescription(DataStructuring.FitToUniformBuffer<TViewport, uint>(), BufferUsage.UniformBuffer));

        Shader[]? shaders;

        if (context.TryGetResourceLayout<TexturedShape2DRenderer<TVertex, TViewport>>(out TextureLayout) is false)
            context.RegisterResourceLayout<TexturedShape2DRenderer<TVertex, TViewport>>(
                TextureLayout = context.ResourceFactory.CreateResourceLayout(
                    new ResourceLayoutDescription(
                        new ResourceLayoutElementDescription("Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                        new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment),
                        new ResourceLayoutElementDescription("Viewport", ResourceKind.UniformBuffer, ShaderStages.Fragment)
                    )
                )
            , out _);

        TextureSet = context.ResourceFactory.CreateResourceSet(new ResourceSetDescription(TextureLayout, TextureView, Sampler, ViewportBuffer));

        if (this is TexturedShape2DRenderer)
            shaders = TexturedShape2DRenderer.GetDefaultShaders(context);
        else if (context.ShaderCache.TryGetResource<TexturedShape2DRenderer<TVertex, TViewport>>(out shaders) is false)
            throw new InvalidOperationException($"Could not find a Shader set for {Helper.BuildTypeNameAsCSharpTypeExpression(typeof(TexturedShape2DRenderer<TVertex, TViewport>))}");

        if (context.ContainsPipeline<TexturedShape2DRenderer<TVertex, TViewport>>() is false)
            context.RegisterPipeline<TexturedShape2DRenderer<TVertex, TViewport>>(context.ResourceFactory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
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
        Debug.Assert(IndexBuffer is not null, "IndexBuffer was unexpectedly null at the time of drawing");
        Debug.Assert(t is VeldridRenderTarget, "target is not of type VeldridRenderTarget");
        Debug.Assert(DrawOperationResourceSet is not null, "DrawOperationResourceSet was unexpectedly null at the time of drawing");
        Debug.Assert(TextureSet is not null, "TextureSet was unexpectedly null at the time of drawing");

        var target = (VeldridRenderTarget)t;
        var cl = target.CommandList;

        cl.SetFramebuffer(target.GetFramebuffer(context));
        cl.SetVertexBuffer(0, VertexBuffer, 0);
        cl.SetIndexBuffer(IndexBuffer, IndexFormat.UInt16, 0);
        cl.SetPipeline(context.GetPipeline<TexturedShape2DRenderer<TVertex, TViewport>>(PipelineIndex));

        cl.SetGraphicsResourceSet(0, context.FrameReportSet);
        cl.SetGraphicsResourceSet(1, target.TransformationSet);
        cl.SetGraphicsResourceSet(2, DrawOperationResourceSet);
        cl.SetGraphicsResourceSet(3, TextureSet);

        cl.DrawIndexed(IndexCount, 1, 0, 0, 0);
#warning Use the index offset
    }
}
