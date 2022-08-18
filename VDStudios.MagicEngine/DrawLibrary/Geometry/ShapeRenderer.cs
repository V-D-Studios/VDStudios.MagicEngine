using SDL2.NET;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using VDStudios.MagicEngine.Geometry;
using Veldrid;
using Veldrid.SPIRV;

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
    public ShapeRenderer(IEnumerable<ShapeDefinition> shapes, ShapeRendererDescription description)
        : base(shapes, description, ShapeVertexGenerator.Default)
    { }
}

/// <summary>
/// Represents an operation to draw a list of 2D shapes
/// </summary>
public class ShapeRenderer<TVertex> : DrawOperation, IReadOnlyList<ShapeDefinition> where TVertex : unmanaged
{
    private readonly List<ShapeDefinition> _shapes;
    
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
    /// Be careful when modifying this -- And know that most changes won't have any effect after <see cref="CreateResources(GraphicsDevice, ResourceFactory)"/> is called
    /// </summary>
    protected ShapeRendererDescription ShapeRendererDescription;
    private ResourceSet[] ResourceSets;

    /// <summary>
    /// Instantiates a new <see cref="ShapeRenderer{TVertex}"/>
    /// </summary>
    /// <param name="shapes">The shapes to fill this list with</param>
    /// <param name="description">Provides data for the configuration of this <see cref="ShapeRenderer{TVertex}"/></param>
    /// <param name="generator">The <see cref="IShapeRendererVertexGenerator{TVertex}"/> object that will generate the vertices for all shapes in the buffer</param>
    public ShapeRenderer(IEnumerable<ShapeDefinition> shapes, ShapeRendererDescription description, IShapeRendererVertexGenerator<TVertex> generator)
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
    public int IndexOf(ShapeDefinition item)
    {
        lock (_shapes)
        {
            return ((IList<ShapeDefinition>)_shapes).IndexOf(item);
        }
    }

    /// <inheritdoc/>
    public void Insert(int index, ShapeDefinition item)
    {
        lock (_shapes)
        {
            ((IList<ShapeDefinition>)_shapes).Insert(index, item);
            IndicesToUpdate.Enqueue(new(index, true, true, 1));
            NotifyPendingGPUUpdate();
        }
    }

    /// <inheritdoc/>
    public void RemoveAt(int index)
    {
        lock (_shapes)
        {
            ((IList<ShapeDefinition>)_shapes).RemoveAt(index);
            IndicesToUpdate.Enqueue(new(index, false, false, -1));
            NotifyPendingGPUUpdate();
        }
    }

    /// <inheritdoc/>
    public ShapeDefinition this[int index]
    {
        get
        {
            lock (_shapes)
            {
                return ((IList<ShapeDefinition>)_shapes)[index];
            }
        }

        set
        {
            lock (_shapes)
            {
                ((IList<ShapeDefinition>)_shapes)[index] = value;
                IndicesToUpdate.Enqueue(new(index, true, true, 1));
                NotifyPendingGPUUpdate();
            }
        }
    }

    /// <inheritdoc/>
    public void Add(ShapeDefinition item)
    {
        lock (_shapes)
        {
            IndicesToUpdate.Enqueue(new(_shapes.Count - 1, true, true, 1));
            ((ICollection<ShapeDefinition>)_shapes).Add(item);
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
            ((ICollection<ShapeDefinition>)_shapes).Clear();
        }
    }

    /// <inheritdoc/>
    public bool Contains(ShapeDefinition item)
    {
        lock (_shapes)
        {
            return ((ICollection<ShapeDefinition>)_shapes).Contains(item);
        }
    }

    /// <inheritdoc/>
    public void CopyTo(ShapeDefinition[] array, int arrayIndex)
    {
        lock (_shapes)
        {
            ((ICollection<ShapeDefinition>)_shapes).CopyTo(array, arrayIndex);
        }
    }

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
                }
            }
    }

    /// <inheritdoc/>
    public int Count => ((ICollection<ShapeDefinition>)_shapes).Count;

    /// <inheritdoc/>
    public IEnumerator<ShapeDefinition> GetEnumerator()
    {
        return ((IEnumerable<ShapeDefinition>)_shapes).GetEnumerator();
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
    /// The shaders that will be used to render the shapes
    /// </summary>
    protected Shader[] Shaders;

    /// <summary>
    /// The Pipeline that will be used to render the shapes
    /// </summary>
    protected Pipeline Pipeline;

    /// <summary>
    /// The temporary buffer into which the shapes held in this object will be copied for drawing.
    /// </summary>
    /// <remarks>
    /// It's best to leave this property alone, the code in <see cref="ShapeRenderer{TVertex}"/> will take care of it
    /// </remarks>
    protected List<ShapeDat> ShapeBufferList = new();

    #endregion

    /// <inheritdoc/>
    protected override ValueTask CreateWindowSizedResources(GraphicsDevice device, ResourceFactory factory, DeviceBuffer screenSizeBuffer)
    {
        return ValueTask.CompletedTask;
    }

    private ShaderDescription vertexDefault = new(ShaderStages.Vertex, BuiltInResources.DefaultPolygonVertexShader.GetUTF8Bytes(), "main");
    private ShaderDescription fragmnDefault = new(ShaderStages.Fragment, BuiltInResources.DefaultPolygonFragmentShader.GetUTF8Bytes(), "main");
    private static readonly VertexLayoutDescription DefaultVector2Layout 
        = new(new VertexElementDescription("Position", VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate));

    /// <summary>
    /// Intercepts the resource layouts and sets for this <see cref="ShapeRenderer"/>. Does nothing by default
    /// </summary>
    /// <remarks>
    /// CAUTION: Make sure the behaviour is thoroughly documented before overriding this method.
    /// </remarks>
    protected virtual void InterceptResources(ref ResourceLayout[] layouts, ref ResourceSet[] sets, ResourceFactory factory) { }

    /// <inheritdoc/>
    protected override ValueTask CreateResources(GraphicsDevice device, ResourceFactory factory)
    {
        ResourceLayout[] layouts;
        ResourceSet[] sets;
        if (ShapeRendererDescription.ResourceLayoutAndSetBuilder is not null)
            ShapeRendererDescription.ResourceLayoutAndSetBuilder.Invoke(Manager!, device, factory, out layouts, out sets);
        else
        {
            layouts = Array.Empty<ResourceLayout>();
            sets = Array.Empty<ResourceSet>();
        }

        if (layouts.Length != sets.Length)
            throw new InvalidOperationException("The length of the ResourceLayout array and ResourceSet array must be equal -- Failure of this condition means that not all layouts and sets correspond");

        InterceptResources(ref layouts, ref sets, factory);

        if (layouts.Length != sets.Length)
            throw new InvalidOperationException("The length of the ResourceLayout array and ResourceSet array must be equal -- Failure of this condition means that not all layouts and sets correspond");

        Shaders = factory.CreateFromSpirv(
            ShapeRendererDescription.VertexShaderSpirv ?? vertexDefault,
            ShapeRendererDescription.FragmentShaderSpirv ?? fragmnDefault
        );

        Pipeline = factory.CreateGraphicsPipeline(new(
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
            }, Shaders),
            layouts,
            device.SwapchainFramebuffer.OutputDescription
        ));

        ResourceSets = sets;

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
    /// <param name="screenSizeBuffer">A <see cref="DeviceBuffer"/> filled with a <see cref="Vector4"/> containing <see cref="GraphicsManager.Window"/>'s size in the form of <c>Vector4(x: Width, y: Height, 0, 0)</c></param>

    protected virtual void DrawShape(TimeSpan delta, ShapeDat shape, CommandList cl, GraphicsDevice device, Framebuffer mainBuffer, DeviceBuffer screenSizeBuffer)
    {
        cl.SetVertexBuffer(0, shape.VertexBuffer);
        cl.SetIndexBuffer(shape.IndexBuffer, IndexFormat.UInt16);
        cl.SetPipeline(Pipeline);
        for (uint index = 0; index < ResourceSets.Length; index++)
            cl.SetGraphicsResourceSet(index, ResourceSets[index]);
        cl.DrawIndexed(shape.CurrentIndexCount, 1, 0, 0, 0);
    }

    /// <inheritdoc/>
    protected override ValueTask Draw(TimeSpan delta, CommandList cl, GraphicsDevice device, Framebuffer mainBuffer, DeviceBuffer screenSizeBuffer)
    {
        QueryForChange();
        cl.SetFramebuffer(mainBuffer);
        foreach (var pd in ShapeBufferList)
            DrawShape(delta, pd, cl, device, mainBuffer, screenSizeBuffer);

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Updates the vertices of a given shape in the renderer
    /// </summary>
    /// <remarks>
    /// CAUTION: Only override this method if you know what you're doing! By default, this method creates a stack-allocated span of <typeparamref name="TVertex"/> instances, generates the vertices using <see cref="TVertexGenerator"/>, and submits it to <see cref="ShapeDat.VertexBuffer"/>, adjusting its size as appropriate
    /// </remarks>
    /// <param name="pol">A reference to the shape's internal data context</param>
    /// <param name="commandList"></param>
    protected virtual void UpdateVertices(ref ShapeDat pol, CommandList commandList)
    {
        var vc = pol.Shape.Count;
        Span<TVertex> vertexBuffer = TVertexGenerator.QueryAllocCPUBuffer(pol.Shape) ? stackalloc TVertex[vc] : default;

        var vc_bytes = (uint)Unsafe.SizeOf<TVertex>() * (uint)vc;

        if (pol.VertexBuffer is null || vc_bytes > pol.VertexBuffer.SizeInBytes)  
            ShapeDat.SetVertexBufferSize(ref pol, Device!.ResourceFactory);

        TVertexGenerator.Generate(pol.Shape, vertexBuffer, commandList, pol.VertexBuffer, out bool vertexBufferAlreadyUpdated);

        if (!vertexBufferAlreadyUpdated)
            commandList.UpdateBuffer(pol.VertexBuffer, 0, vertexBuffer);
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
        var count = pol.Shape.Count;

        int indexCount = pol.LineStripIndexCount;
        if (count <= 3 || ShapeRendererDescription.RenderMode is PolygonRenderMode.LineStripWireframe)
        {
            Span<ushort> indexBuffer = stackalloc ushort[indexCount];
            for (int ind = 0; ind < count; ind++)
                indexBuffer[ind] = (ushort)ind;
            indexBuffer[count] = 0;
            ShapeDat.SetLineStripIndicesBufferSize(ref pol, Device!.ResourceFactory);
            commandList.UpdateBuffer(pol.IndexBuffer!, 0, indexBuffer);
            return;
        }

        // Triangulation

        // For Convex shapes
        // Since we're working exclusively with indices here, this is all data that can be calculated exclusively with a single variable (count).
        // There's probably an easier way to compute this

        if (pol.Shape.IsConvex is false)
            throw new InvalidOperationException($"Triangulation of Concave Polygons is not supported yet");

        if (count is 4) // And is Convex
        {
            ShapeDat.SetTriangulatedIndicesBufferSize(ref pol, 6, Device!.ResourceFactory);
            commandList.UpdateBuffer(pol.IndexBuffer!, 0, stackalloc ushort[6] { 1, 0, 3, 1, 2, 3 });
            return;
        }

        ushort i = count % 2 == 0 ? (ushort)0u : (ushort)1u;
        indexCount = (count - i) * 3;

        Span<ushort> buffer = stackalloc ushort[indexCount];
        int bufind = 0;

        ushort p0 = 0;
        ushort pHelper = 1;
        ushort pTemp;

        for (; i < count; i++)
        {
            pTemp = i;
            buffer[bufind++] = p0;
            buffer[bufind++] = pHelper;
            buffer[bufind++] = pTemp;
            pHelper = pTemp;
        }

        ShapeDat.SetTriangulatedIndicesBufferSize(ref pol, indexCount, Device!.ResourceFactory);
        commandList.UpdateBuffer(pol.IndexBuffer!, 0, buffer);
    }

    /// <inheritdoc/>
    protected override ValueTask UpdateGPUState(GraphicsDevice device, CommandList commandList, DeviceBuffer screenSizeBuffer)
    {
        lock (_shapes)
        {
            while (IndicesToUpdate.TryDequeue(out var dat))
            {
                switch (dat.Added)
                {
                    case < 0:
                        var pd = ShapeBufferList[dat.Index];
                        pd.Dispose();
                        continue;
                    case 0:
                        var polybuffer = CollectionsMarshal.AsSpan(ShapeBufferList);
                        if (dat.UpdateVertices)
                            UpdateVertices(ref polybuffer[dat.Index], commandList);
                        if (dat.UpdateIndices)
                            UpdateIndices(ref polybuffer[dat.Index], commandList);
                        ShapeDat.UpdateLastVer(ref polybuffer[dat.Index]);
                        continue;
                    case > 0:
                        var np = new ShapeDat(_shapes[dat.Index], device.ResourceFactory);
                        UpdateVertices(ref np, commandList);
                        UpdateIndices(ref np, commandList);
                        ShapeBufferList.Insert(dat.Index, np);
                        ShapeDat.UpdateLastVer(ref np);
                        continue;
                }
            }
        }

        for (int i = ShapeBufferList.Count - 1; i >= 0; i--)
            if (ShapeBufferList[i].remove)
                ShapeBufferList.RemoveAt(i);

        return ValueTask.CompletedTask;
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
        public readonly ShapeDefinition Shape;

        /// <summary>
        /// The buffer holding the vertex data for this shape
        /// </summary>
        public DeviceBuffer VertexBuffer = null;

        /// <summary>
        /// The buffer holding the index data for this shape
        /// </summary>
        /// <remarks>
        /// This buffer will be null until <see cref="SetTriangulatedIndicesBufferSize"/> or <see cref="SetLineStripIndicesBufferSize"/> is called. This is guaranteed, by the methods of <see cref="ShapeRenderer{TVertex}"/>, to be the case before <see cref="Draw(TimeSpan, CommandList, GraphicsDevice, Framebuffer, DeviceBuffer)"/> is called
        /// </remarks>
        public DeviceBuffer? IndexBuffer = null;

        /// <summary>
        /// The actual current count of indices for this shape
        /// </summary>
        public ushort CurrentIndexCount;

        /// <summary>
        /// The count of indices in this Polygon
        /// </summary>
        public readonly ushort LineStripIndexCount;

        public int LastVersion;
        public int LastCount;

        public static void SetVertexBufferSize(ref ShapeDat dat, ResourceFactory factory)
        {
            dat.VertexBuffer = factory.CreateBuffer(new(
                (uint)(Unsafe.SizeOf<TVertex>() * dat.Shape.Count),
                BufferUsage.VertexBuffer
            ));
        }

        public static void SetTriangulatedIndicesBufferSize(ref ShapeDat dat, int indexCount, ResourceFactory factory)
        {
            dat.IndexBuffer = factory.CreateBuffer(new(
                sizeof(ushort) * (uint)indexCount,
                BufferUsage.IndexBuffer
            ));
            dat.CurrentIndexCount = (ushort)indexCount;
        }

        public static void SetLineStripIndicesBufferSize(ref ShapeDat dat, ResourceFactory factory)
        {
            dat.IndexBuffer = factory.CreateBuffer(new(
                sizeof(ushort) * (uint)dat.LineStripIndexCount,
                BufferUsage.IndexBuffer
            ));
            dat.CurrentIndexCount = (ushort)dat.LineStripIndexCount;
        }

        public static void UpdateLastVer(ref ShapeDat dat)
        {
            dat.LastVersion = dat.Shape.Version;
            dat.LastCount = dat.Shape.Count;
        }

        public ShapeDat(ShapeDefinition def, ResourceFactory factory)
        {
            ArgumentNullException.ThrowIfNull(def);
            
            LineStripIndexCount = (ushort)(def.Count + 1);
            CurrentIndexCount = 0;
            Shape = def;
            LastVersion = 0;
            LastCount = 0;
        }

        public void Dispose()
        {
            ((IDisposable)VertexBuffer).Dispose();
            ((IDisposable)IndexBuffer).Dispose();
            remove = true;
        }
    }

    #endregion
}
