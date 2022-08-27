using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using VDStudios.MagicEngine.DrawLibrary;
using Veldrid;
using Veldrid.SPIRV;

namespace VDStudios.MagicEngine.Demo.Nodes;
public class ColorBackgroundNode : Node, IDrawableNode
{
    public ColorBackgroundNode()
    {
        DrawOperationManager = new DrawOperationManagerDrawQueueDelegate(this, (q, op) =>
        {
            if (op is ColorDraw)
                q.Enqueue(op, 1);
            else if (op is GradientColorShow)
                q.Enqueue(op, 2);
        });
        DrawOperationManager.AddDrawOperation<ColorDraw>();
        DrawOperationManager.AddDrawOperation<GradientColorShow>();
    }

    #region Draw Operations

    private struct VertexPositionColor
    {
        public Vector2 Position;
        public RgbaFloat Color;
        public VertexPositionColor(Vector2 position, RgbaFloat color)
        {
            Position = position;
            Color = color;
        }
        public const uint SizeInBytes = 24;
    }

    private sealed class ColorDraw : DrawOperation
    {
        #region Shaders
        
        private const string VertexCode = @"
#version 450

layout(location = 0) in vec2 Position;
layout(location = 1) in vec4 Color;

layout(location = 0) out vec4 fsin_Color;

void main()
{
    gl_Position = vec4(Position, 0, 1);
    fsin_Color = Color;
}";

        private const string FragmentCode = @"
#version 450

layout(location = 0) in vec4 fsin_Color;
layout(location = 0) out vec4 fsout_Color;

void main()
{
    fsout_Color = fsin_Color;
}";

        #endregion

        private DeviceBuffer VertexBuffer;
        private DeviceBuffer IndexBuffer;
        private Shader[] Shaders;
        private Pipeline Pipeline;

        protected override ValueTask CreateResources(GraphicsDevice device, ResourceFactory factory, ResourceSet[]? sets, ResourceLayout[]? layouts)
        {
            Span<VertexPositionColor> _vert = stackalloc VertexPositionColor[]
            {
                new(new(-0.75f, 0.75f), RgbaFloat.Red),
                new(new(0.75f, 0.75f), RgbaFloat.Green),
                new(new(-0.75f, -0.75f), RgbaFloat.Blue),
                new(new(0.75f, -0.75f), RgbaFloat.Yellow)
            };

            Span<ushort> inds = stackalloc ushort[] { 0, 1, 2, 3 };

            VertexBuffer = factory.CreateBuffer(new((uint)(Unsafe.SizeOf<VertexPositionColor>() * 4), BufferUsage.VertexBuffer));
            device.UpdateBuffer(VertexBuffer, 0, _vert);
            
            IndexBuffer = factory.CreateBuffer(new(sizeof(ushort) * 4, BufferUsage.IndexBuffer));
            device.UpdateBuffer(IndexBuffer, 0, inds);

            VertexLayoutDescription vertexLayout = new(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));
            
            Shaders = factory.CreateFromSpirv(
                new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(VertexCode), "main"), 
                new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(FragmentCode), "main"));

            var pp = new GraphicsPipelineDescription
            {
                BlendState = BlendStateDescription.SingleOverrideBlend,
                DepthStencilState = new(true, true, ComparisonKind.LessEqual),
                RasterizerState = new RasterizerStateDescription(cullMode: FaceCullMode.Back,
                                                                 fillMode: PolygonFillMode.Solid,
                                                                 frontFace: FrontFace.Clockwise,
                                                                 depthClipEnabled: true,
                                                                 scissorTestEnabled: false),
                PrimitiveTopology = PrimitiveTopology.TriangleStrip,
                ResourceLayouts = Array.Empty<ResourceLayout>(),
                ShaderSet = new ShaderSetDescription(new VertexLayoutDescription[] { vertexLayout }, Shaders),
                Outputs = device.SwapchainFramebuffer.OutputDescription
            };

            Pipeline = factory.CreateGraphicsPipeline(ref pp);

            return ValueTask.CompletedTask;
        }

        protected override ValueTask Draw(TimeSpan delta, CommandList cl, GraphicsDevice gd, Framebuffer mainBuffer, DeviceBuffer screenSizedBuffer)
        {
            cl.SetFramebuffer(mainBuffer);
            cl.SetVertexBuffer(0, VertexBuffer);
            cl.SetIndexBuffer(IndexBuffer, IndexFormat.UInt16);
            cl.SetPipeline(Pipeline);
            cl.DrawIndexed(4, 1, 0, 0, 0);

            return ValueTask.CompletedTask;
        }

        protected override ValueTask UpdateGPUState(GraphicsDevice device, CommandList cl, DeviceBuffer screenSizedBuffer)
        {
            return ValueTask.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
            VertexBuffer?.Dispose();
            IndexBuffer?.Dispose();
            for (int i = 0; i < Shaders.Length; i++)
            {
                Shaders[i].Dispose();
                Shaders[i] = null;
            }
            Shaders = null;
            Pipeline?.Dispose();
            base.Dispose(disposing);
        }

        protected override ValueTask CreateWindowSizedResources(GraphicsDevice device, ResourceFactory factory, DeviceBuffer screenSizeBuffer)
            => ValueTask.CompletedTask;
    }

    public DrawOperationManager DrawOperationManager { get; } 
    public bool SkipDrawPropagation { get; }

    #endregion

}
