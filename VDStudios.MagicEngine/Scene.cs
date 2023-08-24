using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Internal;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a general state of the game
/// </summary>
/// <remarks>
/// A scene can be the MainMenu, a Room, a Dungeon, and any number of things. It is a self-contained state of the <see cref="Game"/>. It's allowed to share states across <see cref="Scene"/>s, but doing so and managing it is your responsibility
/// </remarks>
public abstract class Scene : GameObject, IDisposable
{
    #region Construction

    /// <summary>
    /// Instances and Initializes the current <see cref="Scene"/>
    /// </summary>
    public Scene(Game game) : base(game, "Game Scene", "Update")
    {
        Log?.Verbose("Constructing new Scene");
        Children = NodeList.Empty.Clone();
        Game.SetupScenes += OnGameSetupScenes;
        Game.StopScenes += OnGameStopScenes;
        lock (Game.scenesAwaitingSetup)
            Game.scenesAwaitingSetup.Enqueue(this, (int)QueryConfigurationAsynchronousTendency());
        Services = new(Game.GameServices);
    }

    /// <summary>
    /// Queries this scene for <see cref="ConfigureScene"/>'s tendency to be asynchronous.
    /// </summary>
    /// <remarks>
    /// This method is called exactly once INSIDE THE CONSTRUCTOR OF <see cref="Scene"/>, which is called BEFORE your derived type's constructor. Handle with care; best used by returning a single constant value
    /// </remarks>
    protected virtual AsynchronousTendency QueryConfigurationAsynchronousTendency() => AsynchronousTendency.SometimesAsynchronous;

    #endregion

    #region Scene Setup

    #region Setup

    /// <summary>
    /// This method is called automatically at the beginning of a frame in the <see cref="Game"/> and is called exactly once per instance. This method is guaranteed to only be called by the framework once, and is guaranteed to run before the <see cref="Scene"/> is used for the first time
    /// </summary>
    /// <remarks>
    /// Consider this method an asynchronous constructor for the <see cref="Scene"/>. You may attach nodes and request async services here. Exclusively synchronous work can and should be done in the type's constructor
    /// </remarks>
    protected virtual ValueTask ConfigureScene() => ValueTask.CompletedTask;

    internal ValueTask InternalConfigure()
    {
        InternalLog?.Information("Configuring");
        return ConfigureScene();
    }

    #endregion

    #region Reaction Methods

    /// <summary>
    /// This method is called automatically when the <see cref="Game"/> is loaded and this (and all other) <see cref="Scene"/>s should be set up
    /// </summary>
    /// <remarks>
    /// The <see cref="Game"/> automatically takes care of both <see cref="Scene"/>s that were instanced before the <see cref="Game"/> was loaded and <see cref="Scene"/>s that were instanced afterwards
    /// </remarks>
    protected virtual void GameLoaded() { }

    /// <summary>
    /// This method is called automatically when the <see cref="Game"/> is about to be unloaded (before <see cref="Game.GameUnloading"/> is fired) and this (and all other) <see cref="Scene"/>s should stop as well
    /// </summary>
    protected virtual void GameUnloading() { }

    #endregion

    #region Internal

    private void OnGameSetupScenes()
    {
        GameLoaded();
    }

    private void OnGameStopScenes()
    {
        GameUnloading();
    }

    #endregion

    #endregion

    #region Dependency Injection

    /// <summary>
    /// The <see cref="ServiceCollection"/> for this <see cref="Scene"/>
    /// </summary>
    /// <remarks>
    /// Services to this <see cref="Scene"/> will cascade down from <see cref="Game.GameServices"/>, if not overriden.
    /// </remarks>
    public ServiceCollection Services { get; }

    #endregion

    #region Child Nodes

    /// <summary>
    /// Represents the Children or Subnodes of this <see cref="Scene"/>
    /// </summary>
    /// <remarks>
    /// Due to thread safety concerns, the list is locked during reads
    /// </remarks>
    public NodeList Children { get; internal set; }

    private readonly ConcurrentDictionary<Type, object> drawopmanager_dict = new();

    /// <summary>
    /// Attempts to find a <see cref="DrawOperationManager{TGraphicsContext}"/> for <typeparamref name="TGraphicsContext"/> in this <see cref="Scene"/>.
    /// </summary>
    /// <remarks>
    /// If the <see cref="DrawOperationManager{TGraphicsContext}"/> is not found, consider calling <see cref="RegisterDrawOperationManager{TGraphicsContext}(DrawOperationManager{TGraphicsContext})"/>
    /// </remarks>
    /// <returns><see langword="true"/> if a <see cref="DrawOperationManager{TGraphicsContext}"/> for context <typeparamref name="TGraphicsContext"/> is found, <see langword="false"/> otherwise</returns>
    public bool GetDrawOperationManager<TGraphicsContext>([MaybeNullWhen(false)] [NotNullWhen(true)] out DrawOperationManager<TGraphicsContext>? drawOperationManager)
        where TGraphicsContext : GraphicsContext<TGraphicsContext>
    {
        if (drawopmanager_dict.TryGetValue(typeof(TGraphicsContext), out var value))
        {
            Debug.Assert(value is DrawOperationManager<TGraphicsContext>);
            drawOperationManager = (DrawOperationManager<TGraphicsContext>)value;
            return true;
        }

        drawOperationManager = default;
        return false;
    }

    /// <summary>
    /// Attempts to register <paramref name="drawOperationManager"/> under context <typeparamref name="TGraphicsContext"/> for this <see cref="Scene"/>
    /// </summary>
    /// <remarks>
    /// Can later be retrieved via <see cref="GetDrawOperationManager{TGraphicsContext}(out DrawOperationManager{TGraphicsContext}?)"/>
    /// </remarks>
    /// <returns><see langword="true"/> if a <see cref="DrawOperationManager{TGraphicsContext}"/> for context <typeparamref name="TGraphicsContext"/> was registered, <see langword="false"/> if one was already present</returns>
    public bool RegisterDrawOperationManager<TGraphicsContext>(DrawOperationManager<TGraphicsContext> drawOperationManager)
        where TGraphicsContext : GraphicsContext<TGraphicsContext>
        => drawopmanager_dict.TryAdd(typeof(TGraphicsContext), drawOperationManager ?? throw new ArgumentNullException(nameof(drawOperationManager)));

    /// <summary>
    /// Attempts to remove a <see cref="DrawOperationManager{TGraphicsContext}"/> registered for <typeparamref name="TGraphicsContext"/>
    /// </summary>
    /// <returns><see langword="true"/> if a <see cref="DrawOperationManager{TGraphicsContext}"/> for context <typeparamref name="TGraphicsContext"/> was removed, <see langword="false"/> if one was not found</returns>
    public bool RemoveDrawOperationManager<TGraphicsContext>()
        where TGraphicsContext : GraphicsContext<TGraphicsContext>
        => drawopmanager_dict.TryRemove(typeof(TGraphicsContext), out _);

    internal int RegisterNodeInScene(Node child)
    {
        var id = Children.Add(child);
        return id;
    }

    #region Attachment

    /// <summary>
    /// Attaches <paramref name="child"/> into this <see cref="Scene"/>
    /// </summary>
    /// <param name="child">The child <see cref="Node"/> to attach into this <see cref="Scene"/></param>
    public ValueTask Attach(Node child)
        => child.AttachTo(this);

    #endregion

    #region Filters

    /// <summary>
    /// This method is automatically called when a Child node is about to be attached, and should be used to filter what Nodes are allowed to be children of this <see cref="Scene"/>
    /// </summary>
    /// <param name="child">The node about to be attached</param>
    /// <param name="reasonForDenial">The optional reason for the denial of <paramref name="child"/></param>
    /// <returns><c>true</c> if the child node is allowed to be attached into this <see cref="Scene"/>. <c>false</c> otherwise, along with an optional reason string in <paramref name="reasonForDenial"/></returns>
    protected internal virtual bool FilterChildNode(Node child, [NotNullWhen(false)] out string? reasonForDenial)
    {
        reasonForDenial = null;
        return true;
    }

    #endregion

    #region Sorters

    /// <summary>
    /// This method is automatically called when a Child node is being attached. It assigns a custom updater to the child node, or <c>null</c> to use <see cref="HandleChildUpdate(Node)"/> instead
    /// </summary>
    /// <param name="node">The <see cref="Node"/> that is being attached, and should be assigned an Updater</param>
    /// <returns>The <see cref="NodeUpdater"/> specific to <paramref name="node"/>, or <c>null</c> to use <see cref="HandleChildUpdate(Node)"/> instead</returns>
    protected internal virtual ValueTask<NodeUpdater?> AssignUpdater(Node node) => ValueTask.FromResult<NodeUpdater?>(null);

    #endregion

    #region Default Handlers

    /// <summary>
    /// This method is automatically called when a Child node is about to be updated, and it has no custom handler set
    /// </summary>
    /// <param name="node">The node about to be updated</param>
    protected virtual ValueTask HandleChildUpdate(Node node) => ValueTask.CompletedTask;

    #endregion

    #region Update Batching

    #region Fields

    internal UpdateBatchCollection UpdateBatches = new();

    #endregion

    #region Sorters

    /// <summary>
    /// This method is called automatically when a child node is attached and being registered in an update batch. It can be used to override <see cref="Node.UpdateBatch"/>
    /// </summary>
    /// <remarks>
    /// Use this with care, as no attempts are made by the framework to notify <paramref name="node"/> if its preferred <see cref="UpdateBatch"/> is overriden. You may break something.
    /// </remarks>
    /// <param name="node"></param>
    /// <returns>The <see cref="UpdateBatch"/> <paramref name="node"/> is going to be registered into</returns>
    protected virtual UpdateBatch AssigningToUpdateBatch(Node node) => node.UpdateBatch;

    #endregion

    #region Internal

    internal void AssignToUpdateBatch(Node node)
    {
        var ub = AssigningToUpdateBatch(node);
        node.UpdateAssignation = ub;
        UpdateBatches.Add(node, ub, node.AsynchronousUpdateTendency);
    }

    internal void ExtractFromUpdateBatch(Node node)
    {
        UpdateBatches.Remove(node, node.UpdateAssignation, node.AsynchronousUpdateTendency);
        node.UpdateAssignation = (UpdateBatch)(-1);
    }

    internal async ValueTask InternalHandleChildUpdate(Node node, TimeSpan delta)
    {
        var sd = node.SkipDat;
        // This could potentially cause a state where the node never-endingly skips
        // Protections against such cases are built into the Node.Skip methods; any new instance methods that don't use methods that already have those protections, should include such protections.
        if (sd.MarkedForSkip && (sd.Time is TimeSpan t && t > Game.TotalTime || sd.Frames > 0 && Game.FrameCount % sd.Frames != 0))
            return;
        node.SkipDat = default;
        await node.InternalUpdate(delta);
        await (node.updater is NodeUpdater updater ? updater.PerformUpdate() : HandleChildUpdate(node));
    }

    internal async ValueTask InternalPropagateChildUpdate(TimeSpan delta)
    {
        var pool = ArrayPool<ValueTask>.Shared;
        int toUpdate = Children.Count;
        ValueTask[] tasks = pool.Rent(toUpdate);
        try
        {
            int ind = 0;
            lock (Sync)
            {
                for (int bi = 0; bi < UpdateBatchCollection.BatchCount; bi++)
                    for (int ti = UpdateSynchronicityBatch.BatchCount - 1; ti >= 0; ti--)
                    {
                        var batch = UpdateBatches[(UpdateBatch)bi, (AsynchronousTendency)ti];
                        if (batch is not null and { Count: > 0 })
                            foreach (var child in batch)
                                if (child.IsActive)
                                    tasks[ind++] = InternalHandleChildUpdate(child, delta).Preserve();
                    }
            }
            for (int i = 0; i < ind; i++)
                await tasks[i];
        }
        finally
        {
            pool.Return(tasks, true);
        }
    }

    #endregion

    #endregion

    #endregion

    #region Scene Processing

    #region Internal

    internal bool IsBegun;
    internal async ValueTask Begin()
    {
        if (IsBegun)
            throw new InvalidOperationException("Cannot begin a Scene that has already begun");
        InternalLog?.Information("Beginning Scene");
        await Beginning();
        IsBegun = true;
        SceneBegan?.Invoke(this, Game.TotalTime);
    }

    internal async ValueTask End(Scene next)
    {
        InternalLog?.Information("Ending scene, to make way for {name}-{type}", next.Name, next.GetTypeName());
        await Ending(next);
        SceneEnded?.Invoke(this, Game.TotalTime);
        await next.Transitioning(this);
    }

    internal async ValueTask End()
    {
        InternalLog?.Information("Ending scene");
        await Ending();
        SceneEnded?.Invoke(this, Game.TotalTime);
    }

    #endregion

    #region Events

    /// <summary>
    /// Fired when this <see cref="Scene"/> has begun
    /// </summary>
    public SceneEvent? SceneBegan;

    /// <summary>
    /// Fired when this <see cref="Scene"/> has ended
    /// </summary>
    public SceneEvent? SceneEnded;

    #endregion

    #region Reaction Methods

    /// <summary>
    /// This method is called automatically when the <see cref="Scene"/>
    /// </summary>
    protected virtual ValueTask Beginning() => ValueTask.CompletedTask;

    /// <summary>
    /// This method is called automatically when the previous <see cref="Scene"/> is ending and this <see cref="Scene"/> is being readied
    /// </summary>
    /// <remarks>
    /// This method is called after <paramref name="previous"/>'s <see cref="Ending(Scene)"/>
    /// </remarks>
    protected virtual ValueTask Transitioning(Scene previous) => ValueTask.CompletedTask;

    /// <summary>
    /// This method is called automatically when the <see cref="Scene"/> is ending, and it's not being replaced by another <see cref="Scene"/>
    /// </summary>
    protected virtual ValueTask Ending() => ValueTask.CompletedTask;

    /// <summary>
    /// This method is called automatically when the <see cref="Scene"/> is ending and the next one is being prepared
    /// </summary>
    /// <remarks>
    /// This method is called before <paramref name="next"/>'s <see cref="Transitioning(Scene)"/>
    /// </remarks>
    /// <param name="next">The <see cref="Scene"/> that will begin next</param>
    protected virtual ValueTask Ending(Scene next) => ValueTask.CompletedTask;

    #endregion

    #endregion

    #region Updating and Drawing

    #region Propagation

    #region Update

    internal async ValueTask Update(TimeSpan delta)
    {
        if (!await Updating(delta))
            return;

        await InternalPropagateChildUpdate(delta);
    }

    #endregion

    #endregion

    #region Reaction Methods

    #region Update

    /// <summary>
    /// This method is called automatically when the <see cref="Scene"/> is to be updated
    /// </summary>
    /// <param name="delta">The amount of time that has passed since the last update batch call</param>
    /// <returns>Whether the update sequence should be propagated into this <see cref="Node"/>'s children. If this is false, Update handlers for children will also be skipped</returns>
    protected virtual ValueTask<bool> Updating(TimeSpan delta) => ValueTask.FromResult(true);

    #endregion

    #endregion

    #endregion

    #region Disposal

    internal override void InternalDispose(bool disposing)
    {
        Game.SetupScenes -= OnGameSetupScenes;
        Game.StopScenes -= OnGameStopScenes;

        foreach (var child in Children)
            child.Dispose();
        Children = null!;

        base.InternalDispose(disposing);
    }

    #endregion
}
