using SDL2.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.SPIRV;

namespace VDStudios.MagicEngine.DrawLibrary;

/// <summary>
/// An operation that renders a Texture at a given position
/// </summary>
public class TextureDrawing : DrawOperation
{
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

    /// <summary>
    /// The actual position at which to draw this <see cref="TextureDrawing"/>
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// The texture to draw
    /// </summary>
    public Texture Texture { get; }

    /// <summary>
    /// Whether this <see cref="TextureDrawing"/> solely owns <see cref="Texture"/>
    /// </summary>
    /// <remarks>
    /// If <c>true</c>, this object will assume all responsibility for <see cref="Texture"/>. If this assumption is broken, unexpected behaviour and/or exceptions may occur. Thus, if <c>true</c>, it's better to copy the <see cref="Texture"/> into another object before manipulating it
    /// </remarks>
    public bool OwnsTexture { get; }

    /// <summary>
    /// Instances a new <see cref="TextureDrawing"/> object
    /// </summary>
    /// <param name="texture">The texture to draw</param>
    /// <param name="ownsTexture">Whether or not this <see cref="TextureDrawing"/> is the sole owner of <paramref name="texture"/>. If <c>true</c>, this object will assume that it's solely responsible for <paramref name="texture"/>; including but not limited to disposing of it when this object is disposed</param>
    /// <param name="relativePosition"></param>
    public TextureDrawing(Texture texture, bool ownsTexture = false, DataDependency<Vector2>? relativePosition = null)
    {
        Texture = texture;
    }

    /// <inheritdoc/>
    protected override ValueTask CreateWindowSizedResources(GraphicsDevice device, ResourceFactory factory, DeviceBuffer screenSizeBuffer)
    {
        WinSize = Manager!.WindowSize;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    protected override ValueTask CreateResources(GraphicsDevice device, ResourceFactory factory)
    {
        NotifyPendingGPUUpdate();
        _shiftBuffer = factory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
        _vertexBuffer = factory.CreateBuffer(new BufferDescription(16 * 4, BufferUsage.VertexBuffer));
        _indexBuffer = factory.CreateBuffer(new BufferDescription(2 * 6, BufferUsage.IndexBuffer));

        _computeShader = factory.CreateFromSpirv(new(
            ShaderStages.Compute,
            BuiltInResources.DefaultTextureComputeShader,
            "main"
        ));

        _computeLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("Tex", ResourceKind.TextureReadWrite, ShaderStages.Compute),
            new ResourceLayoutElementDescription("ScreenSizeBuffer", ResourceKind.UniformBuffer, ShaderStages.Compute),
            new ResourceLayoutElementDescription("ShiftBuffer", ResourceKind.UniformBuffer, ShaderStages.Compute)));

        ComputePipelineDescription computePipelineDesc = new ComputePipelineDescription(
            _computeShader,
            _computeLayout,
            16, 16, 1);

        _computePipeline = factory.CreateComputePipeline(ref computePipelineDesc);

        Shader[] shaders = 
        { 
            factory.CreateFromSpirv(new(
                ShaderStages.Vertex,
                BuiltInResources.DefaultTextureVertexShader,
                "main"
            )),
            factory.CreateFromSpirv(new(
                ShaderStages.Fragment,
                BuiltInResources.DefaultTextureFragmentShader,
                "main"
            )) 
        };

        ShaderSetDescription shaderSet = new ShaderSetDescription(
            new VertexLayoutDescription[]
            {
                    new VertexLayoutDescription(
                        new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                        new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2))
            },
            shaders);

        _graphicsLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("Tex", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            new ResourceLayoutElementDescription("Tex11", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            new ResourceLayoutElementDescription("Tex22", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            new ResourceLayoutElementDescription("SS", ResourceKind.Sampler, ShaderStages.Fragment)));

        GraphicsPipelineDescription fullScreenQuadDesc = new GraphicsPipelineDescription(
            BlendStateDescription.SingleOverrideBlend,
            DepthStencilStateDescription.Disabled,
            new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.Clockwise, true, false),
            PrimitiveTopology.TriangleList,
            shaderSet,
            new[] { _graphicsLayout },
            device.SwapchainFramebuffer.OutputDescription);

        _graphicsPipeline = factory.CreateGraphicsPipeline(ref fullScreenQuadDesc);

        return ValueTask.CompletedTask;
    }

    private Size WinSize;

    /// <inheritdoc/>
    protected override ValueTask Draw(TimeSpan delta, CommandList cl, GraphicsDevice device, Framebuffer mainBuffer, DeviceBuffer screenSizeBuffer)
    {
        _ticks += (float)delta.TotalMilliseconds;
        Vector4 shifts = new(
            WinSize.Width * MathF.Cos(_ticks / 500f), // Red shift
            WinSize.Height * MathF.Sin(_ticks / 1250f), // Green shift
            MathF.Sin(_ticks / 1000f), // Blue shift
            0); // Padding
        cl.UpdateBuffer(_shiftBuffer, 0, ref shifts);

        cl.SetPipeline(_computePipeline);
        cl.SetComputeResourceSet(0, _computeResourceSet);
        cl.Dispatch(_computeTexSize / 16, _computeTexSize / 16, 1);

        cl.SetFramebuffer(mainBuffer);
        cl.SetFullViewports();
        cl.SetFullScissorRects();
        cl.ClearColorTarget(0, RgbaFloat.Black);
        cl.SetPipeline(_graphicsPipeline);
        cl.SetVertexBuffer(0, _vertexBuffer);
        cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
        cl.SetGraphicsResourceSet(0, _graphicsResourceSet);
        cl.DrawIndexed(6, 1, 0, 0, 0);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    protected override ValueTask UpdateGPUState(GraphicsDevice device, CommandList cl, DeviceBuffer screenSizeBuffer)
    {
        cl.UpdateBuffer(screenSizeBuffer, 0, new Vector4(_computeTexSize, _computeTexSize, 0, 0));

        Vector4[] quadVerts =
        {
            new Vector4(-1, 1, 0, 0),
            new Vector4(1, 1, 1, 0),
            new Vector4(1, -1, 1, 1),
            new Vector4(-1, -1, 0, 1),
        };

        ushort[] indices = { 0, 1, 2, 0, 2, 3 };

        cl.UpdateBuffer(_vertexBuffer, 0, quadVerts);
        cl.UpdateBuffer(_indexBuffer, 0, indices);

        return ValueTask.CompletedTask;
    }
}
