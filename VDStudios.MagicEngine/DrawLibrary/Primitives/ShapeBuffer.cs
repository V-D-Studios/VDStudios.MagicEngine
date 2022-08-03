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

namespace VDStudios.MagicEngine.DrawLibrary.Primitives;

/// <summary>
/// Represents an operation to draw a list of 2D shapes
/// </summary>
public class ShapeBuffer : DrawOperation, IReadOnlyList<ShapeDefinition>
{
    private readonly List<ShapeDefinition> _shapes;

    private ShaderDescription VertexShaderDesc;
    private ShaderDescription FragmentShaderDesc;

    private readonly ShapeBufferDescription Description;

    /// <summary>
    /// Instantiates a new <see cref="ShapeBuffer"/>
    /// </summary>
    /// <param name="polygons">The polygons to fill this list with</param>
    /// <param name="description">Provides data for the configuration of this <see cref="ShapeBuffer"/></param>
    /// <param name="fragmentShaderSpirv">The description of this <see cref="ShapeBuffer"/>'s Fragment Shader in Vulkan style GLSL or SPIR-V bytecode; or <c>null</c> to use the default</param>
    /// <param name="vertexShaderSpirv">The description of this <see cref="ShapeBuffer"/>'s Vertex Shader in Vulkan style GLSL or SPIR-V bytecode; or <c>null</c> to use the default</param>
    public ShapeBuffer(IEnumerable<ShapeDefinition> polygons, ShapeBufferDescription description, ShaderDescription? vertexShaderSpirv = null, ShaderDescription? fragmentShaderSpirv = null)
    {
        Description = description;
        _shapes = new(polygons);

        IndicesToUpdate.EnsureCapacity(_shapes.Capacity);
        for (int i = 0; i < _shapes.Count; i++)
            IndicesToUpdate.Enqueue(new(i, true, true, 1));

        VertexShaderDesc = vertexShaderSpirv ?? new ShaderDescription(ShaderStages.Vertex, BuiltInResources.DefaultPolygonVertexShader.GetUTF8Bytes(), "main");
        FragmentShaderDesc = fragmentShaderSpirv ?? new ShaderDescription(ShaderStages.Fragment, BuiltInResources.DefaultPolygonFragmentShader.GetUTF8Bytes(), "main");
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
    /// The shaders that will be used to render the polygons
    /// </summary>
    protected Shader[] Shaders;

    /// <summary>
    /// The Pipeline that will be used to render the polygons
    /// </summary>
    protected Pipeline Pipeline;

    /// <summary>
    /// The temporary buffer into which the polygons held in this object will be copied for drawing.
    /// </summary>
    /// <remarks>
    /// It's best to leave this property alone, the code in <see cref="ShapeBuffer"/> will take care of it
    /// </remarks>
    protected List<ShapeDat> ShapeBufferList = new();

    #endregion

    /// <inheritdoc/>
    protected override ValueTask CreateWindowSizedResources(GraphicsDevice device, ResourceFactory factory, DeviceBuffer screenSizeBuffer)
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    protected override ValueTask CreateResources(GraphicsDevice device, ResourceFactory factory)
    {
        VertexLayoutDescription vertexLayout = new(
            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));

        Shaders = factory.CreateFromSpirv(
            VertexShaderDesc,
            FragmentShaderDesc
        );

        Pipeline = factory.CreateGraphicsPipeline(new(
            BlendStateDescription.SingleAlphaBlend,
            DepthStencilStateDescription.DepthOnlyLessEqual,
            new(
                FaceCullMode.Front,
                Description.RenderMode switch
                {
                    PolygonRenderMode.LineStripWireframe or PolygonRenderMode.TriangulatedWireframe => PolygonFillMode.Wireframe,
                    PolygonRenderMode.TriangulatedFill => PolygonFillMode.Solid,
                    _ => throw new InvalidOperationException($"Unknown PolygonRenderMode: {Description.RenderMode}")
                },
                FrontFace.Clockwise,
                true,
                false
            ),
            Description.RenderMode switch
            {
                PolygonRenderMode.TriangulatedFill or PolygonRenderMode.TriangulatedWireframe => PrimitiveTopology.TriangleStrip,
                PolygonRenderMode.LineStripWireframe => PrimitiveTopology.LineStrip,
                _ => throw new InvalidOperationException($"Unknown PolygonRenderMode: {Description.RenderMode}")
            },
            new ShaderSetDescription(new VertexLayoutDescription[]
            {
                vertexLayout
            }, Shaders),
            Array.Empty<ResourceLayout>(),
            device.SwapchainFramebuffer.OutputDescription
        ));

        NotifyPendingGPUUpdate();

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    protected override ValueTask Draw(TimeSpan delta, CommandList cl, GraphicsDevice device, Framebuffer mainBuffer, DeviceBuffer screenSizeBuffer)
    {
        QueryForChange();
        cl.SetFramebuffer(mainBuffer);
        foreach(var pd in ShapeBufferList)
        {
            cl.SetVertexBuffer(0, pd.VertexBuffer);
            cl.SetIndexBuffer(pd.IndexBuffer, IndexFormat.UInt16);
            cl.SetPipeline(Pipeline);
            cl.DrawIndexed(pd.CurrentIndexCount, 1, 0, 0, 0);
        }

        return ValueTask.CompletedTask;
    }

    private void UpdateVertices(ref ShapeDat pol, CommandList commandList)
    {
        var vc = pol.Shape.Count;
        Span<Vector2> vertexBuffer = stackalloc Vector2[vc];

        for (int ind = 0; ind < pol.Shape.Count; ind++)
            vertexBuffer[ind] = pol.Shape[ind];
        if (vc < pol.Shape.Count)
            ShapeDat.SetVertexBufferSize(ref pol, Device!.ResourceFactory);
        commandList.UpdateBuffer(pol.VertexBuffer, 0, vertexBuffer);
    }

    private void UpdateIndices(ref ShapeDat pol, CommandList commandList)
    {
        var count = pol.Shape.Count;

        int indexCount = pol.LineStripIndexCount;
        if (count <= 3 || Description.RenderMode is PolygonRenderMode.LineStripWireframe)
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

        // For Convex polygons
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

        indexCount = (count - 1) * 3;

        Span<ushort> buffer = stackalloc ushort[indexCount];
        int bufind = 0;

        ushort p0 = 0;
        ushort pHelper = 1;
        ushort pTemp;

        for (ushort i = 1; i < count; i++)
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
    /// Represents polygon and device related data
    /// </summary>
    protected struct ShapeDat : IDisposable
    {
        internal bool remove = false;
        /// <summary>
        /// The polygon in question
        /// </summary>
        public readonly ShapeDefinition Shape;

        /// <summary>
        /// The buffer holding the vertex data for this polygon
        /// </summary>
        public DeviceBuffer VertexBuffer = null;

        /// <summary>
        /// The buffer holding the index data for this polygon
        /// </summary>
        /// <remarks>
        /// This buffer will be null until <see cref="SetTriangulatedIndicesBufferSize"/> or <see cref="SetLineStripIndicesBufferSize"/> is called. This is guaranteed, by the methods of <see cref="ShapeBuffer"/>, to be the case before <see cref="Draw(TimeSpan, CommandList, GraphicsDevice, Framebuffer, DeviceBuffer)"/> is called
        /// </remarks>
        public DeviceBuffer? IndexBuffer = null;

        /// <summary>
        /// The actual current count of indices for this polygon
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
                (uint)(Unsafe.SizeOf<Vector2>() * dat.Shape.Count),
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
