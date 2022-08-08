using VDStudios.MagicEngine.Geometry;

namespace VDStudios.MagicEngine.DrawLibrary.Geometry;

/// <summary>
/// Represents the way a given <see cref="ShapeRenderer"/> will render an arbitrary polygon
/// </summary>
public enum PolygonRenderMode : byte
{
    /// <summary>
    /// Uses the <see cref="PolygonDefinition"/>'s vertices in the order they are defined in, and renders it as a line strip
    /// </summary>
    LineStripWireframe,

    /// <summary>
    /// Triangulates the <see cref="PolygonDefinition"/> and renders it as a filled triangle strip
    /// </summary>
    TriangulatedFill,

    /// <summary>
    /// Triangulates the <see cref="PolygonDefinition"/> and renders it as a wireframe triangle strip
    /// </summary>
    TriangulatedWireframe,
}
