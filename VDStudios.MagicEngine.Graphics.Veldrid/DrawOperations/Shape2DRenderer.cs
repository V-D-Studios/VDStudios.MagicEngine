﻿using System.Buffers;
using System.Diagnostics;
using System.Net.Mime;
using System.Numerics;
using System.Resources;
using System.Runtime.CompilerServices;
using VDStudios.MagicEngine.Geometry;
using VDStudios.MagicEngine.Graphics.Veldrid.Generators;
using VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes;
using VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes.Interfaces;
using VDStudios.MagicEngine.Graphics.Veldrid.Properties;
using Veldrid;
using Veldrid.SPIRV;

namespace VDStudios.MagicEngine.Graphics.Veldrid.DrawOperations;

/// <summary>
/// An operation that renders a <see cref="ShapeDefinition2D"/> using <see cref="VertexColor2D"/>
/// </summary>
public class Shape2DRenderer : Shape2DRenderer<VertexColor2D>
{
    /// <inheritdoc/>
    public Shape2DRenderer(ShapeDefinition2D shape, Game game, ElementSkip vertexSkip = default) : base(shape, game, vertexSkip) { }
}

/// <summary>
/// An operation that renders a <see cref="ShapeDefinition2D"/>
/// </summary>
public class Shape2DRenderer<TVertex> : VeldridDrawOperation
    where TVertex : unmanaged, IVertexType<TVertex>
{
    /// <summary>
    /// Creates a new object of type <see cref="Shape2DRenderer{TVertex}"/>
    /// </summary>
    public Shape2DRenderer(ShapeDefinition2D shape, Game game, ElementSkip vertexSkip = default) : base(game)
    {
        Shape = shape;
        VertexSkip = vertexSkip;
    }

    /// <summary>
    /// The <see cref="IVertexGenerator{TInputVertex, TGraphicsVertex}"/> for this <see cref="Shape2DRenderer{TVertex}"/>. It will be used when generating the vertex buffer info
    /// </summary>
    /// <remarks>
    /// Changing this property will not result in vertices being re-generated, see <see cref="NotifyPendingVertexRegeneration"/>
    /// </remarks>
    public IVertexGenerator<Vector2, TVertex>? VertexGenerator { get; set; }

    /// <summary>
    /// Notifies this object to perform a vertex regeneration in the next frame
    /// </summary>
    /// <remarks>
    /// This method calls <see cref="DrawOperation{TGraphicsContext}.NotifyPendingGPUUpdate"/> as well
    /// </remarks>
    public void NotifyPendingVertexRegeneration()
    {
        pendingVertexRegen = true;
    }
    private bool pendingVertexRegen;

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
    /// The index of the <see cref="Shape2DRenderer{TVertex}"/> pipeline this object will use
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
            nameof(Shape2DRenderer<TVertex>), 
            static (n, c) => c.ResourceFactory.CreateFromSpirv(
                vertexShaderDescription: new ShaderDescription(
                    ShaderStages.Vertex,
                    Encoding.UTF8.GetBytes(DefaultShaders.DefaultShape2DRendererVertexShader),
                    "main",
                    true
                ),
                fragmentShaderDescription: new ShaderDescription(
                    ShaderStages.Fragment,
                    Encoding.UTF8.GetBytes(DefaultShaders.DefaultShape2DRendererFragmentShader),
                    "main",
                    true
                )
            ), context);

        if (context.ContainsPipeline<Shape2DRenderer<TVertex>>() is false)
            context.RegisterPipeline<Shape2DRenderer<TVertex>>(context.ResourceFactory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
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
                primitiveTopology: PrimitiveTopology.TriangleStrip,
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
                    context.GetResourceLayout<VeldridDrawOperation>()
                },
                outputs: context.GraphicsDevice.SwapchainFramebuffer.OutputDescription,
                resourceBindingModel: ResourceBindingModel.Improved
            )), out _);
    }

    /// <inheritdoc/>
    protected override void UpdateGPUState(VeldridGraphicsContext context)
    {
        base.UpdateGPUState(context);

        if (shapeChanged)
        {
            pendingVertexRegen = true;
            var vertexlen = Shape.Count;
            var indexlen = Shape.GetTriangulationLength();
            var bufferLen = (uint)(TVertex.Size * vertexlen + sizeof(uint) * indexlen);

            VertexEnd = (uint)(vertexlen * TVertex.Size);

            if (VertexIndexBuffer is not null && bufferLen > VertexIndexBuffer.SizeInBytes)
            {
                VertexIndexBuffer.Dispose();
                VertexIndexBuffer = null;
            }

            VertexIndexBuffer ??= context.ResourceFactory.CreateBuffer(new BufferDescription()
            {
                SizeInBytes = bufferLen,
                Usage = BufferUsage.VertexBuffer | BufferUsage.IndexBuffer
            });

            IndexCount = (uint)Shape.GetTriangulationLength(VertexSkip);

            Span<ushort> indices = stackalloc ushort[indexlen];
            Shape.Triangulate(indices, VertexSkip);

            context.CommandList.UpdateBuffer(VertexIndexBuffer, VertexEnd, indices);
        }

        if (pendingVertexRegen)
        {
            var vertexlen = Shape.Count;
            Debug.Assert(VertexIndexBuffer is not null, "VertexIndexBuffer was unexpectedly null when regenerating vertices");
            var vertices = Shape.AsSpan();

            TVertex[]? rented = null;
            Span<TVertex> graphicVertices = TVertex.Size * vertices.Length > (1024 * 1024)
                ? (rented = ArrayPool<TVertex>.Shared.Rent(vertices.Length)).AsSpan(0, vertices.Length)
                : (stackalloc TVertex[vertices.Length]);

            try
            {
                var gen = VertexGenerator;
                if (gen is null)
                    if (typeof(TVertex).IsAssignableTo(typeof(IDefaultVertexGenerator<Vector2, TVertex>)))
                    {
                        gen = (IVertexGenerator<Vector2, TVertex>)(typeof(TVertex)
                            .GetProperty("DefaultGenerator")!
                            .GetValue(null, null))!;

                        Debug.Assert(gen is not null);
                    }
                    else
                        throw new InvalidOperationException("If a type does not implement IDefaultVertexGenerator, then it VertexGenerator must not be null");

                gen.Generate(vertices, graphicVertices);
                context.CommandList.UpdateBuffer(VertexIndexBuffer, 0, graphicVertices);
            }
            finally
            {
                if (rented is not null)
                    ArrayPool<TVertex>.Shared.Return(rented);
            }
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

        cl.SetFramebuffer(target.GetFramebuffer(context));
        cl.SetVertexBuffer(0, VertexIndexBuffer, 0);
        cl.SetIndexBuffer(VertexIndexBuffer, IndexFormat.UInt16, VertexEnd);
        cl.SetPipeline(context.GetPipeline<Shape2DRenderer<TVertex>>(PipelineIndex));

        cl.SetGraphicsResourceSet(0, context.FrameReportSet);
        cl.SetGraphicsResourceSet(1, target.TransformationSet);
        cl.SetGraphicsResourceSet(2, DrawOperationResourceSet);

        cl.DrawIndexed(IndexCount, 1, 0, 0, 0);
    }
}
