using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Runtime.InteropServices;
using VDStudios.MagicEngine.Exceptions;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Graphics.Veldrid.Caching;
using Veldrid;
using Vulkan;

namespace VDStudios.MagicEngine.Graphics.Veldrid;

/// <summary>
/// A report of the last frame in a <see cref="VeldridGraphicsContext"/>'s attached <see cref="VeldridGraphicsManager"/>
/// </summary>
/// <param name="DeltaSeconds">The total amount of seconds in the last frame's delta</param>
/// <param name="Projection">The projection matrix for this frame</param>
public readonly record struct VeldridFrameReport(Matrix4x4 Projection, float DeltaSeconds);

/// <summary>
/// Represents a context for Veldrid
/// </summary>
public class VeldridGraphicsContext : GraphicsContext<VeldridGraphicsContext>, IVeldridGraphicsContextResources
{
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

        ShaderCache = new(this);
        TextureCache = new(this);
        SamplerCache = new(this);
}

    private readonly ObjectPool<CommandList> commandListPool;

    /// <inheritdoc/>
    public GraphicsDevice GraphicsDevice { get; }

    /// <inheritdoc/>
    public ResourceFactory ResourceFactory => GraphicsDevice.ResourceFactory;

    #region Pipelines

    private readonly Dictionary<Type, Dictionary<uint, Pipeline>> pipelines = new();

    /// <inheritdoc/>
    public Pipeline GetPipeline<T>(uint index = 0)
        => GetPipeline(typeof(T), index);

    /// <inheritdoc/>
    public bool TryGetPipeline<T>([NotNullWhen(true)] out Pipeline? pipeline, uint index = 0)
        => TryGetPipeline(typeof(T), out pipeline, index);

    /// <inheritdoc/>
    public bool ContainsPipeline<T>(uint index = 0)
        => ContainsPipeline(typeof(T), index);

    /// <inheritdoc/>
    public void RegisterPipeline<T>(Pipeline pipeline, out Pipeline? previous, uint index = 0)
        => RegisterPipeline(typeof(T), pipeline, out previous, index);

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public Pipeline GetOrAddPipeline<T>(GraphicsResourceFactory<Pipeline> pipelineFactory, uint index = 0)
        => GetOrAddPipeline(typeof(T), pipelineFactory, index);

    /// <inheritdoc/>
    public Pipeline GetOrAddPipeline(Type type, GraphicsResourceFactory<Pipeline> pipelineFactory, uint index = 0)
    {
        Dictionary<uint, Pipeline>? pd;
        lock (pipelines)
            if (pipelines.TryGetValue(type, out pd) is false)
                pipelines.Add(type, pd = new());

        Pipeline? pipe;
        lock (pd)
            if (pd.TryGetValue(index, out pipe) is false)
            {
                ArgumentNullException.ThrowIfNull(pipelineFactory); 
                pd.Add(index, pipe = pipelineFactory(this));
            }

        return pipe;
    }

    /// <inheritdoc/>
    public bool ContainsPipeline(Type type, uint index = 0)
    {
        ArgumentNullException.ThrowIfNull(type);
        return pipelines.TryGetValue(type, out var pd) && pd.ContainsKey(index);
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public bool TryGetResourceLayout<T>([NotNullWhen(true)] out ResourceLayout? layout)
        => TryGetResourceLayout(typeof(T), out layout);

    /// <inheritdoc/>
    public ResourceLayout GetResourceLayout<T>()
        => GetResourceLayout(typeof(T));

    /// <inheritdoc/>
    public bool ContainsResourceLayout<T>()
        => ContainsResourceLayout(typeof(T));

    /// <inheritdoc/>
    public ResourceLayout RegisterResourceLayout<T>(ResourceLayout resourceLayout, out ResourceLayout? previous)
        => RegisterResourceLayout(typeof(T), resourceLayout, out previous);

    /// <inheritdoc/>
    public bool TryGetResourceLayout(Type type, [NotNullWhen(true)] out ResourceLayout? layout)
    {
        ArgumentNullException.ThrowIfNull(type);
        lock (resourceLayouts)
            return resourceLayouts.TryGetValue(type, out layout);
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public bool ContainsResourceLayout(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return resourceLayouts.ContainsKey(type);
    }

    /// <inheritdoc/>
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

    private readonly HashSet<SharedDrawResource> unnamedDrawResources = new();
    private readonly Dictionary<string, SharedDrawResource> sharedDrawResources = new();

    private void ValidateRegistration(SharedDrawResource resource)
    {
        if ((resource.hash is not null && resource.hash != unnamedDrawResources) || resource.dict is not null && resource.dict != sharedDrawResources)
            throw new ArgumentException("The resource is already registered to another VeldridGraphicsContext", nameof(resource));
    }

    /// <inheritdoc/>
    public void RegisterUnnamedResource(SharedDrawResource resource)
    {
        GameMismatchException.ThrowIfMismatch(resource, Manager);

        ValidateRegistration(resource);
        if (resource.hash is not null || resource.dict is not null) return;

        lock (unnamedDrawResources)
        {
            resource.hash = unnamedDrawResources;
            unnamedDrawResources.Add(resource);
        }
    }

    /// <inheritdoc/>
    public bool RemoveUnnamedResource(SharedDrawResource resource)
    {
        GameMismatchException.ThrowIfMismatch(resource, Manager);

        ValidateRegistration(resource);
        return resource.RemoveSelf();
    }

    /// <inheritdoc/>
    public bool RemoveResource(string name)
    {
        lock (sharedDrawResources)
            return sharedDrawResources.TryGetValue(name, out var res) && res.RemoveSelf();
    }

    /// <inheritdoc/>
    public void RegisterResource(string name, SharedDrawResource resource)
    {
        GameMismatchException.ThrowIfMismatch(resource, Manager);

        ValidateRegistration(resource);

        lock (sharedDrawResources)
        {
            if (sharedDrawResources.ContainsKey(name))
                throw new InvalidOperationException($"Cannot register new resource under '{name}', as another resource already has that name");

            if (resource.hash is not null || resource.dict is not null) return;
            resource.dict = sharedDrawResources;
            resource.name = name;
            sharedDrawResources.Add(name, resource);
        }
    }

    /// <inheritdoc/>
    public bool TryGetResource(string name, [NotNullWhen(true)] out SharedDrawResource? resource)
    {
        lock (sharedDrawResources)
            return sharedDrawResources.TryGetValue(name, out resource);
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public GraphicsContextResourceCache<Shader[]> ShaderCache { get; }

    /// <inheritdoc/>
    public GraphicsContextResourceFactoryCache<Sampler> SamplerCache { get; }

    /// <inheritdoc/>
    public GraphicsContextOwnedResourceFactoryCache<Texture, TextureView> TextureCache { get; }

    /// <summary>
    /// An uniform buffer containing data about the last frame
    /// </summary>
    public DeviceBuffer FrameReportBuffer { get; }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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
        CommandList.SetFullViewports();
        CommandList.SetFullScissorRects();
        CommandList.ClearColorTarget(0, Manager.BackgroundColor.ToRgbaFloat());
        //CommandList.ClearDepthStencil(GraphicsDevice.IsDepthRangeZeroToOne ? 1f : 0f);
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

        GraphicsDevice.WaitForIdle();
        GraphicsDevice.SwapBuffers();

        commands.Clear();
    }
}
