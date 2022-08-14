using System.Numerics;
using Veldrid;

namespace VDStudios.MagicEngine.DrawLibrary.Geometry;

/// <summary>
/// Represents a description to configure a <see cref="ShapeRenderer{TVertex}"/>
/// </summary>
public readonly struct ShapeRendererDescription
{
    /// <summary>
    /// A description of the blend state, which controls how color values are blended into each color target.
    /// </summary>
    public BlendStateDescription BlendState { get; init; }

    /// <summary>
    /// A description of the depth stencil state, which controls depth tests, writing, and comparisons.
    /// </summary>
    public DepthStencilStateDescription DepthStencilState { get; init; }

    /// <summary>
    /// Controls which face will be culled.
    /// </summary>
    public FaceCullMode FaceCullMode { get; init; }

    /// <summary>
    /// Controls the winding order used to determine the front face of primitives.
    /// </summary>
    public FrontFace FrontFace { get; init; }

    /// <summary>
    /// Controls whether depth clipping is enabled.
    /// </summary>
    public bool DepthClipEnabled { get; init; }

    /// <summary>
    /// Controls whether the scissor test is enabled.
    /// </summary>
    public bool ScissorTestEnabled { get; init; }

    /// <summary>
    /// Describes how the polygons for the destination <see cref="ShapeRenderer{TVertex}"/> will be rendered
    /// </summary>
    public PolygonRenderMode RenderMode { get; init; }

    /// <summary>
    /// Describes the vertex buffer's structure for the given <see cref="ShapeRenderer{TVertex}"/>
    /// </summary>
    public VertexLayoutDescription? VertexLayout { get; init; }

    /// <summary>
    /// Describes the Vertex shader for the <see cref="ShapeRenderer{TVertex}"/> in Vulkan style GLSL or SPIR-V bytecode
    /// </summary>
    public ShaderDescription? VertexShaderSpirv { get; init; }

    /// <summary>
    /// Describes the Fragment shader for the <see cref="ShapeRenderer{TVertex}"/> in Vulkan style GLSL or SPIR-V bytecode
    /// </summary>
    public ShaderDescription? FragmentShaderSpirv { get; init; }

    /// <summary>
    /// Represents the method that will be used to build an array of <see cref="ResourceSet"/>s for the <see cref="ShapeRenderer{TVertex}"/>
    /// </summary>
    public ResourceSetBuilder? ResourceSetBuilder { get; init; }

    /// <summary>
    /// Represents the method that will be used to build a set of <see cref="ResourceLayout"/>s for the <see cref="ShapeRenderer{TVertex}"/>
    /// </summary>
    public ResourceLayoutBuilder? ResourceLayoutBuilder { get; init; }

    /// <summary>
    /// Creates a new <see cref="ShapeRendererDescription"/>
    /// </summary>
    /// <param name="blendState">A description of the blend state, which controls how color values are blended into each color target</param>
    /// <param name="depthStencilState">A description of the depth stencil state, which controls depth tests, writing, and comparisons</param>
    /// <param name="faceCullMode">Controls which face will be culled</param>
    /// <param name="frontFace">Controls the winding order used to determine the front face of primitives</param>
    /// <param name="depthClipEnabled">Controls whether depth clipping is enabled</param>
    /// <param name="scissorTestEnabled">Controls whether the scissor test is enabled</param>
    /// <param name="renderMode">Describes how the polygons will be rendered</param>
    /// <param name="vertexLayout">Describes the vertex buffer's structure; or <c>null</c> to use the default (A single element with the structure of a <see cref="Vector2"/>)</param>
    /// <param name="vertexShaderSpirv">Describes the Vertex shader in Vulkan style GLSL or SPIR-V bytecode; or <c>null</c> to use the default</param>
    /// <param name="fragmentShaderSpirv">Describes the Fragment shader in Vulkan style GLSL or SPIR-V bytecode; or <c>null</c> to use the default</param>
    /// <param name="resourceLayoutBuilder">Represents the method that will be used to build a set of <see cref="ResourceLayout"/>s for the <see cref="ShapeRenderer{TVertex}"/>; or <c>null</c> to use an empty set</param>
    /// <param name="resourceSetBuilder">Represents the method that will be used to build an array of <see cref="ResourceSet"/>s for the <see cref="ShapeRenderer{TVertex}"/>; or <c>null</c> to use an empty array</param>
    public ShapeRendererDescription(BlendStateDescription blendState, DepthStencilStateDescription? depthStencilState, FaceCullMode faceCullMode, FrontFace frontFace, bool depthClipEnabled, bool scissorTestEnabled, PolygonRenderMode renderMode, VertexLayoutDescription? vertexLayout, ShaderDescription? vertexShaderSpirv, ShaderDescription? fragmentShaderSpirv, ResourceSetBuilder? resourceSetBuilder, ResourceLayoutBuilder? resourceLayoutBuilder)
    {
        BlendState = blendState;
        DepthStencilState = depthStencilState;
        FaceCullMode = faceCullMode;
        FrontFace = frontFace;
        DepthClipEnabled = depthClipEnabled;
        ScissorTestEnabled = scissorTestEnabled;
        RenderMode = renderMode;
        VertexLayout = vertexLayout;
        VertexShaderSpirv = vertexShaderSpirv;
        FragmentShaderSpirv = fragmentShaderSpirv;
        ResourceSetBuilder = resourceSetBuilder;
        ResourceLayoutBuilder = resourceLayoutBuilder;
    }
}
