using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
/// Not to be confused with Polyhedron, a 3D shape
/// </remarks>
public class PolygonList : DrawOperation, IList<PolygonDefinition>
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
        VertexShaderDesc = vertexShaderSpirv ?? new ShaderDescription(ShaderStages.Vertex, BuiltInResources.DefaultPolygonVertexShader.GetUTF8Bytes(), "main");
        FragmentShaderDesc = fragmentShaderSpirv ?? new ShaderDescription(ShaderStages.Fragment, BuiltInResources.DefaultPolygonFragmentShader.GetUTF8Bytes(), "main");
    }

    #region List

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
            NotifyPendingGPUUpdate();
            ((IList<PolygonDefinition>)_polygons).Insert(index, item);
        }
    }

    /// <inheritdoc/>
    public void RemoveAt(int index)
    {
        lock (_polygons)
        {
            NotifyPendingGPUUpdate();
            ((IList<PolygonDefinition>)_polygons).RemoveAt(index);
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
                NotifyPendingGPUUpdate();
                ((IList<PolygonDefinition>)_polygons)[index] = value;
            }
        }
    }

    /// <inheritdoc/>
    public void Add(PolygonDefinition item)
    {
        lock (_polygons)
        {
            NotifyPendingGPUUpdate();
            ((ICollection<PolygonDefinition>)_polygons).Add(item);
        }
    }

    /// <inheritdoc/>
    public void Clear()
    {
        lock (_polygons)
        {
            NotifyPendingGPUUpdate();
            ((ICollection<PolygonDefinition>)_polygons).Clear();
        }
    }

    /// <inheritdoc/>
    public bool Contains(PolygonDefinition item)
    {
        lock (_polygons)
        {
            NotifyPendingGPUUpdate();
            return ((ICollection<PolygonDefinition>)_polygons).Contains(item);
        }
    }

    /// <inheritdoc/>
    public void CopyTo(PolygonDefinition[] array, int arrayIndex)
    {
        lock (_polygons)
        {
            NotifyPendingGPUUpdate();
            ((ICollection<PolygonDefinition>)_polygons).CopyTo(array, arrayIndex);
        }
    }

    /// <inheritdoc/>
    public bool Remove(PolygonDefinition item)
    {
        lock (_polygons)
        {
            NotifyPendingGPUUpdate();
            return ((ICollection<PolygonDefinition>)_polygons).Remove(item);
        }
    }

    /// <inheritdoc/>
    public int Count => ((ICollection<PolygonDefinition>)_polygons).Count;

    bool ICollection<PolygonDefinition>.IsReadOnly => false;

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
    /// The temporary buffer into which the polygons held in this object will be copied for drawing. This array may be larger than necessary, see <see cref="PolygonBufferFill"/>
    /// </summary>
    /// <remarks>
    /// It's best to leave this property alone, the code in <see cref="PolygonList"/> will take care of it
    /// </remarks>
    protected PolygonDat[] PolygonBuffer = Array.Empty<PolygonDat>();

    /// <summary>
    /// The fill of <see cref="PolygonBuffer"/>. Think of <see cref="List{T}.Count"/> vs <see cref="List{T}.Capacity"/>
    /// </summary>
    /// <remarks>
    /// It's best to leave this property alone, the code in <see cref="PolygonList"/> will take care of it. This property is not read-only because <see cref="PolygonBuffer"/> is mutable
    /// </remarks>
    protected int PolygonBufferFill = 0;

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

    /// <inheritdoc/>
    protected override ValueTask UpdateGPUState(GraphicsDevice device, CommandList commandList, DeviceBuffer screenSizeBuffer)
    {
        foreach (var i in PolygonBuffer)
            i.Dispose();

        var ushortPool = ArrayPool<ushort>.Shared;
        var vec2Pool = ArrayPool<Vector2>.Shared;
        lock (_polygons)
        {
            if (_polygons.Count > PolygonBuffer.Length)
                PolygonBuffer = new PolygonDat[_polygons.Capacity];

            int i = 0;
            for (; i < _polygons.Count; i++)
            {
                var pol = PolygonBuffer[i] = new(_polygons[i], device.ResourceFactory);

                var bc = pol.IndexCount;
                var indexBuffer = ushortPool.Rent(bc);
                var vertexBuffer = vec2Pool.Rent(bc - 1);
                try
                {
                    for (int ind = 0; ind < pol.Polygon.Count; ind++)
                    {
                        vertexBuffer[ind] = pol.Polygon[ind];
                        indexBuffer[ind] = (ushort)ind;
                    }
                    indexBuffer[pol.Polygon.Count] = 0;
                    commandList.UpdateBuffer(pol.IndexBuffer, 0, indexBuffer.AsSpan(0, bc));
                    commandList.UpdateBuffer(pol.VertexBuffer, 0, vertexBuffer.AsSpan(0, bc - 1));
                }
                finally
                {
                    ushortPool.Return(indexBuffer);
                    vec2Pool.Return(vertexBuffer);
                }
            }
            for (; i < PolygonBuffer.Length; i++)
                PolygonBuffer[i] = default;

            PolygonBufferFill = _polygons.Count;
        }

        return ValueTask.CompletedTask;
    }

    #endregion

    #region Helper Classes

    /// <summary>
    /// Represents polygon and device related data
    /// </summary>
    protected readonly struct PolygonDat : IDisposable
    {
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
        }
    }

    #endregion
}
