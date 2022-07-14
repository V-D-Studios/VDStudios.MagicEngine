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
using Veldrid;
using Veldrid.SPIRV;

namespace VDStudios.MagicEngine.DrawLibrary.Primitives;

/// <summary>
/// Represents an operation to draw a list of 2D shapes with arbitrary sides
/// </summary>
/// <remarks>
/// Not to be confused with Polyhedron, a 3D shape. This class implements all elements of <see cref="IList{T}"/> except for <see cref="ICollection{T}.Remove(T)"/>
/// </remarks>
public class PolygonList : DrawOperation, IReadOnlyList<PolygonDefinition>
{
    private readonly List<PolygonDefinition> _polygons;

    private ShaderDescription VertexShaderDesc;
    private ShaderDescription FragmentShaderDesc;

    private readonly PolygonListDescription Description;

    /// <summary>
    /// Instantiates a new <see cref="PolygonList"/>
    /// </summary>
    /// <param name="polygons">The polygons to fill this list with</param>
    /// <param name="description">Provides data for the configuration of this <see cref="PolygonList"/></param>
    /// <param name="fragmentShaderSpirv">The description of this <see cref="PolygonList"/>'s Fragment Shader in Vulkan style GLSL or SPIR-V bytecode; or <c>null</c> to use the default</param>
    /// <param name="vertexShaderSpirv">The description of this <see cref="PolygonList"/>'s Vertex Shader in Vulkan style GLSL or SPIR-V bytecode; or <c>null</c> to use the default</param>
    public PolygonList(IEnumerable<PolygonDefinition> polygons, PolygonListDescription description, ShaderDescription? vertexShaderSpirv = null, ShaderDescription? fragmentShaderSpirv = null)
    {
        Description = description;
        _polygons = new(polygons);

        IndicesToUpdate.EnsureCapacity(_polygons.Capacity);
        for (int i = 0; i < _polygons.Count; i++)
            IndicesToUpdate.Enqueue(new(i, true, true, 1));

        VertexShaderDesc = vertexShaderSpirv ?? new ShaderDescription(ShaderStages.Vertex, BuiltInResources.DefaultPolygonVertexShader.GetUTF8Bytes(), "main");
        FragmentShaderDesc = fragmentShaderSpirv ?? new ShaderDescription(ShaderStages.Fragment, BuiltInResources.DefaultPolygonFragmentShader.GetUTF8Bytes(), "main");
    }

    #region List

    private readonly Queue<UpdateDat> IndicesToUpdate = new();

    /// <inheritdoc/>
    public int IndexOf(PolygonDefinition item)
    {
        lock (_polygons)
        {
            return ((IList<PolygonDefinition>)_polygons).IndexOf(item);
        }
    }

    /// <inheritdoc/>
    public void Insert(int index, PolygonDefinition item)
    {
        lock (_polygons)
        {
            ((IList<PolygonDefinition>)_polygons).Insert(index, item);
            IndicesToUpdate.Enqueue(new(index, true, true, 1));
            NotifyPendingGPUUpdate();
        }
    }

    /// <inheritdoc/>
    public void RemoveAt(int index)
    {
        lock (_polygons)
        {
            ((IList<PolygonDefinition>)_polygons).RemoveAt(index);
            IndicesToUpdate.Enqueue(new(index, false, false, -1));
            NotifyPendingGPUUpdate();
        }
    }

    /// <inheritdoc/>
    public PolygonDefinition this[int index]
    {
        get
        {
            lock (_polygons)
            {
                return ((IList<PolygonDefinition>)_polygons)[index];
            }
        }

        set
        {
            lock (_polygons)
            {
                ((IList<PolygonDefinition>)_polygons)[index] = value;
                IndicesToUpdate.Enqueue(new(index, true, true, 1));
                NotifyPendingGPUUpdate();
            }
        }
    }

    /// <inheritdoc/>
    public void Add(PolygonDefinition item)
    {
        lock (_polygons)
        {
            IndicesToUpdate.Enqueue(new(_polygons.Count - 1, true, true, 1));
            ((ICollection<PolygonDefinition>)_polygons).Add(item);
            NotifyPendingGPUUpdate();
        }
    }

    /// <inheritdoc/>
    public void Clear()
    {
        lock (_polygons)
        {
            NotifyPendingGPUUpdate();
            for (int i = 0; i < _polygons.Count; i++)
                IndicesToUpdate.Enqueue(new(i, false, false, -1));
            ((ICollection<PolygonDefinition>)_polygons).Clear();
        }
    }

    /// <inheritdoc/>
    public bool Contains(PolygonDefinition item)
    {
        lock (_polygons)
        {
            return ((ICollection<PolygonDefinition>)_polygons).Contains(item);
        }
    }

    /// <inheritdoc/>
    public void CopyTo(PolygonDefinition[] array, int arrayIndex)
    {
        lock (_polygons)
        {
            ((ICollection<PolygonDefinition>)_polygons).CopyTo(array, arrayIndex);
        }
    }

    /// <inheritdoc/>
    public int Count => ((ICollection<PolygonDefinition>)_polygons).Count;

    /// <inheritdoc/>
    public IEnumerator<PolygonDefinition> GetEnumerator()
    {
        return ((IEnumerable<PolygonDefinition>)_polygons).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_polygons).GetEnumerator();
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
    /// It's best to leave this property alone, the code in <see cref="PolygonList"/> will take care of it
    /// </remarks>
    protected List<PolygonDat> PolygonBuffer = new();

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
                PolygonFillMode.Solid,
                FrontFace.Clockwise,
                true,
                false
            ),
            PrimitiveTopology.LineStrip,
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
        cl.SetFramebuffer(mainBuffer);
        foreach(var pd in PolygonBuffer)
        {
            cl.SetVertexBuffer(0, pd.VertexBuffer);
            cl.SetIndexBuffer(pd.IndexBuffer, IndexFormat.UInt16);
            cl.SetPipeline(Pipeline);
            cl.DrawIndexed(pd.IndexCount, 1, 0, 0, 0);
        }

        return ValueTask.CompletedTask;
    }

    private void UpdateVertices(in PolygonDat pol, CommandList commandList)
    {
        var vec2Pool = ArrayPool<Vector2>.Shared;
        int i = 0;
        for (; i < _polygons.Count; i++)
        {
            var bc = pol.Polygon.Count;
            var vertexBuffer = vec2Pool.Rent(bc);
            try
            {
                for (int ind = 0; ind < pol.Polygon.Count; ind++)
                    vertexBuffer[ind] = pol.Polygon[ind];
                commandList.UpdateBuffer(pol.VertexBuffer, 0, vertexBuffer.AsSpan(0, bc));
            }
            finally
            {
                vec2Pool.Return(vertexBuffer);
            }
        }
    }

    private void UpdateIndices(in PolygonDat pol, CommandList commandList)
    {
        var ushortPool = ArrayPool<ushort>.Shared;
        int i = 0;
        for (; i < _polygons.Count; i++)
        {
            var bc = pol.IndexCount;
            var indexBuffer = ushortPool.Rent(bc);
            try
            {
                for (int ind = 0; ind < pol.Polygon.Count; ind++)
                    indexBuffer[ind] = (ushort)ind;
                indexBuffer[pol.Polygon.Count] = 0;
                commandList.UpdateBuffer(pol.IndexBuffer, 0, indexBuffer.AsSpan(0, bc));
            }
            finally
            {
                ushortPool.Return(indexBuffer);
            }
        }
    }

    /// <inheritdoc/>
    protected override ValueTask UpdateGPUState(GraphicsDevice device, CommandList commandList, DeviceBuffer screenSizeBuffer)
    {
        lock (_polygons)
        {
            while (IndicesToUpdate.TryDequeue(out var dat))
            {
                switch (dat.Added)
                {
                    case < 0:
                        var pd = PolygonBuffer[dat.Index];
                        pd.Dispose();
                        continue;
                    case 0:
                        var polybuffer = CollectionsMarshal.AsSpan(PolygonBuffer);
                        if (dat.UpdateVertices)
                            UpdateVertices(in polybuffer[dat.Index], commandList);
                        if (dat.UpdateIndices)
                            UpdateIndices(in polybuffer[dat.Index], commandList);
                        continue;
                    case > 0:
                        var np = new PolygonDat(_polygons[dat.Index], device.ResourceFactory);
                        UpdateVertices(in np, commandList);
                        UpdateIndices(in np, commandList);
                        PolygonBuffer.Insert(dat.Index, np);
                        continue;
                }
            }
        }

        for (int i = PolygonBuffer.Count - 1; i >= 0; i--)
            if (PolygonBuffer[i].remove)
                PolygonBuffer.RemoveAt(i);

        return ValueTask.CompletedTask;
    }

    #endregion

    #region Helper Classes

    private readonly record struct UpdateDat(int Index, bool UpdateVertices, bool UpdateIndices, sbyte Added);

    /// <summary>
    /// Represents polygon and device related data
    /// </summary>
    protected struct PolygonDat : IDisposable
    {
        internal bool remove = false;
        /// <summary>
        /// The polygon in question
        /// </summary>
        public readonly PolygonDefinition Polygon;

        /// <summary>
        /// The buffer holding the vertex data for this polygon
        /// </summary>
        public readonly DeviceBuffer VertexBuffer;

        /// <summary>
        /// The buffer holding the index data for this polygon
        /// </summary>
        public readonly DeviceBuffer IndexBuffer;

        /// <summary>
        /// The count of indices in this Polygon
        /// </summary>
        public readonly ushort IndexCount;

        public PolygonDat(PolygonDefinition def, ResourceFactory factory)
        {
            if (def.RefEquals(default)) 
                throw new ArgumentException($"PolygonDefinition def cannot be an empty struct (default)", nameof(def));
            
            VertexBuffer = factory.CreateBuffer(new(
                (uint)(Unsafe.SizeOf<Vector2>() * def.Count),
                BufferUsage.VertexBuffer
            ));

            IndexBuffer = factory.CreateBuffer(new(
                sizeof(ushort) * ((uint)def.Count + 1),
                BufferUsage.IndexBuffer
            ));

            IndexCount = (ushort)(def.Count + 1);

            Polygon = def;
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
