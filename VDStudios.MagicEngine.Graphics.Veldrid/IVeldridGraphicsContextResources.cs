using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Veldrid;

namespace VDStudios.MagicEngine.Graphics.Veldrid;

/// <summary>
/// Represents a set of shared resources for a <see cref="VeldridGraphicsContext"/>
/// </summary>
public interface IVeldridGraphicsContextResources
{
    /// <summary>
    /// The <see cref="GraphicsDevice"/> for this context
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; }

    /// <summary>
    /// The <see cref="ResourceFactory"/> of <see cref="GraphicsDevice"/>
    /// </summary>
    public ResourceFactory ResourceFactory { get; }

    /// <summary>
    /// The <see cref="ResourceLayout"/> for <see cref="VeldridGraphicsContext.FrameReportSet"/>
    /// </summary>
    public ResourceLayout FrameReportLayout { get; }

    #region Pipelines

    /// <summary>
    /// Gets the pipeline 
    /// </summary>
    /// <param name="index">The index of the pipeline in the <typeparamref name="T"/> pipeline set</param>
    /// <typeparam name="T">The type that the pipeline is for</typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public Pipeline GetPipeline<T>(uint index = 0);

    /// <summary>
    /// Attempts to obtain a <see cref="Pipeline"/> for <typeparamref name="T"/> under <paramref name="index"/>
    /// </summary>
    /// <typeparam name="T">The type that the pipeline is for</typeparam>
    /// <param name="pipeline">The pipeline, <see langword="null"/> if not found</param>
    /// <param name="index">The index of the pipeline in the <typeparamref name="T"/> pipeline set</param>
    /// <returns><see langword="true"/> if the pipeline is found and <paramref name="pipeline"/> has it. <see langword="false"/> otherwise</returns>
    public bool TryGetPipeline<T>([NotNullWhen(true)] out Pipeline? pipeline, uint index = 0);

    /// <summary>
    /// Checks if a <see cref="Pipeline"/> under <typeparamref name="T"/> is registered
    /// </summary>
    /// <param name="index">The index of the pipeline in the <typeparamref name="T"/> pipeline set</param>
    /// <typeparam name="T">The type that the pipeline is for</typeparam>
    /// <returns><see langword="true"/> if a <see cref="Pipeline"/> was found, <see langword="false"/> otherwise</returns>
    public bool ContainsPipeline<T>(uint index = 0);

    /// <summary>
    /// Registers a pipeline into the provided index
    /// </summary>
    /// <param name="pipeline">The pipeline to be registered</param>
    /// <param name="index">The index of the pipeline in the <typeparamref name="T"/> pipeline set</param>
    /// <typeparam name="T">The type that the pipeline is for</typeparam>
    /// <param name="previous">If there was previously a pipeline registered under <paramref name="index"/>, this is that pipeline. Otherwise, <see langword="null"/></param>
    public void RegisterPipeline<T>(Pipeline pipeline, out Pipeline? previous, uint index = 0);

    /// <summary>
    /// Gets the pipeline 
    /// </summary>
    /// <param name="index">The index of the pipeline in the <paramref name="type"/> pipeline set</param>
    /// <param name="type">The type that the pipeline is for</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public Pipeline GetPipeline(Type type, uint index = 0);

    /// <summary>
    /// Attempts to obtain a <see cref="Pipeline"/> for <paramref name="type"/> under <paramref name="index"/>
    /// </summary>
    /// <param name="type">The type of the object requesting the pipeline</param>
    /// <param name="pipeline">The pipeline, <see langword="null"/> if not found</param>
    /// <param name="index">The index of the pipeline in the <paramref name="type"/> pipeline set</param>
    /// <returns><see langword="true"/> if the pipeline is found and <paramref name="pipeline"/> has it. <see langword="false"/> otherwise</returns>
    public bool TryGetPipeline(Type type, [NotNullWhen(true)] out Pipeline? pipeline, uint index = 0);

    /// <summary>
    /// Checks if a <see cref="Pipeline"/> under <paramref name="type"/> is registered
    /// </summary>
    /// <param name="index">The index of the pipeline in the <paramref name="type"/> pipeline set</param>
    /// <param name="type">The type that the pipeline is for</param>
    /// <returns><see langword="true"/> if a <see cref="Pipeline"/> was found, <see langword="false"/> otherwise</returns>
    public bool ContainsPipeline(Type type, uint index = 0);

    /// <summary>
    /// Registers a pipeline into the provided index
    /// </summary>
    /// <param name="pipeline">The pipeline to be registered</param>
    /// <param name="index">The index of the pipeline in the <paramref name="type"/> pipeline set</param>
    /// <param name="type">The type that the pipeline is for</param>
    /// <param name="previous">If there was previously a pipeline registered under <paramref name="index"/>, this is that pipeline. Otherwise, <see langword="null"/></param>
    public void RegisterPipeline(Type type, Pipeline pipeline, out Pipeline? previous, uint index = 0);

    /// <summary>
    /// Exchanges the position of two pipelines registered for <paramref name="type"/>
    /// </summary>
    /// <param name="type">The type that the pipeline is for</param>
    /// <param name="indexA">The original index of the pipeline to move to <paramref name="indexB"/></param>
    /// <param name="indexB">The original index of the pipeline to move to <paramref name="indexA"/></param>
    public void ExchangePipelines(Type type, uint indexA, uint indexB);

    #endregion

    #region Resource Layouts

    /// <summary>
    /// Attempts to obtain a <see cref="ResourceLayout"/> under <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">The type of the object requesting the layout</typeparam>
    /// <param name="layout">The layout, <see langword="null"/> if not found</param>
    /// <returns><see langword="true"/> if the layout is found and <paramref name="layout"/> has it. <see langword="false"/> otherwise</returns>
    public bool TryGetResourceLayout<T>([NotNullWhen(true)] out ResourceLayout? layout);

    /// <summary>
    /// Gets the resourceLayout 
    /// </summary>
    /// <typeparam name="T">The type of the object requesting the layout</typeparam>
    /// <exception cref="ArgumentException"></exception>
    public ResourceLayout GetResourceLayout<T>();

    /// <summary>
    /// Checks if a <see cref="ResourceLayout"/> under <typeparamref name="T"/> is registered
    /// </summary>
    /// <typeparam name="T">The type of the object requesting the layout</typeparam>
    /// <returns><see langword="true"/> if a <see cref="ResourceLayout"/> was found, <see langword="false"/> otherwise</returns>
    public bool ContainsResourceLayout<T>();

    /// <summary>
    /// Registers a resource layout into the provided name
    /// </summary>
    /// <param name="resourceLayout">The resource layout to be registered</param>
    /// <typeparam name="T">The type of the object requesting the layout</typeparam>
    /// <param name="previous">If there was previously a resource layout registered under <typeparamref name="T"/>, this is that resource layout. Otherwise, <see langword="null"/></param>
    /// <returns>The same <see cref="ResourceLayout"/> that was just registered: <paramref name="resourceLayout"/></returns>
    public ResourceLayout RegisterResourceLayout<T>(ResourceLayout resourceLayout, out ResourceLayout? previous);

    /// <summary>
    /// Attempts to obtain a <see cref="ResourceLayout"/> under <paramref name="type"/>
    /// </summary>
    /// <param name="type">The type of the object requesting the layout</param>
    /// <param name="layout">The layout, <see langword="null"/> if not found</param>
    /// <returns><see langword="true"/> if the layout is found and <paramref name="layout"/> has it. <see langword="false"/> otherwise</returns>
    public bool TryGetResourceLayout(Type type, [NotNullWhen(true)] out ResourceLayout? layout);

    /// <summary>
    /// Gets the resourceLayout 
    /// </summary>
    /// <param name="type">The type of the object requesting the layout</param>
    /// <exception cref="ArgumentException"></exception>
    public ResourceLayout GetResourceLayout(Type type);

    /// <summary>
    /// Checks if a <see cref="ResourceLayout"/> under <paramref name="type"/> is registered
    /// </summary>
    /// <param name="type">The type of the object requesting the layout</param>
    /// <returns><see langword="true"/> if a <see cref="ResourceLayout"/> was found, <see langword="false"/> otherwise</returns>
    public bool ContainsResourceLayout(Type type);

    /// <summary>
    /// Registers a resource layout into the provided name
    /// </summary>
    /// <param name="resourceLayout">The resource layout to be registered</param>
    /// <param name="type">The type of the object requesting the layout</param>
    /// <param name="previous">If there was previously a resource layout registered under <paramref name="type"/>, this is that resource layout. Otherwise, <see langword="null"/></param>
    /// <returns>The same <see cref="ResourceLayout"/> that was just registered: <paramref name="resourceLayout"/></returns>
    public ResourceLayout RegisterResourceLayout(Type type, ResourceLayout resourceLayout, out ResourceLayout? previous);

    #endregion

    #region Shared Draw Resources

    /// <summary>
    /// Removes a named <see cref="SharedDrawResource"/> 
    /// </summary>
    /// <returns><see langword="true"/> if the resource was already registered and was succesfully removed, <see langword="false"/> otherwise.</returns>
    public bool RemoveResource(string name);

    /// <summary>
    /// Registers an unnamed <see cref="SharedDrawResource"/> 
    /// </summary>
    public void RegisterUnnamedResource(SharedDrawResource resource);

    /// <summary>
    /// Removes <paramref name="resource"/> from this <see cref="IVeldridGraphicsContextResources"/>
    /// </summary>
    /// <returns><see langword="true"/> if the resource was already registered and was succesfully removed, <see langword="false"/> otherwise.</returns>
    public bool RemoveUnnamedResource(SharedDrawResource resource);

    /// <summary>
    /// Registers a new <see cref="SharedDrawResource"/> on this <see cref="VeldridGraphicsContext"/>
    /// </summary>
    /// <param name="resource">The resource that will be registered</param>
    /// <param name="name">The name of the resource</param>
    public void RegisterResource(string name, SharedDrawResource resource);

    /// <summary>
    /// Attempts to get the resource under <paramref name="name"/>
    /// </summary>
    /// <param name="name">The name of the resource</param>
    /// <param name="resource">The resource, if found</param>
    /// <returns><see langword="true"/> if the resource is found and <paramref name="resource"/> has a value. <see langword="false"/> otherwise.</returns>
    public bool TryGetResource(string name, [NotNullWhen(true)] out SharedDrawResource? resource);

    /// <summary>
    /// Attempts to get the resource under <paramref name="name"/>
    /// </summary>
    /// <param name="name">The name of the resource</param>
    /// <param name="resource">The resource, if found</param>
    /// <typeparam name="TSharedDrawResource">The type of the <see cref="SharedDrawResource"/></typeparam>
    /// <returns><see langword="true"/> if the resource is found and <paramref name="resource"/> has a value. <see langword="false"/> otherwise.</returns>
    public bool TryGetResource<TSharedDrawResource>(string name, [NotNullWhen(true)] out TSharedDrawResource? resource)
        where TSharedDrawResource : SharedDrawResource;

    #endregion

    /// <summary>
    /// The Resource cache for this <see cref="VeldridGraphicsContext"/>'s shaders
    /// </summary>
    public GraphicsContextResourceCache<Shader[]> ShaderCache { get; }

    /// <summary>
    /// The Resource cache for this <see cref="VeldridGraphicsContext"/>'s shared textures
    /// </summary>
    public GraphicsContextResourceFactoryCache<Texture> TextureCache { get; }

    /// <summary>
    /// The <see cref="VeldridFrameReport"/> of the last frame
    /// </summary>
    public VeldridFrameReport FrameReport { get; }
}
