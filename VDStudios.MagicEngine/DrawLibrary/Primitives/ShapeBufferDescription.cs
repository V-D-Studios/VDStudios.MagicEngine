using System.Numerics;
using Veldrid;

namespace VDStudios.MagicEngine.DrawLibrary.Primitives;

/// <summary>
/// Represents a description to configure a <see cref="ShapeBuffer{TVertex}"/>
/// </summary>
public readonly struct ShapeBufferDescription
{
    /// <summary>
    /// Describes how the polygons for the destination <see cref="ShapeBuffer{TVertex}"/> will be rendered
    /// </summary>
    public PolygonRenderMode RenderMode { get; init; }

    /// <summary>
    /// Describes the vertex buffer's structure for the given <see cref="ShapeBuffer{TVertex}"/>
    /// </summary>
    public VertexLayoutDescription? VertexLayout { get; init; }

    /// <summary>
    /// Describes the Vertex shader for the <see cref="ShapeBuffer{TVertex}"/> in Vulkan style GLSL or SPIR-V bytecode
    /// </summary>
    public ShaderDescription? VertexShaderSpirv { get; init; }

    /// <summary>
    /// Describes the Fragment shader for the <see cref="ShapeBuffer{TVertex}"/> in Vulkan style GLSL or SPIR-V bytecode
    /// </summary>
    public ShaderDescription? FragmentShaderSpirv { get; init; }

    /// <summary>
    /// Represents the method that will be used to build a set of <see cref="ResourceLayout"/>s for the <see cref="ShapeBuffer{TVertex}"/>
    /// </summary>
    public ResourceLayoutBuilder? ResourceLayoutBuilder { get; init; }

    /// <summary>
    /// Creates a new <see cref="ShapeBufferDescription"/>
    /// </summary>
    /// <param name="renderMode">Describes how the polygons will be rendered</param>
    /// <param name="vertexLayout">Describes the vertex buffer's structure; or <c>null</c> to use the default (A single element with the structure of a <see cref="Vector2"/>)</param>
    /// <param name="vertexShaderSpirv">Describes the Vertex shader in Vulkan style GLSL or SPIR-V bytecode; or <c>null</c> to use the default</param>
    /// <param name="fragmentShaderSpirv">Describes the Fragment shader in Vulkan style GLSL or SPIR-V bytecode; or <c>null</c> to use the default</param>
    /// <param name="resourceLayoutBuilder">Represents the method that will be used to build a set of <see cref="ResourceLayout"/>s for the <see cref="ShapeBuffer{TVertex}"/>; or <c>null</c> to use an empty set</param>
    public ShapeBufferDescription(PolygonRenderMode renderMode, VertexLayoutDescription? vertexLayout, ShaderDescription? vertexShaderSpirv, ShaderDescription? fragmentShaderSpirv, ResourceLayoutBuilder? resourceLayoutBuilder)
    {
        RenderMode = renderMode;
        VertexLayout = vertexLayout;
        VertexShaderSpirv = vertexShaderSpirv;
        FragmentShaderSpirv = fragmentShaderSpirv;
        ResourceLayoutBuilder = resourceLayoutBuilder;
    }
}
