using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    /// <summary>
    /// Instantiates a new <see cref="PolygonList"/>
    /// </summary>
    /// <param name="polygons">The polygons to fill this list with</param>
    /// <param name="fragmentShaderSpirv">The description of this <see cref="PolygonList"/>'s Fragment Shader in Vulkan style GLSL or SPIR-V bytecode; or <c>null</c> to use the default</param>
    /// <param name="vertexShaderSpirv">The description of this <see cref="PolygonList"/>'s Vertex Shader in Vulkan style GLSL or SPIR-V bytecode; or <c>null</c> to use the default</param>
    public PolygonList(IEnumerable<PolygonDefinition> polygons, ShaderDescription? vertexShaderSpirv = null, ShaderDescription? fragmentShaderSpirv = null)
    {
        _polygons = new(polygons);
        VertexShaderDesc = vertexShaderSpirv ?? new ShaderDescription(ShaderStages.Vertex, BuiltInResources.DefaultPolygonVertexShader, "main");
        FragmentShaderDesc = fragmentShaderSpirv ?? new ShaderDescription(ShaderStages.Fragment, BuiltInResources.DefaultPolygonFragmentShader, "main");
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
    /// The temporary buffer into which the polygons held in this object will be copied for drawing. This array may be larger than necessary, see <see cref="PolygonBufferFill"/>
    /// </summary>
    /// <remarks>
    /// It's best to leave this property alone, the code in <see cref="PolygonList"/> will take care of it
    /// </remarks>
    protected PolygonDefinition[] PolygonBuffer = Array.Empty<PolygonDefinition>();

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
        Shaders = factory.CreateFromSpirv(VertexShaderDesc, FragmentShaderDesc);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    protected override ValueTask Draw(TimeSpan delta, CommandList commandList, GraphicsDevice device, Framebuffer mainBuffer, DeviceBuffer screenSizeBuffer)
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    protected override ValueTask UpdateGPUState(GraphicsDevice device, CommandList commandList, DeviceBuffer screenSizeBuffer)
    {
        throw new NotImplementedException();
    }

    #endregion
}
