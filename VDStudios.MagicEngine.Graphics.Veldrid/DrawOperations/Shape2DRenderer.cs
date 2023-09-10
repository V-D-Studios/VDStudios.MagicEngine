using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using VDStudios.MagicEngine.Geometry;
using Veldrid;

namespace VDStudios.MagicEngine.Graphics.Veldrid.DrawOperations;

#warning NOTE: For multi-shape renderers, disallow changing the shape or adding more shapes

/// <summary>
/// An operation that renders a <see cref="ShapeDefinition2D"/>
/// </summary>
public class Shape2DRenderer : VeldridDrawOperation
{
    /// <summary>
    /// Creates a new object of type <see cref="TextOperation"/>
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
    private uint IndexEnd;

    /// <inheritdoc/>
    protected override ValueTask CreateResourcesAsync()
        => ValueTask.CompletedTask;

    /// <inheritdoc/>
    protected override void CreateGPUResources(VeldridGraphicsContext context) 
    {
        base.CreateGPUResources(context);
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

            VertexIndexBuffer ??= context.ResourceFactory.CreateBuffer(new BufferDescription()
            {
                SizeInBytes = bufferLen,
                Usage = BufferUsage.VertexBuffer | BufferUsage.IndexBuffer
            });

            VertexEnd = (uint)(vertexlen * Unsafe.SizeOf<Vector2>());
            indexlen = Shape.GetTriangulationLength(VertexSkip);
            IndexEnd = (uint)(indexlen * sizeof(uint));

            Span<uint> indices = stackalloc uint[vertexlen];
            Shape.Triangulate(indices, VertexSkip);

            context.CommandList.UpdateBuffer(VertexIndexBuffer, 0, Shape.AsSpan());
            context.CommandList.UpdateBuffer(VertexIndexBuffer, VertexEnd, indices);
        }
    }

    /// <inheritdoc/>
    protected override void Draw(TimeSpan delta, VeldridGraphicsContext context, RenderTarget<VeldridGraphicsContext> target)
    {
        Debug.Assert(target is VeldridRenderTarget, "target is not of type VeldridRenderTarget");
        var veldridTarget = (VeldridRenderTarget)target;
        var cl = veldridTarget.CommandList;
        veldridTarget.TransformationBuffer
    }
}
