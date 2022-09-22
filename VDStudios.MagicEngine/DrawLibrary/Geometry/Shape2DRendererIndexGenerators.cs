using System.Buffers;
using Vulkan;

namespace VDStudios.MagicEngine.DrawLibrary.Geometry;

/// <summary>
/// Contains static references to instances of <see cref="IShape2DRendererIndexGenerator"/> that are included in this library
/// </summary>
public static class Shape2DRendererIndexGenerators
{
    /// <summary>
    /// An index generator that generates indices that, when uploaded to an index buffer, cause the rendering pipeline to draw triangles across the shape
    /// </summary>
    public static IShape2DRendererIndexGenerator TriangulatedIndexGenerator { get; } = new Shape2DTriangulatedIndexGenerator();

    /// <summary>
    /// An index generator that merely outputs a sequence from 0 to the expected number of indices, respecting the set vertex skip if any
    /// </summary>
    public static IShape2DRendererIndexGenerator LinearIndexGenerator { get; } = new Shape2DLinearIndexGenerator();
}
