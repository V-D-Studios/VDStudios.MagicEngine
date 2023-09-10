using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Runtime.InteropServices;
using VDStudios.MagicEngine.Graphics;
using Veldrid;

namespace VDStudios.MagicEngine.Graphics.Veldrid;

/// <summary>
/// Represents a context for Veldrid
/// </summary>
public class VeldridGraphicsContext : GraphicsContext<VeldridGraphicsContext>
{
    /// <summary>
    /// A report of the last frame in a <see cref="VeldridGraphicsContext"/>'s attached <see cref="VeldridGraphicsManager"/>
    /// </summary>
    /// <param name="DeltaSeconds">The total amount of seconds in the last frame's delta</param>
    /// <param name="Projection">The projection matrix for this frame</param>
    public readonly record struct VeldridFrameReport(Matrix4x4 Projection, float DeltaSeconds);

    /// <summary>
    /// Creates a new object of type <see cref="VeldridGraphicsContext"/> with a given manager
    /// </summary>
    /// <param name="manager">The <see cref="VeldridGraphicsManager"/> that owns this <see cref="VeldridGraphicsContext"/></param>
    /// <param name="device">The <see cref="GraphicsDevice"/> tied to the manager</param>
    /// <exception cref="ArgumentNullException"></exception>
    public VeldridGraphicsContext(GraphicsManager<VeldridGraphicsContext> manager, GraphicsDevice device) : base(manager)
    {
        GraphicsDevice = device;
        commandListPool = new(p => ResourceFactory.CreateCommandList(), _ => { });
        CommandList = ResourceFactory.CreateCommandList();
        
        FrameReportBuffer = ResourceFactory.CreateBuffer(new BufferDescription(
            DataStructuring.FitToUniformBuffer<VeldridFrameReport, uint>(),
            BufferUsage.UniformBuffer
        ));

        FrameReportLayout = ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("FrameReport", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment)
        ));

        FrameReportSet = ResourceFactory.CreateResourceSet(new ResourceSetDescription(
            FrameReportLayout,
            FrameReportBuffer
        ));
    }

    private readonly ObjectPool<CommandList> commandListPool;

    /// <summary>
    /// The <see cref="GraphicsDevice"/> for this context
    /// </summary>
    public GraphicsDevice GraphicsDevice { get; }

    /// <summary>
    /// The <see cref="ResourceFactory"/> of <see cref="GraphicsDevice"/>
    /// </summary>
    public ResourceFactory ResourceFactory => GraphicsDevice.ResourceFactory;

    #region Pipelines

    private readonly Dictionary<Type, Dictionary<uint, Pipeline>> pipelines = new();

    /// <summary>
    /// Gets the pipeline 
    /// </summary>
    /// <param name="index">The index of the pipeline in the <typeparamref name="T"/> pipeline set</param>
    /// <typeparam name="T">The type that the pipeline is for</typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public Pipeline GetPipeline<T>(uint index = 0)
        => GetPipeline(typeof(T), index);

    /// <summary>
    /// Attempts to obtain a <see cref="Pipeline"/> for <typeparamref name="T"/> under <paramref name="index"/>
    /// </summary>
    /// <typeparam name="T">The type that the pipeline is for</typeparam>
    /// <param name="pipeline">The pipeline, <see langword="null"/> if not found</param>
    /// <param name="index">The index of the pipeline in the <typeparamref name="T"/> pipeline set</param>
    /// <returns><see langword="true"/> if the pipeline is found and <paramref name="pipeline"/> has it. <see langword="false"/> otherwise</returns>
    public bool TryGetPipeline<T>([NotNullWhen(true)] out Pipeline? pipeline, uint index = 0)
        => TryGetPipeline(typeof(T), out pipeline, index);

    /// <summary>
    /// Checks if a <see cref="Pipeline"/> under <typeparamref name="T"/> is registered
    /// </summary>
    /// <param name="index">The index of the pipeline in the <typeparamref name="T"/> pipeline set</param>
    /// <typeparam name="T">The type that the pipeline is for</typeparam>
    /// <returns><see langword="true"/> if a <see cref="Pipeline"/> was found, <see langword="false"/> otherwise</returns>
    public bool ContainsPipeline<T>(uint index = 0)
        => ContainsPipeline(typeof(T), index);

    /// <summary>
    /// Registers a pipeline into the provided index
    /// </summary>
    /// <param name="pipeline">The pipeline to be registered</param>
    /// <param name="index">The index of the pipeline in the <typeparamref name="T"/> pipeline set</param>
    /// <typeparam name="T">The type that the pipeline is for</typeparam>
    /// <param name="previous">If there was previously a pipeline registered under <paramref name="index"/>, this is that pipeline. Otherwise, <see langword="null"/></param>
    public void RegisterPipeline<T>(Pipeline pipeline, out Pipeline? previous, uint index = 0)
        => RegisterPipeline(typeof(T), pipeline, out previous, index);

    /// <summary>
    /// Gets the pipeline 
    /// </summary>
    /// <param name="index">The index of the pipeline in the <paramref name="type"/> pipeline set</param>
    /// <param name="type">The type that the pipeline is for</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public Pipeline GetPipeline(Type type, uint index = 0)
    {
        Dictionary<uint, Pipeline>? pd;
        lock (pipelines)
            if (pipelines.TryGetValue(type, out pd) is false)
                throw new ArgumentException($"Could not find a pipeline set for type {type}", nameof(type));

        lock (pd)
            return pd.TryGetValue(index, out var pipe)
                ? pipe
                : throw new ArgumentException($"Found a pipeline set for type {type}, but no pipeline under index {index}", nameof(index));
    }

    /// <summary>
    /// Attempts to obtain a <see cref="Pipeline"/> for <paramref name="type"/> under <paramref name="index"/>
    /// </summary>
    /// <param name="type">The type of the object requesting the pipeline</param>
    /// <param name="pipeline">The pipeline, <see langword="null"/> if not found</param>
    /// <param name="index">The index of the pipeline in the <paramref name="type"/> pipeline set</param>
    /// <returns><see langword="true"/> if the pipeline is found and <paramref name="pipeline"/> has it. <see langword="false"/> otherwise</returns>
    public bool TryGetPipeline(Type type, [NotNullWhen(true)] out Pipeline? pipeline, uint index = 0)
    {
        Dictionary<uint, Pipeline>? pd;
        lock (pipelines)
            if (pipelines.TryGetValue(type, out pd) is false)
            {
                pipeline = null;
                return false;
            }

        lock (pd)
            return pd.TryGetValue(index, out pipeline);
    }

    /// <summary>
    /// Checks if a <see cref="Pipeline"/> under <paramref name="type"/> is registered
    /// </summary>
    /// <param name="index">The index of the pipeline in the <paramref name="type"/> pipeline set</param>
    /// <param name="type">The type that the pipeline is for</param>
    /// <returns><see langword="true"/> if a <see cref="Pipeline"/> was found, <see langword="false"/> otherwise</returns>
    public bool ContainsPipeline(Type type, uint index = 0)
    {
        ArgumentNullException.ThrowIfNull(type);
        return pipelines.TryGetValue(type, out var pd) && pd.ContainsKey(index);
    }

    /// <summary>
    /// Registers a pipeline into the provided index
    /// </summary>
    /// <param name="pipeline">The pipeline to be registered</param>
    /// <param name="index">The index of the pipeline in the <paramref name="type"/> pipeline set</param>
    /// <param name="type">The type that the pipeline is for</param>
    /// <param name="previous">If there was previously a pipeline registered under <paramref name="index"/>, this is that pipeline. Otherwise, <see langword="null"/></param>
    public void RegisterPipeline(Type type, Pipeline pipeline, out Pipeline? previous, uint index = 0)
    {
        Dictionary<uint, Pipeline>? pd;

        lock (pipelines)
            if (pipelines.TryGetValue(type, out pd) is false)
                pipelines.Add(type, pd = new Dictionary<uint, Pipeline>());

        lock (pd)
        {
            pd.Remove(index, out previous);
            pd.Add(index, pipeline);
        }
    }

    /// <summary>
    /// Exchanges the position of two pipelines registered for <paramref name="type"/>
    /// </summary>
    /// <param name="type">The type that the pipeline is for</param>
    /// <param name="indexA">The original index of the pipeline to move to <paramref name="indexB"/></param>
    /// <param name="indexB">The original index of the pipeline to move to <paramref name="indexA"/></param>
    public void ExchangePipelines(Type type, uint indexA, uint indexB)
    {
        Dictionary<uint, Pipeline>? pd;
        if (indexA == indexB) return;

        lock (pipelines)
            if (pipelines.TryGetValue(type, out pd) is false)
                throw new ArgumentException($"There are no pipelines registered for type {type}", nameof(type));

        lock (pd)
        {
            if (pd.TryGetValue(indexA, out var pipeA) is false)
                throw new ArgumentException($"There's no pipeline for type {type} under index {indexA}", nameof(indexA));

            if (pd.TryGetValue(indexB, out var pipeB) is false)
                throw new ArgumentException($"There's no pipeline for type {type} under index {indexB}", nameof(indexB));

            pd[indexA] = pipeB;
            pd[indexB] = pipeA;
        }
    }

    #endregion

    #region Resource Layouts

    private readonly Dictionary<Type, ResourceLayout> resourceLayouts = new();

    /// <summary>
    /// Attempts to obtain a <see cref="ResourceLayout"/> under <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">The type of the object requesting the layout</typeparam>
    /// <param name="layout">The layout, <see langword="null"/> if not found</param>
    /// <returns><see langword="true"/> if the layout is found and <paramref name="layout"/> has it. <see langword="false"/> otherwise</returns>
    public bool TryGetResourceLayout<T>([NotNullWhen(true)] out ResourceLayout? layout)
        => TryGetResourceLayout(typeof(T), out layout);

    /// <summary>
    /// Gets the resourceLayout 
    /// </summary>
    /// <typeparam name="T">The type of the object requesting the layout</typeparam>
    /// <exception cref="ArgumentException"></exception>
    public ResourceLayout GetResourceLayout<T>()
        => GetResourceLayout(typeof(T));

    /// <summary>
    /// Checks if a <see cref="ResourceLayout"/> under <typeparamref name="T"/> is registered
    /// </summary>
    /// <typeparam name="T">The type of the object requesting the layout</typeparam>
    /// <returns><see langword="true"/> if a <see cref="ResourceLayout"/> was found, <see langword="false"/> otherwise</returns>
    public bool ContainsResourceLayout<T>()
        => ContainsResourceLayout(typeof(T));

    /// <summary>
    /// Registers a resource layout into the provided name
    /// </summary>
    /// <param name="resourceLayout">The resource layout to be registered</param>
    /// <typeparam name="T">The type of the object requesting the layout</typeparam>
    /// <param name="previous">If there was previously a resource layout registered under <typeparamref name="T"/>, this is that resource layout. Otherwise, <see langword="null"/></param>
    /// <returns>The same <see cref="ResourceLayout"/> that was just registered: <paramref name="resourceLayout"/></returns>
    public ResourceLayout RegisterResourceLayout<T>(ResourceLayout resourceLayout, out ResourceLayout? previous)
        => RegisterResourceLayout(typeof(T), resourceLayout, out previous);

    /// <summary>
    /// Attempts to obtain a <see cref="ResourceLayout"/> under <paramref name="type"/>
    /// </summary>
    /// <param name="type">The type of the object requesting the layout</param>
    /// <param name="layout">The layout, <see langword="null"/> if not found</param>
    /// <returns><see langword="true"/> if the layout is found and <paramref name="layout"/> has it. <see langword="false"/> otherwise</returns>
    public bool TryGetResourceLayout(Type type, [NotNullWhen(true)] out ResourceLayout? layout)
    {
        ArgumentNullException.ThrowIfNull(type);
        lock (resourceLayouts)
            return resourceLayouts.TryGetValue(type, out layout);
    }

    /// <summary>
    /// Gets the resourceLayout 
    /// </summary>
    /// <param name="type">The type of the object requesting the layout</param>
    /// <exception cref="ArgumentException"></exception>
    public ResourceLayout GetResourceLayout(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        lock (resourceLayouts)
        {
            return resourceLayouts.TryGetValue(type, out var rl)
                ? rl
                : throw new ArgumentException($"Could not find a resourceLayout for {type}", nameof(type));
        }
    }

    /// <summary>
    /// Checks if a <see cref="ResourceLayout"/> under <paramref name="type"/> is registered
    /// </summary>
    /// <param name="type">The type of the object requesting the layout</param>
    /// <returns><see langword="true"/> if a <see cref="ResourceLayout"/> was found, <see langword="false"/> otherwise</returns>
    public bool ContainsResourceLayout(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return resourceLayouts.ContainsKey(type);
    }

    /// <summary>
    /// Registers a resource layout into the provided name
    /// </summary>
    /// <param name="resourceLayout">The resource layout to be registered</param>
    /// <param name="type">The type of the object requesting the layout</param>
    /// <param name="previous">If there was previously a resource layout registered under <paramref name="type"/>, this is that resource layout. Otherwise, <see langword="null"/></param>
    /// <returns>The same <see cref="ResourceLayout"/> that was just registered: <paramref name="resourceLayout"/></returns>
    public ResourceLayout RegisterResourceLayout(Type type, ResourceLayout resourceLayout, out ResourceLayout? previous)
    {
        ArgumentNullException.ThrowIfNull(type);
        lock (resourceLayouts)
        {
            resourceLayouts.Remove(type, out previous);
            resourceLayouts.Add(type, resourceLayout);
            return resourceLayout;
        }
    }

    #endregion

    #region Shared Draw Resources

#warning Consider adding a special case ServiceProvider for SharedDrawResources, or, rather, model it after ServiceProvider

    private readonly Dictionary<string, SharedDrawResource> sharedDrawResources = new();

    /// <summary>
    /// Registers a new <see cref="SharedDrawResource"/> on this <see cref="VeldridGraphicsContext"/>
    /// </summary>
    /// <param name="resource">The resource that will be registered</param>
    /// <param name="name">The name of the resource</param>
    public void RegisterResource(string name, SharedDrawResource resource)
    {
        lock (sharedDrawResources)
        {
            if (sharedDrawResources.ContainsKey(name))
                throw new InvalidOperationException($"Cannot register new resource under '{name}', as another resource already has that name");
            sharedDrawResources.Add(name, resource);
        }
    }

    /// <summary>
    /// Attempts to get the resource under <paramref name="name"/>
    /// </summary>
    /// <param name="name">The name of the resource</param>
    /// <param name="resource">The resource, if found</param>
    /// <returns><see langword="true"/> if the resource is found and <paramref name="resource"/> has a value. <see langword="false"/> otherwise.</returns>
    public bool TryGetResource(string name, [NotNullWhen(true)] out SharedDrawResource? resource)
    {
        lock (sharedDrawResources)
            return sharedDrawResources.TryGetValue(name, out resource);
    }

    /// <summary>
    /// Attempts to get the resource under <paramref name="name"/>
    /// </summary>
    /// <param name="name">The name of the resource</param>
    /// <param name="resource">The resource, if found</param>
    /// <typeparam name="TSharedDrawResource">The type of the <see cref="SharedDrawResource"/></typeparam>
    /// <returns><see langword="true"/> if the resource is found and <paramref name="resource"/> has a value. <see langword="false"/> otherwise.</returns>
    public bool TryGetResource<TSharedDrawResource>(string name, [NotNullWhen(true)] out TSharedDrawResource? resource)
        where TSharedDrawResource : SharedDrawResource
    {
        lock (sharedDrawResources)
        {
            if (sharedDrawResources.TryGetValue(name, out var rsc))
            {
                resource = rsc as TSharedDrawResource ?? throw new InvalidCastException($"The SharedDrawResource under name '{name}' could not be cast to type {typeof(TSharedDrawResource)}");
                return true;
            }

            resource = null;
            return false;
        }
    }

    #endregion

    /// <summary>
    /// An uniform buffer containing data about the last frame
    /// </summary>
    public DeviceBuffer FrameReportBuffer { get; }

    /// <summary>
    /// The <see cref="ResourceLayout"/> for <see cref="FrameReportBuffer"/>
    /// </summary>
    public ResourceLayout FrameReportLayout { get; }

    /// <summary>
    /// The <see cref="ResourceLayout"/> containing <see cref="FrameReportBuffer"/>
    /// </summary>
    public ResourceSet FrameReportSet { get; }

    private readonly List<CommandList> commands = new();

    /// <summary>
    /// The primary <see cref="CommandList"/> for this <see cref="VeldridGraphicsContext"/>
    /// </summary>
    /// <remarks>
    /// This <see cref="CommandList"/> cannot be used outside of a <see cref="DrawOperation{TGraphicsContext}.Draw(TimeSpan, TGraphicsContext, RenderTarget{TGraphicsContext})"/>, <see cref="DrawOperation{TGraphicsContext}.UpdateGPUState(TGraphicsContext)"/>, <see cref="DrawOperation{TGraphicsContext}.CreateGPUResources(TGraphicsContext)"/> 
    /// </remarks>
    public CommandList CommandList { get; }

    /// <summary>
    /// The <see cref="VeldridFrameReport"/> of the last frame
    /// </summary>
    public VeldridFrameReport FrameReport { get; private set; }

    internal void AssignCommandList(VeldridRenderTarget target)
    {
        var cl = commandListPool.Rent().Item;
        cl.Begin();
        commands.Add(cl);
        target.cl = cl;
    }

    internal void RemoveCommandList(VeldridRenderTarget target)
    {
        Debug.Assert(target.cl is not null, "target.cl is unexpectedly null");
        target.cl.End();
        target.cl = null;
    }

    /// <inheritdoc/>
    public override void Update(TimeSpan delta)
    {
        FrameReport = new(Manager.WindowView, (float)delta.TotalSeconds);

        CommandList.Begin();

        CommandList.UpdateBuffer(FrameReportBuffer, 0, FrameReport);

        lock (sharedDrawResources)
            foreach (var (_, sdr) in sharedDrawResources)
                if (sdr.PendingGpuUpdate)
                    sdr.UpdateGPUState(this, CommandList);
    }

    /// <inheritdoc/>
    public override void BeginFrame()
    {
        CommandList.SetFramebuffer(GraphicsDevice.SwapchainFramebuffer);
        CommandList.ClearColorTarget(0, Manager.BackgroundColor.ToRgbaFloat());
    }

    /// <inheritdoc/>
    public override void EndAndSubmitFrame()
    {
        CommandList.End();
        GraphicsDevice.SubmitCommands(CommandList);

        var cmds = CollectionsMarshal.AsSpan(commands);
        for (int i = 0; i < cmds.Length; i++)
        {
            var cmd = cmds[i];
            GraphicsDevice.SubmitCommands(cmd);
            commandListPool.Return(cmd);
        }

        commands.Clear();
    }
}
