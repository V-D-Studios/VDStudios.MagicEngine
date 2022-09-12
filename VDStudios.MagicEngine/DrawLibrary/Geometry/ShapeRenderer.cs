﻿using System.Buffers;
using System.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using VDStudios.MagicEngine.Geometry;
using Veldrid;
using Veldrid.SPIRV;
using Vulkan;

namespace VDStudios.MagicEngine.DrawLibrary.Geometry;

/// <summary>
/// Represents an operation to draw a list of 2D shapes, using normal <see cref="Vector2"/> as vertices
/// </summary>
public class ShapeRenderer : ShapeRenderer<Vector2>
{
    /// <summary>
    /// Instantiates a new <see cref="ShapeRenderer"/>
    /// </summary>
    /// <param name="shapes">The shapes to fill this list with</param>
    /// <param name="description">Provides data for the configuration of this <see cref="ShapeRenderer"/></param>
    public ShapeRenderer(IEnumerable<ShapeDefinition2D> shapes, ShapeRendererDescription description)
        : base(shapes, description, ShapeVertexGenerator.Default)
    { }
}

/// <summary>
/// Represents an operation to draw a list of 2D shapes
/// </summary>
public class ShapeRenderer<TVertex> : DrawOperation, IReadOnlyList<ShapeDefinition2D> where TVertex : unmanaged
{
    /// <summary>
    /// This list is always updated instantaneously, and represents the real-time state of the renderer before it's properly updated for the next draw sequence
    /// </summary>
    private readonly List<ShapeDefinition2D> _shapes;

    /// <summary>
    /// This enumerable is always updated instantaneously, and represents the real-time state of the renderer before it's properly updated for the next draw sequence
    /// </summary>
    /// <remarks>
    /// Don't mutate this property -- Use <see cref="ShapeRenderer{TVertex}"/>'s methods instead. This property is meant exclusively to be passed to a <see cref="IShapeRendererVertexGenerator{TVertex}"/>
    /// </remarks>
    protected IEnumerable<ShapeDefinition2D> Shapes => _shapes;
    
    /// <summary>
    /// The Vertex Generator for this <see cref="ShapeRenderer{TVertex}"/>
    /// </summary>
    /// <remarks>
    /// Can NEVER be null; an exception will be thrown if an attempt is made to set this property to null
    /// </remarks>
    protected IShapeRendererVertexGenerator<TVertex> TVertexGenerator
    {
        get => _gen;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _gen = value;
        }
    }
    private IShapeRendererVertexGenerator<TVertex> _gen;

    /// <summary>
    /// Be careful when modifying this -- And know that most changes won't have any effect after <see cref="CreateResources(GraphicsDevice, ResourceFactory, ResourceSet[], ResourceLayout[])"/> is called
    /// </summary>
    protected ShapeRendererDescription ShapeRendererDescription;
    private ResourceSet[] ResourceSets;

    /// <summary>
    /// A number, between 0.0 and 1.0, defining the percentage of vertices that will be skipped for a shape
    /// </summary>
    public float VertexSkipFactor
    {
        get => __vertexSkipFactor;
        set
        {
            if (__vertexSkipFactor is < .0f or > 1.01f)
                throw new ArgumentOutOfRangeException(nameof(value), "value must be between 0.0 and 1.0");
            __vertexSkipFactor = value; // Don't bother checking equality for floating points; and there's no need for fancy tolerance calc
            NotifyPendingGPUUpdate();
        }
    }
    private float __vertexSkipFactor = 0.0f;

    /// <summary>
    /// Instantiates a new <see cref="ShapeRenderer{TVertex}"/>
    /// </summary>
    /// <param name="shapes">The shapes to fill this list with</param>
    /// <param name="description">Provides data for the configuration of this <see cref="ShapeRenderer{TVertex}"/></param>
    /// <param name="generator">The <see cref="IShapeRendererVertexGenerator{TVertex}"/> object that will generate the vertices for all shapes in the buffer</param>
    public ShapeRenderer(IEnumerable<ShapeDefinition2D> shapes, ShapeRendererDescription description, IShapeRendererVertexGenerator<TVertex> generator)
    {
        ShapeRendererDescription = description;
        _shapes = new(shapes);

        IndicesToUpdate.EnsureCapacity(_shapes.Capacity);
        for (int i = 0; i < _shapes.Count; i++)
            IndicesToUpdate.Enqueue(new(i, true, true, 1));
        
        TVertexGenerator = generator;
    }

    #region List

    private readonly Queue<UpdateDat> IndicesToUpdate = new();

    /// <inheritdoc/>
    public int IndexOf(ShapeDefinition2D item)
    {
        lock (_shapes)
        {
            return ((IList<ShapeDefinition2D>)_shapes).IndexOf(item);
        }
    }

    /// <inheritdoc/>
    public void Insert(int index, ShapeDefinition2D item)
    {
        lock (_shapes)
        {
            ((IList<ShapeDefinition2D>)_shapes).Insert(index, item);
            IndicesToUpdate.Enqueue(new(index, true, true, 1));
            NotifyPendingGPUUpdate();
        }
    }

    /// <inheritdoc/>
    public void RemoveAt(int index)
    {
        lock (_shapes)
        {
            ((IList<ShapeDefinition2D>)_shapes).RemoveAt(index);
            IndicesToUpdate.Enqueue(new(index, false, false, -1));
            NotifyPendingGPUUpdate();
        }
    }

    /// <inheritdoc/>
    public ShapeDefinition2D this[int index]
    {
        get
        {
            lock (_shapes)
            {
                return ((IList<ShapeDefinition2D>)_shapes)[index];
            }
        }

        set
        {
            lock (_shapes)
            {
                ((IList<ShapeDefinition2D>)_shapes)[index] = value;
                IndicesToUpdate.Enqueue(new(index, true, true, 1));
                NotifyPendingGPUUpdate();
            }
        }
    }

    /// <inheritdoc/>
    public void Add(ShapeDefinition2D item)
    {
        lock (_shapes)
        {
            IndicesToUpdate.Enqueue(new(_shapes.Count - 1, true, true, 1));
            ((ICollection<ShapeDefinition2D>)_shapes).Add(item);
            NotifyPendingGPUUpdate();
        }
    }

    /// <inheritdoc/>
    public void Clear()
    {
        lock (_shapes)
        {
            NotifyPendingGPUUpdate();
            for (int i = 0; i < _shapes.Count; i++)
                IndicesToUpdate.Enqueue(new(i, false, false, -1));
            ((ICollection<ShapeDefinition2D>)_shapes).Clear();
        }
    }

    /// <inheritdoc/>
    public bool Contains(ShapeDefinition2D item)
    {
        lock (_shapes)
        {
            return ((ICollection<ShapeDefinition2D>)_shapes).Contains(item);
        }
    }

    /// <inheritdoc/>
    public void CopyTo(ShapeDefinition2D[] array, int arrayIndex)
    {
        lock (_shapes)
        {
            ((ICollection<ShapeDefinition2D>)_shapes).CopyTo(array, arrayIndex);
        }
    }

    // IMPORTANT // Should it be called every frame?
    private void QueryForChange() // call every frame 
    {
        lock (_shapes)
            for (int i = 0; i < ShapeBufferList.Count; i++) 
            {
                var sh = ShapeBufferList[i];
                if (sh.LastVersion != sh.Shape.Version)
                {
                    NotifyPendingGPUUpdate();
                    IndicesToUpdate.Enqueue(new(i, true, sh.LastCount != sh.Shape.Count, 0));
                    // If the counts don't match, update the indices
                }
            }
    }

    /// <inheritdoc/>
    public int Count => ((ICollection<ShapeDefinition2D>)_shapes).Count;

    /// <inheritdoc/>
    public IEnumerator<ShapeDefinition2D> GetEnumerator()
    {
        return ((IEnumerable<ShapeDefinition2D>)_shapes).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_shapes).GetEnumerator();
    }

    #endregion

    #region Properties

    #endregion

    #region Draw

    #region Resources

    /// <summary>
    /// The Pipeline that will be used to render the shapes
    /// </summary>
    /// <remarks>
    /// Will become available after <see cref="DrawOperation.IsReady"/> is <c>true</c>. If, for any reason, this property is set before that, it will be overwritten
    /// </remarks>
    public Pipeline Pipeline
    {
        get => _pipeline;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
#if VALIDATE_USAGE
            if (ReferenceEquals(_pipeline, value))
                return;
            if (value.IsComputePipeline)
                throw new ArgumentException("A ShapeRenderer cannot have a Compute Pipeline as its Pipeline. It must be a Graphics Pipeline.", nameof(value));
#endif
            _pipeline = value;
        }
    }
    private Pipeline _pipeline;

    /// <summary>
    /// The temporary buffer into which the shapes held in this object will be copied for drawing.
    /// </summary>
    /// <remarks>
    /// It's best to leave this property alone, the code in <see cref="ShapeRenderer{TVertex}"/> will take care of it
    /// </remarks>
    protected List<ShapeDat> ShapeBufferList = new();

#endregion

    private ShaderDescription vertexDefault = new(ShaderStages.Vertex, BuiltInResources.DefaultPolygonVertexShader.GetUTF8Bytes(), "main");
    private ShaderDescription fragmnDefault = new(ShaderStages.Fragment, BuiltInResources.DefaultPolygonFragmentShader.GetUTF8Bytes(), "main");
    private static readonly VertexLayoutDescription DefaultVector2Layout 
        = new(new VertexElementDescription("Position", VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate));

    /// <inheritdoc/>
    protected override async ValueTask CreateResourceSets(GraphicsDevice device, ResourceSetBuilder builder, ResourceFactory factory)
    {
        await base.CreateResourceSets(device, builder, factory);
        ShapeRendererDescription.ResourceLayoutAndSetBuilder?.Invoke(Manager!, device, factory, builder);
    }

    /// <inheritdoc/>
    protected override ValueTask CreateResources(GraphicsDevice device, ResourceFactory factory, ResourceSet[]? resourcesSets, ResourceLayout[]? resourceLayouts)
    {
        var shaders = ShapeRendererDescription.Shaders ?? factory.CreateFromSpirv(
            ShapeRendererDescription.VertexShaderSpirv ?? vertexDefault,
            ShapeRendererDescription.FragmentShaderSpirv ?? fragmnDefault
        );

        Pipeline = ShapeRendererDescription.Pipeline ?? factory.CreateGraphicsPipeline(new(
            ShapeRendererDescription.BlendState,
            ShapeRendererDescription.DepthStencilState,
            new(
                ShapeRendererDescription.FaceCullMode,
                ShapeRendererDescription.RenderMode switch
                {
                    PolygonRenderMode.LineStripWireframe or PolygonRenderMode.TriangulatedWireframe => PolygonFillMode.Wireframe,
                    PolygonRenderMode.TriangulatedFill => PolygonFillMode.Solid,
                    _ => throw new InvalidOperationException($"Unknown PolygonRenderMode: {ShapeRendererDescription.RenderMode}")
                },
                ShapeRendererDescription.FrontFace,
                ShapeRendererDescription.DepthClipEnabled,
                ShapeRendererDescription.ScissorTestEnabled
            ),
            ShapeRendererDescription.RenderMode switch
            {
                PolygonRenderMode.TriangulatedFill or PolygonRenderMode.TriangulatedWireframe => PrimitiveTopology.TriangleStrip,
                PolygonRenderMode.LineStripWireframe => PrimitiveTopology.LineStrip,
                _ => throw new InvalidOperationException($"Unknown PolygonRenderMode: {ShapeRendererDescription.RenderMode}")
            },
            new ShaderSetDescription(new VertexLayoutDescription[]
            {
                ShapeRendererDescription.VertexLayout ?? DefaultVector2Layout
            }, shaders),
            resourceLayouts,
            device.SwapchainFramebuffer.OutputDescription
        ));

        ResourceSets = resourcesSets!;

        NotifyPendingGPUUpdate();

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Submits the commands necessary to render each individual shape on-screen
    /// </summary>
    /// <param name="shape">The data context specific of the shape being drawn</param>
    /// <param name="delta">The amount of time that has passed since the last draw sequence</param>
    /// <param name="device">The Veldrid <see cref="GraphicsDevice"/> attached to the <see cref="GraphicsManager"/> this <see cref="DrawOperation"/> is registered on</param>
    /// <param name="cl">The <see cref="CommandList"/> opened specifically for this call. <see cref="CommandList.End"/> will be called AFTER this method returns, so don't call it yourself</param>
    /// <param name="mainBuffer">The <see cref="GraphicsDevice"/> owned by this <see cref="GraphicsManager"/>'s main <see cref="Framebuffer"/>, to use with <see cref="CommandList.SetFramebuffer(Framebuffer)"/></param>
    protected virtual void DrawShape(TimeSpan delta, in ShapeDat shape, CommandList cl, GraphicsDevice device, Framebuffer mainBuffer)
    {
        cl.SetVertexBuffer(0, shape.Buffer, shape.VertexStart);
        cl.SetIndexBuffer(shape.Buffer!, IndexFormat.UInt16, shape.IndexStart);
        cl.SetPipeline(Pipeline);
        for (uint index = 0; index < ResourceSets.Length; index++)
            cl.SetGraphicsResourceSet(index, ResourceSets[index]);
        cl.DrawIndexed(shape.CurrentIndexCount, 1, 0, 0, 0);
    }

    /// <inheritdoc/>
    protected override ValueTask Draw(TimeSpan delta, CommandList cl, GraphicsDevice device, Framebuffer mainBuffer)
    {
        QueryForChange();
        cl.SetFramebuffer(mainBuffer);
        var sbl = CollectionsMarshal.AsSpan(ShapeBufferList);
        for (int i = 0; i < sbl.Length; i++)
            DrawShape(delta, in sbl[i], cl, device, mainBuffer);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Updates the vertices of a given shape in the renderer
    /// </summary>
    /// <remarks>
    /// CAUTION: Only override this method if you know what you're doing! By default, this method creates a stack-allocated span of <typeparamref name="TVertex"/> instances, generates the vertices using <see cref="TVertexGenerator"/>, and submits it to <see cref="ShapeDat.Buffer"/>, adjusting its size as appropriate
    /// </remarks>
    /// <param name="pol">A reference to the shape's internal data context</param>
    /// <param name="commandList"></param>
    /// <param name="gen">The generator that will be used by this method. Prevents <see cref="TVertexGenerator"/> from being changed while the vertices are being generated</param>
    /// <param name="index">The index of the shape in relation to the amount of shapes whose vertices are being regenerated</param>
    /// <param name="generatorContext">Represents a handle to the <see cref="IShapeRendererVertexGenerator{TVertex}"/>'s context for the current vertex update batch.</param>
    protected virtual void UpdateVertices(ref ShapeDat pol, CommandList commandList, int index, IShapeRendererVertexGenerator<TVertex> gen, ref object? generatorContext)
    {
        var vc = pol.Shape.Count;
        var vc_bytes = DataStructuring.GetSize<TVertex, uint>((uint)vc);

        TVertex[]? rented = null;
        Span<TVertex> vertexBuffer = gen.QueryAllocCPUBuffer(pol.Shape, Shapes, ref generatorContext)
            ? vc_bytes > 2048 // 2KB 
                ? (rented = ArrayPool<TVertex>.Shared.Rent(vc)).AsSpan(0, vc) 
                : (stackalloc TVertex[vc])
            : default;
        vertexBuffer.Clear();

        try
        {
            gen.Generate(pol.Shape, Shapes, vertexBuffer, commandList, pol.Buffer, index, out bool vertexBufferAlreadyUpdated, ref generatorContext);
            if (!vertexBufferAlreadyUpdated)
                commandList.UpdateBuffer(pol.Buffer, pol.VertexStart, vertexBuffer);
        }
        finally
        {
            if (rented is not null)
                ArrayPool<TVertex>.Shared.Return(rented, true);
        }
    }

    /// <summary>
    /// Updates the indices of a given shape in the renderer
    /// </summary>
    /// <remarks>
    /// CAUTION: Only override this method if you know what you're doing! By default, this method chooses the appropriate method to define the shape's indices: Triangulation, uploading as-is, pre-triangulated buffers for specific types of shape, etc.
    /// </remarks>
    /// <param name="pol"></param>
    /// <param name="commandList"></param>
    /// <exception cref="InvalidOperationException"></exception>
    protected virtual void UpdateIndices(ref ShapeDat pol, CommandList commandList)
    {
        const int MaxVertices = 21845;
        const int MaxSize = 2048 / sizeof(ushort);
        var count = pol.Shape.Count;
        if (count >= MaxVertices)
            throw new NotSupportedException($"Triangulating indices for shapes with {MaxVertices} or more vertices is not supported! The shape in question has {count}. The ints used for indices are 16 bits wide, and switching to 32 or 64 bits is not yet supported");

        int indexCount = pol.LineStripIndexCount;
        ushort[]? rented = null;
        if (count <= 3 || ShapeRendererDescription.RenderMode is PolygonRenderMode.LineStripWireframe)
        {
            Span<ushort> indexBuffer = indexCount > MaxSize 
                ? (rented = ArrayPool<ushort>.Shared.Rent(indexCount)).AsSpan(0, indexCount)
                : stackalloc ushort[indexCount];
            indexBuffer.Clear();

            try
            {
                for (int ind = 0; ind < count; ind++)
                    indexBuffer[ind] = (ushort)ind;
                indexBuffer[count] = 0;
                ShapeDat.SetLineStripIndexAndVertexBufferSize(ref pol, Device!.ResourceFactory);
                commandList.UpdateBuffer(pol.Buffer!, pol.IndexStart, indexBuffer);
                return;
            }
            finally
            {
                if (rented is not null)
                    ArrayPool<ushort>.Shared.Return(rented, true);
            }
        }

        // Triangulation

        // For Convex shapes
        // Since we're working exclusively with indices here, this is all data that can be calculated exclusively with a single variable (count).
        // There's probably an easier way to compute this

        if (pol.Shape.IsConvex is false)
            throw new InvalidOperationException($"Triangulation of Concave Polygons is not supported yet");

        if (count is 4) // And is Convex
        {
            ShapeDat.SetTriangulatedIndexAndVertexBufferSize(ref pol, 6, Device!.ResourceFactory);
            commandList.UpdateBuffer(pol.Buffer!, pol.IndexStart, stackalloc ushort[6] { 1, 0, 3, 1, 2, 3 });
            return;
        }

        ushort i = count % 2 == 0 ? (ushort)0u : (ushort)1u;
        indexCount = (count - i) * 3;

        Span<ushort> buffer = indexCount > MaxSize 
            ? (rented = ArrayPool<ushort>.Shared.Rent(indexCount)).AsSpan(0, indexCount)
            : stackalloc ushort[indexCount];
        int bufind = 0;

        ushort p0 = 0;
        ushort pHelper = 1;
        ushort pTemp;
        try
        {
            for (; i < count; i++)
            {
                pTemp = i;
                buffer[bufind++] = p0;
                buffer[bufind++] = pHelper;
                buffer[bufind++] = pTemp;
                pHelper = pTemp;
            }

            ShapeDat.SetTriangulatedIndexAndVertexBufferSize(ref pol, indexCount, Device!.ResourceFactory);
            commandList.UpdateBuffer(pol.Buffer, 0, buffer);
        }
        finally
        {
            if (rented is not null)
                ArrayPool<ushort>.Shared.Return(rented, true);
        }
    }

    /// <inheritdoc/>
    protected override async ValueTask UpdateGPUState(GraphicsDevice device, CommandList commandList)
    {
        var t = base.UpdateGPUState(device, commandList);
        UpdateShapes(device, commandList);
        await t;
    }

    private void UpdateShapes(GraphicsDevice device, CommandList commandList)
    {
        lock (_shapes)
        {
            if (IndicesToUpdate.TryDequeue(out var dat))
            {
                var gen = TVertexGenerator;
                object? genContext = null;
                int ind = 0;
                gen.Start(this, Shapes, IndicesToUpdate.Count, ref genContext);
                do
                {
                    switch (dat.Added)
                    {
                        case < 0:
                            var pd = ShapeBufferList[dat.Index];
                            pd.Dispose();
                            continue;
                        case 0:
                            var polybuffer = CollectionsMarshal.AsSpan(ShapeBufferList);
                            if (dat.UpdateIndices)
                                UpdateIndices(ref polybuffer[dat.Index], commandList);
                            if (dat.UpdateVertices)
                                UpdateVertices(ref polybuffer[dat.Index], commandList, ind++, gen, ref genContext);
                            ShapeDat.UpdateLastVer(ref polybuffer[dat.Index]);
                            continue;
                        case > 0:
                            var np = new ShapeDat(_shapes[dat.Index], device.ResourceFactory);
                            UpdateIndices(ref np, commandList);
                            UpdateVertices(ref np, commandList, ind++, gen, ref genContext);
                            ShapeBufferList.Insert(dat.Index, np);
                            ShapeDat.UpdateLastVer(ref np);
                            continue;
                    }
                } while (IndicesToUpdate.TryDequeue(out dat));
                gen.Stop(this, ref genContext);
            }
        }

        for (int i = ShapeBufferList.Count - 1; i >= 0; i--)
            if (ShapeBufferList[i].remove)
                ShapeBufferList.RemoveAt(i);
    }

#endregion

#region Helper Classes

    private readonly record struct UpdateDat(int Index, bool UpdateVertices, bool UpdateIndices, sbyte Added);

    /// <summary>
    /// Represents shape and device related data
    /// </summary>
    protected struct ShapeDat : IDisposable
    {
        internal bool remove = false;
        /// <summary>
        /// The shape in question
        /// </summary>
        public readonly ShapeDefinition2D Shape;

        /// <summary>
        /// The buffer holding the vertex data for this shape
        /// </summary>
        /// <remarks>
        /// The vertices are set first, and the indices are set right after the vertices as follows, where I: index space, V: vertex space: [VVVIIIIII]. This is because vertices actually change less often than indices
        /// </remarks>
        public DeviceBuffer Buffer = null;

        /// <summary>
        /// The actual current count of indices for this shape
        /// </summary>
        public ushort CurrentIndexCount;

        /// <summary>
        /// The offset at which vertex data starts
        /// </summary>
        public uint VertexStart;

        /// <summary>
        /// The offset at which index data starts
        /// </summary>
        public uint IndexStart;

        /// <summary>
        /// The count of indices in this Polygon
        /// </summary>
        public readonly ushort LineStripIndexCount;

        /// <summary>
        /// This property is used to keep track of changes to the shape, so that it can be re-processed if needed
        /// </summary>
        public int LastVersion { get; private set; }

        /// <summary>
        /// This property is used to keep track of changes to the shape, so that it can be re-processed if needed
        /// </summary>
        public int LastCount { get; private set; }

        /// <summary>
        /// Sets the size of the buffer, taking into account that the indices are triangulated. Adjusts the offsets, and creates or resizes the buffer as needed.
        /// </summary>
        /// <remarks>
        /// If the buffer is created and large enough, only the offsets are updated. If it's <c>null</c> or too small, it's recreated (and disposed of, if necessary)
        /// </remarks>
        public static void SetTriangulatedIndexAndVertexBufferSize(ref ShapeDat dat, int indexCount, ResourceFactory factory)
        {
            var indexSize = DataStructuring.GetSize<ushort, uint>((uint)indexCount);
            var vertexSize = DataStructuring.GetSize<TVertex, uint>((uint)dat.Shape.Count);
            var size = vertexSize + indexSize;
            
            dat.VertexStart = indexSize;
            dat.IndexStart = 0;

            if (dat.Buffer is null || size > dat.Buffer.SizeInBytes)
            {
                dat.Buffer?.Dispose();
                dat.Buffer = factory.CreateBuffer(new(
                    size,
                    BufferUsage.VertexBuffer | BufferUsage.IndexBuffer
                ));
            }

            dat.CurrentIndexCount = (ushort)indexCount;
        }

        /// <summary>
        /// Sets the size of the buffer, taking into account that the indices are sequential. Adjusts the offsets, and creates or resizes the buffer as needed.
        /// </summary>
        /// <remarks>
        /// If the buffer is created and large enough, only the offsets are updated. If it's <c>null</c> or too small, it's recreated (and disposed of, if necessary)
        /// </remarks>
        public static void SetLineStripIndexAndVertexBufferSize(ref ShapeDat dat, ResourceFactory factory)
        {
            var indexSize = DataStructuring.GetSize<ushort, uint>(dat.LineStripIndexCount);
            var vertexSize = DataStructuring.GetSize<TVertex, uint>((uint)dat.Shape.Count);
            var size = vertexSize + indexSize;

            dat.VertexStart = indexSize;
            dat.IndexStart = 0;

            if (dat.Buffer is null || size > dat.Buffer.SizeInBytes)
            {
                dat.Buffer?.Dispose();
                dat.Buffer = factory.CreateBuffer(new(
                    size,
                    BufferUsage.VertexBuffer | BufferUsage.IndexBuffer
                ));
            }

            dat.CurrentIndexCount = dat.LineStripIndexCount;
        }

        /// <summary>
        /// Updates the shape version data cache so that it may be re-processed if needed
        /// </summary>
        public static void UpdateLastVer(ref ShapeDat dat)
        {
            dat.LastVersion = dat.Shape.Version;
            dat.LastCount = dat.Shape.Count;
        }

        /// <summary>
        /// Instances a new ShapeDat object for <paramref name="def"/>
        /// </summary>
        /// <param name="def"></param>
        /// <param name="factory"></param>
        public ShapeDat(ShapeDefinition2D def, ResourceFactory factory)
        {
            ArgumentNullException.ThrowIfNull(def);
            
            LineStripIndexCount = (ushort)(def.Count + 1);
            CurrentIndexCount = 0;
            Shape = def;
            LastVersion = 0;
            LastCount = 0;
        }

        /// <summary>
        /// Disposes of the resources held by this <see cref="ShapeDat"/> and marks it for removal
        /// </summary>
        public void Dispose()
        {
            ((IDisposable)Buffer).Dispose();
            remove = true;
        }
    }

#endregion
}
