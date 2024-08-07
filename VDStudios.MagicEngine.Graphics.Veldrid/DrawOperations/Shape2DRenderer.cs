﻿using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using System.Numerics;
using System.Resources;
using System.Runtime.CompilerServices;
using SDL2.NET;
using Serilog.Sinks.SystemConsole.Themes;
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
    public Shape2DRenderer(ShapeDefinition2D shape, Game game, IVertexGenerator<Vector2, VertexColor2D>? vertexGenerator = null, ElementSkip vertexSkip = default) 
        : base(shape, game, VertexColor2D.DefaultGenerator, vertexSkip) { }

    /// <summary>
    /// Fetches or registers (and then fetches) the default shader set for <see cref="Shape2DRenderer"/>
    /// </summary>
    public static Shader[] GetDefaultShaders(IVeldridGraphicsContextResources resources) 
        => resources.ShaderCache.GetOrAddResource<Shape2DRenderer>(
            static c => c.ResourceFactory.CreateFromSpirv(
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
            ));
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
    public Shape2DRenderer(ShapeDefinition2D shape, Game game, IVertexGenerator<Vector2, TVertex>? vertexGenerator, ElementSkip vertexSkip = default)
        : this(shape, game, vertexGenerator, vertexSkip, false) { }

    internal Shape2DRenderer(ShapeDefinition2D shape, Game game, IVertexGenerator<Vector2, TVertex>? vertexGenerator, ElementSkip vertexSkip, bool skipResourceCreation) 
        : base(game)
    {
        Shape = shape;
        VertexSkip = vertexSkip;

        if (vertexGenerator is not null)
            VertexGenerator = vertexGenerator;
        else if (typeof(TVertex).IsAssignableTo(typeof(IDefaultVertexGenerator<Vector2, TVertex>)))
        {
            VertexGenerator = (IVertexGenerator<Vector2, TVertex>)(typeof(TVertex)
                .GetProperty("DefaultGenerator")!
                .GetValue(null, null))!;

            Debug.Assert(VertexGenerator is not null);
        }
        else
            throw new InvalidOperationException("If a type does not implement IDefaultVertexGenerator, then it VertexGenerator must not be null");

        SkipResourceCreation = skipResourceCreation;
    }
    private readonly bool SkipResourceCreation;

    /// <summary>
    /// The <see cref="IVertexGenerator{TInputVertex, TGraphicsVertex}"/> for this <see cref="Shape2DRenderer{TVertex}"/>. It will be used when generating the vertex buffer info
    /// </summary>
    /// <remarks>
    /// Changing this property will not result in vertices being re-generated, see <see cref="NotifyPendingVertexRegeneration"/>
    /// </remarks>
    [MemberNotNull(nameof(_vtx))]
    public IVertexGenerator<Vector2, TVertex> VertexGenerator
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
    private IVertexGenerator<Vector2, TVertex> _vtx;

    /// <summary>
    /// Notifies this object to perform a vertex regeneration in the next frame
    /// </summary>
    /// <remarks>
    /// This method calls <see cref="DrawOperation{TGraphicsContext}.NotifyPendingGPUUpdate"/> as well
    /// </remarks>
    public void NotifyPendingVertexRegeneration()
    {
        pendingVertexRegen = true;
        NotifyPendingGPUUpdate();
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

    /// <summary>
    /// The DeviceBuffer used for vertex data
    /// </summary>
    protected DeviceBuffer? VertexBuffer { get; private set; }

    /// <summary>
    /// The DeviceBuffer used for index data
    /// </summary>
    protected DeviceBuffer? IndexBuffer { get; private set; }

    /// <summary>
    /// The amount of indices currently in <see cref="VertexBuffer"/>
    /// </summary>
    protected uint IndexCount { get; private set; }

    /// <summary>
    /// The amount of available vertex sets
    /// </summary>
    public uint AvailableVertexSets { get; private set; }

    /// <summary>
    /// The pipeline category of this type
    /// </summary>
    public Type PipelineCategory { get; protected init; } = typeof(Shape2DRenderer<TVertex>);

    /// <summary>
    /// The index of the vertex set to use
    /// </summary>
    public uint VertexSet
    {
        get => currentVertexSet;
        set
        {
            if (value >= AvailableVertexSets)
                throw new ArgumentException("Cannot set the current Vertex Set to a value above the currently available sets", nameof(value));
            currentVertexSet = value;
        }
    }
    private uint currentVertexSet;
    private uint vertexSetSize;

    /// <summary>
    /// The offset that should be used when binding the vertex buffer, to use only the currently selected vertices
    /// </summary>
    public uint VertexSetOffset => currentVertexSet * vertexSetSize;

    /// <inheritdoc/>
    protected override ValueTask CreateResourcesAsync()
        => ValueTask.CompletedTask;

    /// <inheritdoc/>
    protected override void CreateGPUResources(VeldridGraphicsContext context) 
    {
        base.CreateGPUResources(context);

        if (SkipResourceCreation) return;

        Shader[]? shaders;

        if (this is Shape2DRenderer)
            shaders = Shape2DRenderer.GetDefaultShaders(context);
        else if (context.ShaderCache.TryGetResource<Shape2DRenderer<TVertex>>(out shaders) is false)
            throw new InvalidOperationException($"Could not find a Shader set for {Helper.BuildTypeNameAsCSharpTypeExpression(typeof(Shape2DRenderer<TVertex>))}");

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
                    context.GetResourceLayout<VeldridDrawOperation>()
                },
                outputs: context.GraphicsDevice.SwapchainFramebuffer.OutputDescription,
                resourceBindingModel: ResourceBindingModel.Improved
            )), out _);
    }

    /// <summary>
    /// This method is called automatically within <see cref="UpdateGPUState(VeldridGraphicsContext)"/> if the shape's vertices are being regenerated
    /// </summary>
    protected virtual void VerticesRegenerated(VeldridGraphicsContext context) { }

    /// <inheritdoc/>
    protected override void UpdateGPUState(VeldridGraphicsContext context)
    {
        base.UpdateGPUState(context);

        if (shapeChanged)
        {
            var indexlen = Shape.GetTriangulationLength();
            var indexSize = (uint)(sizeof(ushort) * indexlen);

            if (IndexBuffer is not null && indexSize > IndexBuffer.SizeInBytes)
            {
                IndexBuffer.Dispose();
                IndexBuffer = null;
            }

            IndexBuffer ??= context.ResourceFactory.CreateBuffer(new BufferDescription()
            {
                SizeInBytes = indexSize,
                Usage = BufferUsage.IndexBuffer
            });

            IndexCount = (uint)Shape.GetTriangulationLength(VertexSkip);

            Span<ushort> indices = stackalloc ushort[indexlen];
            Shape.Triangulate(indices, VertexSkip);

            context.CommandList.UpdateBuffer(IndexBuffer, 0, indices);

            pendingVertexRegen = true;
        }

        if (pendingVertexRegen)
        {
            var vertices = Shape.AsSpan();

            var gen = VertexGenerator;
            Debug.Assert(gen is not null, "VertexGenerator was unexpectedly null at the time of rendering");

            uint vertexSetAmount;
            int totalVertexCount;
            checked
            {
                vertexSetAmount = gen.GetOutputSetAmount(vertices);
                if (vertexSetAmount == 0) throw new InvalidOperationException("The current VertexGenerator returned 0 for vertex set amount, there must be at least one available set");
                totalVertexCount = vertices.Length * (int)vertexSetAmount;
            }

            var vertexBufferSizeBytes = (uint)(TVertex.Size * totalVertexCount);

            if (VertexBuffer is not null && vertexBufferSizeBytes > VertexBuffer.SizeInBytes)
            {
                VertexBuffer.Dispose();
                VertexBuffer = null;
            }

            VertexBuffer ??= context.ResourceFactory.CreateBuffer(new BufferDescription()
            {
                SizeInBytes = vertexBufferSizeBytes,
                Usage = BufferUsage.VertexBuffer
            });

            TVertex[]? rented = null;
            Span<TVertex> graphicVertices = TVertex.Size * totalVertexCount > (1024 * 1024)
                ? (rented = ArrayPool<TVertex>.Shared.Rent(totalVertexCount)).AsSpan(0, totalVertexCount)
                : (stackalloc TVertex[totalVertexCount]);

            try
            {
                gen.Generate(vertices, graphicVertices);
                context.CommandList.UpdateBuffer(VertexBuffer, 0, graphicVertices);

                AvailableVertexSets = vertexSetAmount;
                if (currentVertexSet >= AvailableVertexSets)
                    currentVertexSet = 0;
                vertexSetSize = (uint)(TVertex.Size * vertices.Length);
            }
            finally
            {
                if (rented is not null)
                    ArrayPool<TVertex>.Shared.Return(rented);
            }

            pendingVertexRegen = false;
            VerticesRegenerated(context);
        }
    }

    /// <inheritdoc/>
    protected override void Draw(TimeSpan delta, VeldridGraphicsContext context, RenderTarget<VeldridGraphicsContext> t)
    {
        Debug.Assert(VertexBuffer is not null, "VertexBuffer was unexpectedly null at the time of drawing");
        Debug.Assert(IndexBuffer is not null, "IndexBuffer was unexpectedly null at the time of drawing");
        Debug.Assert(t is VeldridRenderTarget, "target is not of type VeldridRenderTarget");
        Debug.Assert(DrawOperationResourceSet is not null, "DrawOperationResourceSet was unexpectedly null at the time of drawing");

        var target = (VeldridRenderTarget)t;
        var cl = target.CommandList;

        cl.SetFramebuffer(target.GetFramebuffer(context));
        
        cl.SetPipeline(context.GetPipeline<Shape2DRenderer<TVertex>>(PipelineIndex));

        cl.SetVertexBuffer(0, VertexBuffer, VertexSetOffset);
        cl.SetIndexBuffer(IndexBuffer, IndexFormat.UInt16, 0);

        cl.SetGraphicsResourceSet(0, context.FrameReportSet);
        cl.SetGraphicsResourceSet(1, target.TransformationSet);
        cl.SetGraphicsResourceSet(2, DrawOperationResourceSet);

        cl.DrawIndexed(IndexCount, 1, 0, 0, 0);
    }
}
