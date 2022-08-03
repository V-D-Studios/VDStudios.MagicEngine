using Veldrid;

namespace VDStudios.MagicEngine.DrawLibrary.Primitives;

/// <summary>
/// Represents a description to configure a <see cref="ShapeBuffer"/>
/// </summary>
public struct PolygonListDescription
{
    /// <summary>
    /// Describes how the polygons for the destination <see cref="ShapeBuffer"/> will be rendered
    /// </summary>
    public PolygonRenderMode RenderMode;
}
