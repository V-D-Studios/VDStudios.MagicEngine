using System.Diagnostics;
using System.Net.Mime;
using System.Numerics;
using System.Resources;
using System.Runtime.CompilerServices;
using VDStudios.MagicEngine.Geometry;
using VDStudios.MagicEngine.Graphics.Veldrid.Properties;
using Veldrid;
using Veldrid.SPIRV;

namespace VDStudios.MagicEngine.Graphics.Veldrid.DrawOperations;

#warning NOTE: For multi-shape renderers, disallow changing the shape or adding more shapes

/// <summary>
/// An operation that renders a <see cref="ShapeDefinition2D"/>
/// </summary>
public class Shape2DRenderer : VeldridDrawOperation
{
    /// <summary>
    /// Creates a new object of type <see cref="Shape2DRenderer"/>
    /// </summary>
    public Shape2DRenderer(ShapeDefinition2D shape, Game game, ElementSkip vertexSkip = default) : base(game)
    {
        Shape = shape;
        VertexSkip = vertexSkip;
    }

    /// <summary>
    /// The shape that will be Rendered
    /// </summary>
    public ShapeDefinition2D Shape { get; private set; }

    /// <summary>
    /// The vertices to skip
    /// </summary>
    public ElementSkip VertexSkip { get; private set; }
    private bool shapeChanged = true;

    /// <summary>
    /// The index of the <see cref="Shape2DRenderer"/> pipeline this object will use
    /// </summary>
    public uint PipelineIndex { get; set; }

    /// <summary>
    /// Sets the <see cref="Shape"/> and or <see cref="VertexSkip"/>. If either is null, their respective proprety will not be changed
    /// </summary>
    /// <param name="shape">The new shape to set. Ignored if <see langword="null"/>. If it's the same as <see cref="Shape"/>, a <see cref="UpdateGPUState(VeldridGraphicsContext)"/> call will not be incurred</param>
    /// <param name="vertexSkip">The new Vertex Skip to set. Ignored if <see langword="null"/>. If it's the same as <see cref="VertexSkip"/>, a <see cref="UpdateGPUState(VeldridGraphicsContext)"/> call will not be incurred</param>
    public void SetShape(ShapeDefinition2D? shape, ElementSkip? vertexSkip)
    {
        if (shape is null && vertexSkip is null) return;

        if (shape is not null && shape != Shape)
        {
            Shape = shape;
            shapeChanged = true;
        }

        if (vertexSkip is ElementSkip skip && skip != VertexSkip)
        {
            VertexSkip = skip;
            shapeChanged = true;
        }

        if (shapeChanged)
            NotifyPendingGPUUpdate();
    }

    private DeviceBuffer? VertexIndexBuffer;
    private uint VertexEnd;
    private uint IndexCount;

    /// <inheritdoc/>
    protected override ValueTask CreateResourcesAsync()
        => ValueTask.CompletedTask;

    /// <inheritdoc/>
    protected override void CreateGPUResources(VeldridGraphicsContext context) 
    {
        base.CreateGPUResources(context);

        var shaders = context.ShaderCache.GetOrAdd(
            nameof(Shape2DRenderer), 
            static (n, c) => c.ResourceFactory.CreateFromSpirv(
                vertexShaderDescription: new ShaderDescription(
                    ShaderStages.Vertex,
                    DefaultShaders.DefaultShape2DRendererVertexShaderBytes,
                    "main"
                ),
                fragmentShaderDescription: new ShaderDescription(
                    ShaderStages.Fragment,
                    DefaultShaders.DefaultShape2DRendererFragmentShaderBytes,
                    "main"
                )
            ), context);

        if (context.ContainsPipeline<Shape2DRenderer>() is false)
            context.RegisterPipeline<Shape2DRenderer>(context.ResourceFactory.CreateGraphicsPipeline(new GraphicsPipelineDescription()
            {
                BlendState = BlendStateDescription.SingleAlphaBlend,
                DepthStencilState = DepthStencilStateDescription.DepthOnlyGreaterEqual,
                Outputs = context.GraphicsDevice.SwapchainFramebuffer.OutputDescription,
                PrimitiveTopology = PrimitiveTopology.TriangleStrip,
                RasterizerState = RasterizerStateDescription.Default,
                ResourceBindingModel = ResourceBindingModel.Improved,
                ResourceLayouts = new ResourceLayout[]
                {
                    context.GetResourceLayout<VeldridDrawOperation>(),
                    context.GetResourceLayout<VeldridRenderTarget>(),
                    context.FrameReportLayout
                },
                ShaderSet = new ShaderSetDescription(
                    new VertexLayoutDescription[]
                    {
                        new VertexLayoutDescription(
                            new VertexElementDescription(
                                "Vertex",
                                VertexElementFormat.Float2,
                                VertexElementSemantic.Position
                            )
                        )
                    },
                    shaders
                )
            }), out _);
    }

    /// <inheritdoc/>
    protected override void UpdateGPUState(VeldridGraphicsContext context)
    {
        base.UpdateGPUState(context);

        if (shapeChanged)
        {
            var vertexlen = Shape.Count;
            var indexlen = Shape.GetTriangulationLength();
            var bufferLen = (uint)((Unsafe.SizeOf<Vector2>() * vertexlen) + (sizeof(uint) * indexlen));

            if (VertexIndexBuffer is not null && bufferLen > VertexIndexBuffer.SizeInBytes)
            {
                VertexIndexBuffer.Dispose();
                VertexIndexBuffer = null;
            }

            if (VertexIndexBuffer is null)
            {
                VertexIndexBuffer = context.ResourceFactory.CreateBuffer(new BufferDescription()
                {
                    SizeInBytes = bufferLen,
                    Usage = BufferUsage.VertexBuffer | BufferUsage.IndexBuffer
                });

                context.CommandList.UpdateBuffer(VertexIndexBuffer, 0, Shape.AsSpan());
                VertexEnd = (uint)(vertexlen * Unsafe.SizeOf<Vector2>());
            }

            IndexCount = (uint)Shape.GetTriangulationLength(VertexSkip);

            Span<ushort> indices = stackalloc ushort[vertexlen];
            Shape.Triangulate(indices, VertexSkip);

            context.CommandList.UpdateBuffer(VertexIndexBuffer, VertexEnd, indices);
        }
    }

    /// <inheritdoc/>
    protected override void Draw(TimeSpan delta, VeldridGraphicsContext context, RenderTarget<VeldridGraphicsContext> t)
    {
        Debug.Assert(VertexIndexBuffer is not null, "VertexIndexBuffer was unexpectedly null at the time of drawing");
        Debug.Assert(t is VeldridRenderTarget, "target is not of type VeldridRenderTarget");
        Debug.Assert(DrawOperationResourceSet is not null, "DrawOperationResourceSet was unexpectedly null at the time of drawing");

        var target = (VeldridRenderTarget)t;
        var cl = target.CommandList;

        cl.SetVertexBuffer(0, VertexIndexBuffer, 0);
        cl.SetIndexBuffer(VertexIndexBuffer, IndexFormat.UInt16, VertexEnd);
        cl.SetPipeline(context.GetPipeline<Shape2DRenderer>(PipelineIndex));

        cl.SetGraphicsResourceSet(0, context.FrameReportSet);
        cl.SetGraphicsResourceSet(1, target.TransformationSet);
        cl.SetGraphicsResourceSet(2, DrawOperationResourceSet);

        cl.DrawIndexed(IndexCount, 1, 0, 0, 0);
    }
}
