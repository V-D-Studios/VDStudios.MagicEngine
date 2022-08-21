using SDL2.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using VDStudios.MagicEngine.Properties;
using VDStudios.MagicEngine.Utility;
using Veldrid;
using Veldrid.ImageSharp;
using Veldrid.SPIRV;
using PixelFormat = Veldrid.PixelFormat;
using Texture = Veldrid.Texture;

namespace VDStudios.MagicEngine.DrawLibrary;

public class GradientColorShow : DrawOperation
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
    /// The actual position at which to draw this <see cref="GradientColorShow"/>
    /// </summary>
    public Vector2 Position { get; set; }

    #endregion

    /// <inheritdoc/>
    protected override ValueTask CreateWindowSizedResources(GraphicsDevice device, ResourceFactory factory, DeviceBuffer screenSizeBuffer)
    {
        _computeTargetTexture?.Dispose();
        _computeTargetTextureView?.Dispose();
        _computeResourceSet?.Dispose();
        _graphicsResourceSet?.Dispose();

        _computeTargetTexture = factory.CreateTexture(TextureDescription.Texture2D(
            _computeTexSize,
            _computeTexSize,
            1,
            1,
            PixelFormat.R32_G32_B32_A32_Float,
            TextureUsage.Sampled | TextureUsage.Storage));

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

        WinSize = Manager!.WindowSize;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    protected override ValueTask CreateResources(GraphicsDevice device, ResourceFactory factory, ResourceSet[]? sets, ResourceLayout[]? layouts)
    {
        _shiftBuffer = factory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
        _vertexBuffer = factory.CreateBuffer(new BufferDescription(16 * 4, BufferUsage.VertexBuffer));
        _indexBuffer = factory.CreateBuffer(new BufferDescription(2 * 6, BufferUsage.IndexBuffer));

        _computeShader = factory.CreateFromSpirv(new(
            ShaderStages.Compute,
            Compute.GetUTF8Bytes(),
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

        Shader[] shaders = factory.CreateFromSpirv(
            new ShaderDescription(
                    ShaderStages.Vertex,
                    Vertex.GetUTF8Bytes(),
                    "main"
                ),
            new ShaderDescription(
                    ShaderStages.Fragment,
                    Fragment.GetUTF8Bytes(),
                    "main"
                )
            );

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

        NotifyPendingGPUUpdate();

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

        Span<Vector4> quadVerts = stackalloc Vector4[]
        {
            new Vector4(-1, 1, 0, 0),
            new Vector4(1, 1, 1, 0),
            new Vector4(1, -1, 1, 1),
            new Vector4(-1, -1, 0, 1),
        };

        Span<ushort> indices = stackalloc ushort[] { 0, 1, 2, 0, 2, 3 };

        cl.UpdateBuffer(_vertexBuffer, 0, quadVerts);
        cl.UpdateBuffer(_indexBuffer, 0, indices);

        return ValueTask.CompletedTask;
    }

    private const string Compute = @"#version 450

layout(set = 0, binding = 1) uniform ScreenSizeBuffer
{
    float ScreenWidth;
    float ScreenHeight;
    vec2 Padding_;
};

layout(set = 0, binding = 2) uniform ShiftBuffer
{
    float RShift;
    float GShift;
    float BShift;
    float Padding1_;
};

layout(set = 0, binding = 0, rgba32f) uniform image2D Tex;

layout(local_size_x = 16, local_size_y = 16, local_size_z = 1) in;

void main()
{
    float x = (gl_GlobalInvocationID.x + RShift);
    float y = (gl_GlobalInvocationID.y + GShift);

    imageStore(Tex, ivec2(gl_GlobalInvocationID.xy), vec4(x / ScreenWidth, y / ScreenHeight, BShift, 1));
}";

    private const string Vertex = @"#version 450

layout (location = 0) in vec2 Position;
layout (location = 1) in vec2 TexCoords;
layout (location = 0) out vec2 fsin_TexCoords;

void main()
{
    fsin_TexCoords = TexCoords;
    gl_Position = vec4(Position, 0, 1);
}";

    private const string Fragment = @"#version 450

layout(set = 0, binding = 0) uniform texture2D Tex;
layout(set = 0, binding = 1) uniform texture2D Tex11;
layout(set = 0, binding = 2) uniform texture2D Tex22;
layout(set = 0, binding = 3) uniform sampler SS;

layout(location = 0) in vec2 fsin_TexCoords;
layout(location = 0) out vec4 OutColor;

void main()
{
    OutColor = texture(sampler2D(Tex, SS), fsin_TexCoords) + texture(sampler2D(Tex11, SS), fsin_TexCoords) * .01 + texture(sampler2D(Tex22, SS), fsin_TexCoords) * .01;
}";
}
