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
    /// A set of Shaders
    /// </summary>
    /// <param name="VertexShader">The Vertex Shader</param>
    /// <param name="FragmentShader">The Fragment Shader</param>
    /// <param name="OtherShaders">Other shaders in this set. This array should not contain neither <paramref name="VertexShader"/> nor <paramref name="FragmentShader"/></param>
    public readonly record struct ShaderSet(Shader VertexShader, Shader FragmentShader, Shader[]? OtherShaders);

    private readonly record struct ShaderSetKey(string Name, ResourceLayout ResourceLayout);

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

    private readonly Dictionary<uint, Pipeline> pipelines = new();

    /// <summary>
    /// The index of the default pipeline
    /// </summary>
    public uint DefaultPipelineIndex
    {
        get => dpipei;
        set
        {
            if (pipelines.ContainsKey(value) is false)
                throw new ArgumentException($"Could not find a pipeline by index {value}", nameof(value));
            dpipei = value;
        }
    }
    private uint dpipei;

    /// <summary>
    /// Gets the pipeline 
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public Pipeline GetPipeline(uint? index = null)
    {
        lock (pipelines)
        {
            uint ind = index ?? DefaultPipelineIndex;
            if (pipelines.TryGetValue(ind, out var pipe))
                return pipe;
            else
            {
                Debug.Assert(ind != DefaultPipelineIndex, "DefaultPipelineIndex did not turn up a valid Pipeline");
                throw new ArgumentException($"Could not find a pipeline by index {ind}", nameof(index));
            }
        }
    }

    /// <summary>
    /// Registers a pipeline into the provided index
    /// </summary>
    /// <param name="pipeline">The pipeline to be registered</param>
    /// <param name="index">The index of the pipeline</param>
    /// <param name="previous">If there was previously a pipeline registered under <paramref name="index"/>, this is that pipeline. Otherwise, <see langword="null"/></param>
    public void RegisterPipeline(Pipeline pipeline, uint index, out Pipeline? previous)
    {
        lock (pipelines)
        {
            pipelines.Remove(index, out previous);
            pipelines.Add(index, pipeline);
        }
    }

    #endregion

    #region Resource Layouts

    private readonly Dictionary<string, ResourceLayout> resourceLayouts = new();

    /// <summary>
    /// Gets the resourceLayout 
    /// </summary>
    /// <param name="name">The name of the resourceLayout</param>
    /// <exception cref="ArgumentException"></exception>
    public ResourceLayout GetResourceLayout(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        lock (resourceLayouts)
        {
            return resourceLayouts.TryGetValue(name, out var rl)
                ? rl
                : throw new ArgumentException($"Could not find a resourceLayout by name {name}", nameof(name));
        }
    }

    /// <summary>
    /// Registers a resource layout into the provided name
    /// </summary>
    /// <param name="resourceLayout">The resource layout to be registered</param>
    /// <param name="name">The name of the resource layout</param>
    /// <param name="previous">If there was previously a resource layout registered under <paramref name="name"/>, this is that resource layout. Otherwise, <see langword="null"/></param>
    public void RegisterResourceLayout(ResourceLayout resourceLayout, string name, out ResourceLayout? previous)
    {
        ArgumentNullException.ThrowIfNull(name);
        lock (resourceLayouts)
        {
            resourceLayouts.Remove(name, out previous);
            resourceLayouts.Add(name, resourceLayout);
        }
    }

    #endregion

    #region Shaders

    private readonly Dictionary<ShaderSetKey, ShaderSet> shaders = new();

    /// <summary>
    /// Gets the shader set 
    /// </summary>
    /// <param name="name">The name of the shader</param>
    /// <param name="layout">The <see cref="ResourceLayout"/> the set is registered under</param>
    /// <exception cref="ArgumentException"></exception>
    public ShaderSet GetShaderSet(string name, ResourceLayout layout)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(layout);
        lock (shaders)
        {
            return shaders.TryGetValue(new(name, layout), out var rl)
                ? rl
                : throw new ArgumentException($"Could not find a shader by name {name} registered under the specified layout", nameof(name));
        }
    }

    /// <summary>
    /// Registers a resource layout into the provided name
    /// </summary>
    /// <param name="shader">The resource layout to be registered</param>
    /// <param name="name">The name of the resource layout</param>
    /// <param name="layout">The <see cref="ResourceLayout"/> the set is registered under</param>
    /// <param name="previous">If there was previously a resource layout registered under <paramref name="name"/>, this is that resource layout. Otherwise, <see langword="null"/></param>
    public void RegisterShaderSet(ShaderSet shader, string name, ResourceLayout layout, out ShaderSet previous)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(layout);
        var k = new ShaderSetKey(name, layout);
        lock (shaders)
        {
            shaders.Remove(k, out previous);
            shaders.Add(k, shader);
        }
    }

    #endregion

    #region Shared Draw Resources

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
