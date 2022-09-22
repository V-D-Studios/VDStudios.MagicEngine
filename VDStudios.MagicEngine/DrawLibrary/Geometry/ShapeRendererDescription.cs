﻿using System.Numerics;
using Veldrid;
using Veldrid.SPIRV;

namespace VDStudios.MagicEngine.DrawLibrary.Geometry;

/// <summary>
/// Represents a description to configure a <see cref="ShapeRenderer{TVertex}"/>
/// </summary>
public struct ShapeRendererDescription
{
    /// <summary>
    /// A description of the blend state, which controls how color values are blended into each color target.
    /// </summary>
    public BlendStateDescription BlendState;

    /// <summary>
    /// A description of the depth stencil state, which controls depth tests, writing, and comparisons.
    /// </summary>
    public DepthStencilStateDescription DepthStencilState;

    /// <summary>
    /// Controls which face will be culled.
    /// </summary>
    public FaceCullMode FaceCullMode;

    /// <summary>
    /// Controls the winding order used to determine the front face of primitives.
    /// </summary>
    public FrontFace FrontFace;

    /// <summary>
    /// Controls whether depth clipping is enabled.
    /// </summary>
    public bool DepthClipEnabled;

    /// <summary>
    /// Controls whether the scissor test is enabled.
    /// </summary>
    public bool ScissorTestEnabled;

    /// <summary>
    /// The primitive topology of the Pipeline that will be created.
    /// </summary>
    /// <remarks>
    /// Ignored if <see cref="Pipeline"/> is set. If <see langword="null"/> it will be selected automatically using <see cref="RenderMode"/>
    /// </remarks>
    public PrimitiveTopology? Topology;

    /// <summary>
    /// The polygon fillmode of the Pipeline that will be created.
    /// </summary>
    /// <remarks>
    /// Ignored if <see cref="Pipeline"/> is set. If <see langword="null"/> it will be selected automatically using <see cref="RenderMode"/>
    /// </remarks>
    public PolygonFillMode? FillMode;

    /// <summary>
    /// Describes how the polygons for the destination <see cref="ShapeRenderer{TVertex}"/> will be rendered
    /// </summary>
    public PolygonRenderMode? RenderMode;

    /// <summary>
    /// Describes the vertex buffer's structure for the given <see cref="ShapeRenderer{TVertex}"/>
    /// </summary>
    public VertexLayoutDescription? VertexLayout;

    /// <summary>
    /// Represents the *Graphics* pipeline that will be used by the <see cref="ShapeRenderer{TVertex}"/>
    /// </summary>
    public Pipeline? Pipeline;

    /// <summary>
    /// Describes the Vertex shader for the <see cref="ShapeRenderer{TVertex}"/> in Vulkan style GLSL or SPIR-V bytecode
    /// </summary>
    public ShaderDescription? VertexShaderSpirv;

    /// <summary>
    /// Describes the Fragment shader for the <see cref="ShapeRenderer{TVertex}"/> in Vulkan style GLSL or SPIR-V bytecode
    /// </summary>
    public ShaderDescription? FragmentShaderSpirv;

    /// <summary>
    /// The index generator that will be used by the <see cref="ShapeRenderer{TVertex}"/>
    /// </summary>
    /// <remarks>
    /// If <see langword="null"/> it will be selected from the defaults using <see cref="RenderMode"/>
    /// </remarks>
    public IShape2DRendererIndexGenerator IndexGenerator;

    /// <summary>
    /// Represents the Shader array for the <see cref="ShapeRenderer{TVertex}"/>
    /// </summary>
    /// <remarks>
    /// Generally, this contains a single pair of Vertex and Fragment shaders, and is created with <see cref="ResourceFactoryExtensions.CreateFromSpirv(ResourceFactory, ShaderDescription, ShaderDescription)"/>
    /// </remarks>
    public Shader[]? Shaders;

    /// <summary>
    /// Represents the method that will be used to build an array of <see cref="ResourceSet"/>s and <see cref="ResourceLayout"/>s for the <see cref="ShapeRenderer{TVertex}"/>
    /// </summary>
    public ResourceBuilder? ResourceLayoutAndSetBuilder;

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
    /// <param name="resourceLayoutAndSetBuilder">Represents the method that will be used to build a set of <see cref="ResourceLayout"/>s and <see cref="ResourceSet"/>s for the <see cref="ShapeRenderer{TVertex}"/>; or <c>null</c> to use an empty set</param>
    public ShapeRendererDescription(BlendStateDescription blendState, DepthStencilStateDescription depthStencilState, FaceCullMode faceCullMode, FrontFace frontFace, bool depthClipEnabled, bool scissorTestEnabled, PolygonRenderMode renderMode, VertexLayoutDescription? vertexLayout, ShaderDescription? vertexShaderSpirv, ShaderDescription? fragmentShaderSpirv, MagicEngine.ResourceBuilder? resourceLayoutAndSetBuilder)
    {
        BlendState = blendState;
        DepthStencilState = depthStencilState;
        FaceCullMode = faceCullMode;
        FrontFace = frontFace;
        DepthClipEnabled = depthClipEnabled;
        ScissorTestEnabled = scissorTestEnabled;
        RenderMode = renderMode;
        VertexLayout = vertexLayout;

        if (vertexShaderSpirv is ShaderDescription vsd && !vsd.Stage.HasFlag(ShaderStages.Vertex))
            throw new ArgumentException("Cannot pass a ShaderDescription whose stage is not set to Vertex", nameof(vertexShaderSpirv));
        VertexShaderSpirv = vertexShaderSpirv;

        if (fragmentShaderSpirv is ShaderDescription fsd && !fsd.Stage.HasFlag(ShaderStages.Fragment))
            throw new ArgumentException("Cannot pass a ShaderDescription whose stage is not set to Fragment", nameof(fragmentShaderSpirv));
        FragmentShaderSpirv = fragmentShaderSpirv;
        
        ResourceLayoutAndSetBuilder = resourceLayoutAndSetBuilder;
    }
}
