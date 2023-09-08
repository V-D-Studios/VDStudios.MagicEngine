using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Graphics.Veldrid;
using Veldrid;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a context for Veldrid
/// </summary>
public class VeldridGraphicsContext : GraphicsContext<VeldridGraphicsContext>
{
    /// <summary>
    /// A report of the last frame in a <see cref="VeldridGraphicsContext"/>'s attached <see cref="VeldridGraphicsManager"/>
    /// </summary>
    /// <param name="DeltaSeconds">The total amount of seconds in the last frame's delta</param>
    public readonly record struct VeldridFrameReport(float DeltaSeconds);

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

    private readonly Dictionary<uint, Pipeline> pipelines = new();

    /// <summary>
    /// An uniform buffer containing ;
    /// </summary>
    public DeviceBuffer FrameReportBuffer { get; }

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
        FrameReport = new((float)delta.TotalSeconds);

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
        CommandList.ClearColorTarget(0, Manager.BackgroundColor.ToRgbaFloat())
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
