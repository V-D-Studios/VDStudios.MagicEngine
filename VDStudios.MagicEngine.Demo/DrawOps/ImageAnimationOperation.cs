using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.ImageSharp;
using Veldrid.SPIRV;
using VDStudios.MagicEngine.Demo.ResourceExtensions;

namespace VDStudios.MagicEngine.Demo.DrawOps;
public class ImageAnimationOperation : DrawOperation
{
    #region Construction

    ImageSharpTexture sharpTexture;

    public ImageAnimationOperation(ImageSharpTexture texture)
    {
        sharpTexture = texture;
    }

    #endregion

    #region Resources

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

    protected override ValueTask CreateResources(GraphicsDevice device, ResourceFactory factory)
    {
        NotifyPendingGPUUpdate();
        _shiftBuffer = factory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
        _vertexBuffer = factory.CreateBuffer(new BufferDescription(16 * 4, BufferUsage.VertexBuffer));
        _indexBuffer = factory.CreateBuffer(new BufferDescription(2 * 6, BufferUsage.IndexBuffer));

        _computeShader = factory.CreateTextureComputeShader();

        _computeLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("Tex", ResourceKind.TextureReadWrite, ShaderStages.Compute),
            new ResourceLayoutElementDescription("ScreenSizeBuffer", ResourceKind.UniformBuffer, ShaderStages.Compute),
            new ResourceLayoutElementDescription("ShiftBuffer", ResourceKind.UniformBuffer, ShaderStages.Compute)));

        ComputePipelineDescription computePipelineDesc = new ComputePipelineDescription(
            _computeShader,
            _computeLayout,
            16, 16, 1);

        _computePipeline = factory.CreateComputePipeline(ref computePipelineDesc);

        Shader[] shaders = { factory.CreateTextureVertexShader(), factory.CreateTextureFragmentShader() };
            
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

    protected override ValueTask CreateWindowSizedResources(GraphicsDevice device, ResourceFactory factory, DeviceBuffer? screenSizeBuffer)
    {
        _computeTargetTexture?.Dispose();
        _computeTargetTextureView?.Dispose();
        _computeResourceSet?.Dispose();
        _graphicsResourceSet?.Dispose();

        _computeTargetTexture = ImageTextures.RobinSpriteSheet.CreateDeviceTexture(device, factory);

        _computeTargetTextureView = factory.CreateTextureView(_computeTargetTexture);

        _computeResourceSet = factory.CreateResourceSet(new ResourceSetDescription(
            _computeLayout,
            _computeTargetTextureView,
            screenSizeBuffer,
            _shiftBuffer));

        _graphicsResourceSet = factory.CreateResourceSet(new ResourceSetDescription(
            _graphicsLayout,
            _computeTargetTextureView,
            _computeTargetTextureView,
            _computeTargetTextureView,
            device.PointSampler));

        NotifyPendingGPUUpdate();

        return ValueTask.CompletedTask;
    }

    #endregion

    #region Drawing

    private readonly object sync = new();

    Viewport Source;
    Viewport Destination;

    public void SetState(Viewport src, Viewport dst)
    {
        lock (sync)
        {
            Source = src;
            Destination = dst;
        }
    }

    protected override ValueTask Draw(TimeSpan delta, CommandList cl, GraphicsDevice device, Framebuffer mainBuffer, DeviceBuffer? screenSizeBuffer)
    {
        Viewport src;
        Viewport dst;
        lock (sync)
        {
            src = Source;
            dst = Destination;
        }

        var winSize = Manager.WindowSize;

        _ticks += (float)delta.TotalMilliseconds;
        Vector4 shifts = new(
            winSize.Width * (float)Math.Cos(_ticks / 500f), // Red shift
            winSize.Height * (float)Math.Sin(_ticks / 1250f), // Green shift
            (float)Math.Sin(_ticks / 1000f), // Blue shift
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

    #endregion

    #region GPU State

    protected override ValueTask UpdateGPUState(GraphicsDevice device, CommandList cl, DeviceBuffer? screenSizeBuffer)
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

    #endregion
}
