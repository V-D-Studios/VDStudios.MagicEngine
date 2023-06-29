using SixLabors.ImageSharp.Processing;
using System.Buffers;
using System.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using VDStudios.MagicEngine;
using VDStudios.MagicEngine.Geometry;
using Veldrid;
using Veldrid.MetalBindings;
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
    /// Don't mutate this property -- Use <see cref="ShapeRenderer{TVertex}"/>'s methods instead. This property is meant exclusively to be passed to a <see cref="IShape2DRendererVertexGenerator{TVertex}"/>
    /// </remarks>
    protected IEnumerable<ShapeDefinition2D> Shapes => _shapes;
    
    /// <summary>
    /// The Vertex Generator for this <see cref="ShapeRenderer{TVertex}"/>
    /// </summary>
    /// <remarks>
    /// Can NEVER be null; an exception will be thrown if an attempt is made to set this property to null
    /// </remarks>
    protected IShape2DRendererVertexGenerator<TVertex> TVertexGenerator
    {
        get => _vgen;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _vgen = value;
        }
    }
    private IShape2DRendererVertexGenerator<TVertex> _vgen;

    /// <summary>
    /// The Index Generator for this <see cref="ShapeRenderer{TVertex}"/>
    /// </summary>
    /// <remarks>
    /// Can NEVER be null; an exception will be thrown if an attempt is made to set this property to null
    /// </remarks>
    protected IShape2DRendererIndexGenerator IndexGenerator
    {
        get => _igen;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _igen = value;
        }
    }
    private IShape2DRendererIndexGenerator _igen;

    /// <summary>
    /// Be careful when modifying this -- And know that most changes won't have any effect after <see cref="CreateResources(GraphicsDevice, ResourceFactory, ResourceSet[], ResourceLayout[])"/> is called
    /// </summary>
    protected ShapeRendererDescription ShapeRendererDescription;
    private ResourceSet[] ResourceSets;

    /// <summary>
    /// A number, between 0.0 and 1.0, defining the percentage of vertices that will be skipped for a shape
    /// </summary>
    public ElementSkip VertexSkip
    {
        get => __vertexSkip;
        set
        {
            if(__vertexSkip == value)
                return;
            __vertexSkip = value;
            __vertexSkipChanged = true;
            NotifyPendingGPUUpdate();
        }
    }
    private ElementSkip __vertexSkip;
    private bool __vertexSkipChanged = false;

    /// <summary>
    /// Instantiates a new <see cref="ShapeRenderer{TVertex}"/>
    /// </summary>
    /// <param name="shapes">The shapes to fill this list with</param>
    /// <param name="description">Provides data for the configuration of this <see cref="ShapeRenderer{TVertex}"/></param>
    /// <param name="generator">The <see cref="IShape2DRendererVertexGenerator{TVertex}"/> object that will generate the vertices for all shapes in the buffer</param>
    public ShapeRenderer(IEnumerable<ShapeDefinition2D> shapes, ShapeRendererDescription description, IShape2DRendererVertexGenerator<TVertex> generator)
    {
        ShapeRendererDescription = description;
        _shapes = new(shapes);

        IndicesToUpdate.EnsureCapacity(_shapes.Capacity);
        for (int i = 0; i < _shapes.Count; i++)
            IndicesToUpdate.Enqueue(new(i, true, true, 1));
        
        TVertexGenerator = generator;
        IndexGenerator = description.IndexGenerator ?? description.RenderMode switch
        {
            null => throw new ArgumentException("If description.IndexGenerator is null, description.RenderMode can't also be null; at least one of them must be set in order to select an IndexGenerator for this ShapeRenderer", nameof(description)),
            PolygonRenderMode.LineStripWireframe => Shape2DRendererIndexGenerators.LinearIndexGenerator,
            PolygonRenderMode.TriangulatedFill => Shape2DRendererIndexGenerators.TriangulatedIndexGenerator,
            PolygonRenderMode.TriangulatedWireframe => Shape2DRendererIndexGenerators.TriangulatedIndexGenerator,
            _ => throw new NotSupportedException($"Unknown PolygonRenderMode: {description.RenderMode}")
        };
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
                    IndicesToUpdate.Enqueue(new(i, true, true, 0));
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

    /// <inheritdoc/>
    protected override async ValueTask CreateResourceSets(GraphicsDevice device, ResourceSetBuilder builder, ResourceFactory factory)
    {
        await base.CreateResourceSets(device, builder, factory);
        ShapeRendererDescription.ResourceLayoutAndSetBuilder?.Invoke(Manager!, device, factory, builder);
    }

    /// <summary>
    /// Creates a Pipeline using the passed resources and the description
    /// </summary>
    public static Pipeline CreatePipeline(
        GraphicsManager manager, 
        GraphicsDevice device,
        ResourceFactory factory,
        ResourceLayout[] resourceLayouts,
        ShapeRendererDescription description)
    {
        Array.Resize(ref resourceLayouts, resourceLayouts.Length + 1);
        resourceLayouts[^1] = manager.DrawParametersLayout;

        Shader[] shaders = description.Shaders is null
            ? description.VertexShaderSpirv is null && description.FragmentShaderSpirv is null
                ? manager.DefaultResourceCache.DefaultShapeRendererShaders
                : description.VertexShaderSpirv is null || description.FragmentShaderSpirv is null
                    ? throw new InvalidOperationException("Cannot have only one shader description set. Either they must both be set, or they must both be null")
                    : factory.CreateFromSpirv(
                                    (ShaderDescription)description.VertexShaderSpirv,
                                    (ShaderDescription)description.FragmentShaderSpirv
                                )
            : description.Shaders;

        var fillmode = description.FillMode ?? description.RenderMode switch
        {
            null => throw new InvalidOperationException("If description.FillMode is null, description.RenderMode can't also be null; at least one of them must be set to select a FillMode for the pipeline that is being created"),
            PolygonRenderMode.LineStripWireframe or PolygonRenderMode.TriangulatedWireframe => PolygonFillMode.Wireframe,
            PolygonRenderMode.TriangulatedFill => PolygonFillMode.Solid,
            _ => throw new NotSupportedException($"Unknown PolygonRenderMode: {description.RenderMode}")
        };

        var topology = description.Topology ?? description.RenderMode switch
        {
            null => throw new InvalidOperationException("If description.Topology is null, description.RenderMode can't also be null; at least one of them must be set to select a PrimitiveTopology for the pipeline that is being created"),
            PolygonRenderMode.TriangulatedFill or PolygonRenderMode.TriangulatedWireframe => PrimitiveTopology.TriangleStrip,
            PolygonRenderMode.LineStripWireframe => PrimitiveTopology.LineStrip,
            _ => throw new NotSupportedException($"Unknown PolygonRenderMode: {description.RenderMode}")
        };

        var pipeline = description.Pipeline ?? factory.CreateGraphicsPipeline(new(
            description.BlendState,
            description.DepthStencilState,
            new(
                description.FaceCullMode,
                fillmode,
                description.FrontFace,
                description.DepthClipEnabled,
                description.ScissorTestEnabled
            ),
            topology,
            new ShaderSetDescription(new VertexLayoutDescription[]
            {
                description.VertexLayout ?? manager.DefaultResourceCache.DefaultShapeRendererLayout
            }, shaders),
            resourceLayouts,
            device.SwapchainFramebuffer.OutputDescription
        ));

        return pipeline;
    }

    /// <inheritdoc/>
    protected override ValueTask CreateResources(GraphicsDevice device, ResourceFactory factory, ResourceSet[]? resourcesSets, ResourceLayout[]? resourceLayouts)
    {
        Pipeline = CreatePipeline(Manager!, device, factory, resourceLayouts, ShapeRendererDescription);

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
    /// <param name="target">Information about the target <see cref="Framebuffer"/> that this draw operation is being directed into</param>
    protected virtual void DrawShape(TimeSpan delta, in ShapeDat shape, CommandList cl, GraphicsDevice device, FramebufferTargetInfo target)
    {
        cl.SetVertexBuffer(0, shape.Buffer, shape.VertexStart);
        cl.SetIndexBuffer(shape.Buffer!, IndexFormat.UInt16, shape.IndexStart);
        cl.SetPipeline(Pipeline);
        uint index = 0;
        for (; index < ResourceSets.Length; index++)
            cl.SetGraphicsResourceSet(index, ResourceSets[index]);
        cl.SetGraphicsResourceSet(index, target.Parameters.ResourceSet);
        cl.DrawIndexed(shape.IndexCount, 1, 0, 0, 0);
    }

    /// <inheritdoc/>
    protected override ValueTask Draw(TimeSpan delta, CommandList cl, GraphicsDevice device, FramebufferTargetInfo target)
    {
        QueryForChange();
        cl.SetFramebuffer(target.Target);
        var sbl = CollectionsMarshal.AsSpan(ShapeBufferList);
        for (int i = 0; i < sbl.Length; i++)
            DrawShape(delta, in sbl[i], cl, device, target);
        return ValueTask.CompletedTask;
    }

    #region Buffer Updates

    #region Vertex Data

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
    /// <param name="generatorContext">Represents a handle to the <see cref="IShape2DRendererVertexGenerator{TVertex}"/>'s context for the current vertex update batch.</param>
    protected virtual void UpdateVertices(ref ShapeDat pol, CommandList commandList, int index, IShape2DRendererVertexGenerator<TVertex> gen, ref object? generatorContext)
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
            gen.Generate(pol.Shape, Shapes, vertexBuffer, commandList, pol.Buffer, index, pol.VertexStart, vc_bytes, out bool vertexBufferAlreadyUpdated, ref generatorContext);
            if (!vertexBufferAlreadyUpdated)
                commandList.UpdateBuffer(pol.Buffer, pol.VertexStart, vertexBuffer);
        }
        finally
        {
            if (rented is not null)
                ArrayPool<TVertex>.Shared.Return(rented, true);
        }
    }

    #endregion

    #region Index Data

    /// <summary>
    /// Updates the indices of a given shape in the renderer
    /// </summary>
    /// <remarks>
    /// CAUTION: Only override this method if you know what you're doing! By default, this method chooses the appropriate method to define the shape's indices: Triangulation, uploading as-is, pre-triangulated buffers for specific types of shape, etc.
    /// </remarks>
    /// <param name="pol"></param>
    /// <param name="commandList"></param>
    /// <exception cref="InvalidOperationException"></exception>
    protected virtual void UpdateIndices(ref ShapeDat pol, CommandList commandList, int index, IShape2DRendererIndexGenerator gen, ref object? generatorContext, out bool forceUpdateVertices)
    {
        var vsk = VertexSkip;
        var alloc = gen.QueryUInt16BufferSize(pol.Shape, Shapes, index, vsk, out int indexCount, out int indexSpace, ref generatorContext);
        forceUpdateVertices = ShapeDat.ResizeBuffer(ref pol, indexCount, indexSpace, Device!.ResourceFactory);

        ushort[]? rented = null;
        Span<ushort> indexBuffer = alloc
            ? indexCount * 2 > 2048 // 2KB 
                ? (rented = ArrayPool<ushort>.Shared.Rent(indexCount)).AsSpan(0, indexCount)
                : (stackalloc ushort[indexCount])
            : default;

        try
        {
            indexBuffer.Clear();
            gen.GenerateUInt16(pol.Shape, Shapes, indexBuffer, commandList, pol.Buffer, index, indexCount, vsk, pol.IndexStart, pol.IndexCount * 2, out bool isBufferReady, ref generatorContext);

            if (!isBufferReady)
                commandList.UpdateBuffer(pol.Buffer!, pol.IndexStart, indexBuffer);
        }
        finally
        {
            if (rented is not null)
                ArrayPool<ushort>.Shared.Return(rented);
        }
    }

    #endregion

    #endregion

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
            var polybuffer = CollectionsMarshal.AsSpan(ShapeBufferList);

            if (__vertexSkipChanged)
            {
                object? igenContext = null;
                var igen = IndexGenerator;
                __vertexSkipChanged = false;
                for (int i = 0; i < polybuffer.Length; i++)
                {
                    UpdateIndices(ref polybuffer[i], commandList, i, igen, ref igenContext, out bool updatevertices);
                    if (updatevertices)
                        IndicesToUpdate.Enqueue(new(i, true, false, 0));
                }
            }

            if (IndicesToUpdate.TryDequeue(out var dat))
            {
                var vgen = TVertexGenerator;
                var igen = IndexGenerator;
                object? vgenContext = null;
                object? igenContext = null;
                int ind = 0;
                bool forceUpdateVertices = false;
                vgen.Start(this, Shapes, IndicesToUpdate.Count, ref vgenContext);
                do
                {
                    switch (dat.Added)
                    {
                        case < 0:
                            var pd = ShapeBufferList[dat.Index];
                            pd.Dispose();
                            continue;
                        case 0:
                            if (dat.UpdateIndices)
                                UpdateIndices(ref polybuffer[dat.Index], commandList, dat.Index, igen, ref igenContext, out forceUpdateVertices);
                            if (forceUpdateVertices || dat.UpdateVertices)
                                UpdateVertices(ref polybuffer[dat.Index], commandList, ind++, vgen, ref vgenContext);
                            ShapeDat.UpdateLastVer(ref polybuffer[dat.Index]);
                            continue;
                        case > 0:
                            var np = new ShapeDat(_shapes[dat.Index], dat.Index, device.ResourceFactory);
                            UpdateIndices(ref np, commandList, dat.Index, igen, ref igenContext, out _);
                            UpdateVertices(ref np, commandList, ind++, vgen, ref vgenContext);
                            ShapeBufferList.Insert(dat.Index, np);
                            ShapeDat.UpdateLastVer(ref np);
                            continue;
                    }
                } while (IndicesToUpdate.TryDequeue(out dat));
                vgen.Stop(this, ref vgenContext);
            }
        }

        bool changed = false;
        for (int i = ShapeBufferList.Count - 1; i >= 0; i--)
            if (ShapeBufferList[i].remove) 
            {
                changed = true;
                ShapeBufferList.RemoveAt(i);
            }

        if (changed)
        {
            var polybuffer = CollectionsMarshal.AsSpan(ShapeBufferList);
            for (int i = 0; i < polybuffer.Length; i++)
                polybuffer[i].ShapeIndex = i;
        }
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
        /// The offset at which vertex data starts
        /// </summary>
        public uint VertexStart;

        /// <summary>
        /// The offset at which index data starts
        /// </summary>
        public uint IndexStart;

        /// <summary>
        /// The amount of indices in the buffer
        /// </summary>
        public uint IndexCount;

        /// <summary>
        /// This property is used to keep track of changes to the shape, so that it can be re-processed if needed
        /// </summary>
        public int LastVersion { get; private set; }

        /// <summary>
        /// This property is used to keep track of changes to the shape, so that it can be re-processed if needed
        /// </summary>
        public int LastCount { get; private set; }

        /// <summary>
        /// The index of the shape represented by this <see cref="ShapeDat"/> in the renderer that owns it
        /// </summary>
        public int ShapeIndex { get; internal set; }

        /// <summary>
        /// Sets the size of the buffer, taking into account that the indices are triangulated. Adjusts the offsets, and creates or resizes the buffer as needed.
        /// </summary>
        /// <remarks>
        /// If the buffer is created and large enough, only the offsets are updated. If it's <c>null</c> or too small, it's recreated (and disposed of, if necessary)
        /// </remarks>
        public static bool ResizeBuffer(ref ShapeDat dat, int indexCount, int indexSpace, ResourceFactory factory)
        {
            var indexSize = DataStructuring.GetSize<ushort, uint>((uint)indexSpace);
            var vertexSize = DataStructuring.GetSize<TVertex, uint>((uint)dat.Shape.Count);
            var size = vertexSize + indexSize;
            var resize = indexSize > dat.VertexStart;
            
            dat.VertexStart = indexSize;
            dat.IndexStart = 0;

            if (dat.Buffer is null || size > dat.Buffer.SizeInBytes)
            {
                dat.Buffer?.Dispose();
                dat.Buffer = factory.CreateBuffer(new(
                    size,
                    BufferUsage.VertexBuffer | BufferUsage.IndexBuffer
                ));
                resize = true;
            }

            dat.IndexCount = (ushort)indexCount;

            return resize;
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
        /// <param name="shapeIndex"></param>
        public ShapeDat(ShapeDefinition2D def, int shapeIndex, ResourceFactory factory)
        {
            ArgumentNullException.ThrowIfNull(def);
            
            IndexCount = 0;
            ShapeIndex = shapeIndex;
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
