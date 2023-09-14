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
/// An operation that renders a texture on top of a <see cref="ShapeDefinition2D"/>, using <see cref="VertexTextureColor2D"/>
/// </summary>
public class TexturedShape2DRenderer : TexturedShape2DRenderer<VertexTextureColor2D>
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
    public TexturedShape2DRenderer(
        ShapeDefinition2D shape,
        Game game,
        GraphicsResourceFactory<Texture> textureFactory,
        GraphicsResourceFactory<Sampler> samplerFactory,
        GraphicsResourceFactory<Texture, TextureView> viewFactory,
        IVertexGenerator<Vector2, VertexTextureColor2D>? vertexGenerator = null,
        ElementSkip vertexSkip = default
    ) : base(shape, game, textureFactory, samplerFactory, viewFactory, vertexGenerator ?? Texture2DFillVertexGenerator.Default, vertexSkip) { }

    /// <summary>
    /// Fetches or registers (and then fetches) the default shader set for <see cref="Shape2DRenderer"/>
    /// </summary>
    public static Shader[] GetDefaultShaders(IVeldridGraphicsContextResources resources)
        => resources.ShaderCache.GetOrAddResource<TexturedShape2DRenderer>(
            static c => c.ResourceFactory.CreateFromSpirv(
                vertexShaderDescription: new ShaderDescription(
                    ShaderStages.Vertex,
                    Encoding.UTF8.GetBytes(DefaultShaders.DefaultShape2DRendererVertexShader),
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
        GraphicsResourceFactory<Texture, TextureView> viewFactory,
        IVertexGenerator<Vector2, TVertex>? vertexGenerator,
        ElementSkip vertexSkip = default
    )
        : base(shape, game, vertexGenerator, vertexSkip, true)
    {
        ArgumentNullException.ThrowIfNull(textureFactory);
        ArgumentNullException.ThrowIfNull(samplerFactory);
        ArgumentNullException.ThrowIfNull(viewFactory);
        
        TextureFactory = textureFactory;
        SamplerFactory = samplerFactory;
        ViewFactory = viewFactory;
    }

    private Texture? Texture;
    private Sampler? Sampler;
    private TextureView? TextureView;

    private readonly GraphicsResourceFactory<Texture> TextureFactory;
    private readonly GraphicsResourceFactory<Sampler> SamplerFactory;
    private readonly GraphicsResourceFactory<Texture, TextureView> ViewFactory;

    private ResourceLayout? TextureLayout;
    private ResourceSet? TextureSet;

    /// <inheritdoc/>
    protected override void CreateGPUResources(VeldridGraphicsContext context)
    {
        base.CreateGPUResources(context); // The base class was constructed with SkipResourceCreation set to true, this is for the grandbase's (VeldridDrawOperation) resources

        Texture = TextureFactory(context) ?? throw new InvalidOperationException("TextureFactory unexpectedly produced a null result");
        Sampler = SamplerFactory(context) ?? throw new InvalidOperationException("SamplerFactory unexpectedly produced a null result");
        TextureView = ViewFactory(context, Texture) ?? throw new InvalidOperationException("ViewFactory unexpectedly produced a null result");

        Shader[]? shaders;

        if (context.TryGetResourceLayout<TexturedShape2DRenderer<TVertex>>(out TextureLayout) is false)
            context.RegisterResourceLayout<TexturedShape2DRenderer<TVertex>>(
                TextureLayout = context.ResourceFactory.CreateResourceLayout(
                    new ResourceLayoutDescription(
                        new ResourceLayoutElementDescription("Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                        new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment)
                    )
                )
            , out _);

        TextureSet = context.ResourceFactory.CreateResourceSet(new ResourceSetDescription(TextureLayout, TextureView, Sampler));

        if (this is TexturedShape2DRenderer)
            shaders = TexturedShape2DRenderer.GetDefaultShaders(context);
        else if (context.ShaderCache.TryGetResource<TexturedShape2DRenderer<TVertex>>(out shaders) is false)
            throw new InvalidOperationException($"Could not find a Shader set for {Helper.BuildTypeNameAsCSharpTypeExpression(typeof(TexturedShape2DRenderer<TVertex>))}");

        if (context.ContainsPipeline<TexturedShape2DRenderer<TVertex>>() is false)
            context.RegisterPipeline<TexturedShape2DRenderer<TVertex>>(context.ResourceFactory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
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
        Debug.Assert(VertexIndexBuffer is not null, "VertexIndexBuffer was unexpectedly null at the time of drawing");
        Debug.Assert(t is VeldridRenderTarget, "target is not of type VeldridRenderTarget");
        Debug.Assert(DrawOperationResourceSet is not null, "DrawOperationResourceSet was unexpectedly null at the time of drawing");
        Debug.Assert(TextureSet is not null, "TextureSet was unexpectedly null at the time of drawing");

        var target = (VeldridRenderTarget)t;
        var cl = target.CommandList;

        cl.SetFramebuffer(target.GetFramebuffer(context));
        cl.SetVertexBuffer(0, VertexIndexBuffer, 0);
        cl.SetIndexBuffer(VertexIndexBuffer, IndexFormat.UInt16, VertexEnd);
        cl.SetPipeline(context.GetPipeline<Shape2DRenderer<TVertex>>(PipelineIndex));

        cl.SetGraphicsResourceSet(0, context.FrameReportSet);
        cl.SetGraphicsResourceSet(1, target.TransformationSet);
        cl.SetGraphicsResourceSet(2, DrawOperationResourceSet);
        cl.SetGraphicsResourceSet(3, TextureSet);

        cl.DrawIndexed(IndexCount, 1, 0, 0, 0);
    }
}
