using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace VDStudios.MagicEngine.DrawLibrary.Primitives;

/// <summary>
/// Represents a 2D shape with arbitrary sides
/// </summary>
/// <remarks>
/// Not to be confused with Polyhedron, a 3D shape with arbitrary faces
/// </remarks>
public class PolygonList : DrawOperation, IList<PolygonDefinition>
{
    private readonly List<PolygonDefinition> _polygons = new();

    #region List

    /// <inheritdoc/>
    public int IndexOf(PolygonDefinition item)
    {
        lock (_polygons)
            return ((IList<PolygonDefinition>)_polygons).IndexOf(item);
    }

    /// <inheritdoc/>
    public void Insert(int index, PolygonDefinition item)
    {
        lock (_polygons)
            ((IList<PolygonDefinition>)_polygons).Insert(index, item);
    }

    /// <inheritdoc/>
    public void RemoveAt(int index)
    {
        lock (_polygons)
            ((IList<PolygonDefinition>)_polygons).RemoveAt(index);
    }

    /// <inheritdoc/>
    public PolygonDefinition this[int index]
    {
        get
        {
            lock (_polygons)
                return ((IList<PolygonDefinition>)_polygons)[index];
        }

        set
        {
            lock (_polygons)
                ((IList<PolygonDefinition>)_polygons)[index] = value;
        }
    }

    /// <inheritdoc/>
    public void Add(PolygonDefinition item)
    {
        lock (_polygons)
            ((ICollection<PolygonDefinition>)_polygons).Add(item);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        lock (_polygons)
            ((ICollection<PolygonDefinition>)_polygons).Clear();
    }

    /// <inheritdoc/>
    public bool Contains(PolygonDefinition item)
    {
        lock (_polygons)
            return ((ICollection<PolygonDefinition>)_polygons).Contains(item);
    }

    /// <inheritdoc/>
    public void CopyTo(PolygonDefinition[] array, int arrayIndex)
    {
        lock (_polygons)
            ((ICollection<PolygonDefinition>)_polygons).CopyTo(array, arrayIndex);
    }

    /// <inheritdoc/>
    public bool Remove(PolygonDefinition item)
    {
        lock (_polygons)
            return ((ICollection<PolygonDefinition>)_polygons).Remove(item);
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

    protected override ValueTask CreateWindowSizedResources(GraphicsDevice device, ResourceFactory factory, DeviceBuffer screenSizeBuffer)
    {
        throw new NotImplementedException();
    }

    protected override ValueTask CreateResources(GraphicsDevice device, ResourceFactory factory)
    {
        throw new NotImplementedException();
    }

    protected override ValueTask Draw(TimeSpan delta, CommandList commandList, GraphicsDevice device, Framebuffer mainBuffer, DeviceBuffer screenSizeBuffer)
    {
        throw new NotImplementedException();
    }

    protected override ValueTask UpdateGPUState(GraphicsDevice device, CommandList commandList, DeviceBuffer screenSizeBuffer)
    {
        throw new NotImplementedException();
    }

    #endregion
}
