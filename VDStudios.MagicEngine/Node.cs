using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using VDStudios.MagicEngine.Exceptions;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Internal;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents an active object in the <see cref="Game"/>
/// </summary>
/// <remarks>
/// A <see cref="Node"/> can be an entity, a bullet, code to update another <see cref="Node"/>'s values, or any kind of active game object that is not a <see cref="FunctionalComponent"/>. To make a <see cref="Node"/> drawable, have it implement one of <see cref="IDrawableNode"/>. A <see cref="Node"/> is responsible for updating its own <see cref="FunctionalComponent"/>s
/// </remarks>
public abstract class Node : GameObject, IDisposable
{
    #region Constructors

    /// <summary>
    /// Initializes basic properties and fields to give a <see cref="Node"/> its default functionality
    /// </summary>
    protected Node() : base("Game Node Tree", "Update")
    {
        ReadySemaphore = new(0, 1);
    }

    #endregion

    #region Updating

    #region Update Batching

    #region Properties

    /// <summary>
    /// Represents the tendency this <see cref="Node"/> has to update asynchronously. This is used as a hint to MagicEngine for optimization purposes
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="AsynchronousTendency.SometimesAsynchronous"/>
    /// </remarks>
    public AsynchronousTendency AsynchronousUpdateTendency { get; protected init; } = AsynchronousTendency.SometimesAsynchronous;

    #endregion

    #region Fields

    /// <summary>
    /// This field belongs to this <see cref="Node"/>'s parent
    /// </summary>
    internal UpdateBatch UpdateAssignation = (UpdateBatch)(-1);

    #endregion

    #endregion

    #region Public Properties

    /// <summary>
    /// <c>true</c> if this <see cref="Node"/> is active and should be updated and, if it implements <see cref="IDrawableNode"/>, added to the draw queue. <c>false</c> otherwise
    /// </summary>
    /// <remarks>
    /// If this property is <c>false</c>, this <see cref="Node"/> and its children will be skipped, along with any handlers it may have. Defaults to <c>true</c> and must be set to <c>false</c> manually if desired
    /// </remarks>
    public bool IsActive
    {
        get => isActive;
        protected set
        {
            if (isActive == value)
                return;
            isActive = value;
            ActiveChanged?.Invoke(this, Game.TotalTime, value);
        }
    }
    private bool isActive = true;

    /// <summary>
    /// <c>true</c> when the node has been added to the scene tree and initialized
    /// </summary>
    public bool IsReady 
    {
        get => _isReady; 
        private set
        {
            if (value == _isReady) return;
            if (value)
                ReadySemaphore.Release();
            else
                ReadySemaphore.Wait();
            _isReady = value;
        }
    }
    private bool _isReady;
    private readonly SemaphoreSlim ReadySemaphore;

    /// <summary>
    /// Asynchronously waits until the Node has been added to the scene tree and is ready to be used
    /// </summary>
    public async ValueTask WaitUntilReadyAsync()
    {
        if (IsReady)
            return;
        if (ReadySemaphore.Wait(15))
        {
            ReadySemaphore.Release();
            return;
        }

        await ReadySemaphore.WaitAsync();
        ReadySemaphore.Release();
    }

    /// <summary>
    /// Asynchronously waits until the Node has been added to the scene tree and is ready to be used
    /// </summary>
    public async ValueTask<bool> WaitUntilReadyAsync(int timeoutMilliseconds)
    {
        if (IsReady)
            return true;
        if (timeoutMilliseconds > 25)
        {
            if (ReadySemaphore.Wait(15))
            {
                ReadySemaphore.Release();
                return true;
            }

            if (await ReadySemaphore.WaitAsync(timeoutMilliseconds - 15))
            {
                ReadySemaphore.Release();
                return true;
            }
            return false;
        }

        if (await ReadySemaphore.WaitAsync(timeoutMilliseconds))
        {
            ReadySemaphore.Release();
            return true;
        }
        return false;
    }

    #endregion

    #region Events

    /// <summary>
    /// Fired when this <see cref="Node"/>'s readiness to be updated or drawn changes
    /// </summary>
    /// <remarks>
    /// Specifically, when this <see cref="Node.IsActive"/> changes
    /// </remarks>
    public NodeReadinessChangedEvent? ActiveChanged;

    #endregion

    #region Public Methods

    /// <summary>
    /// This method is called automatically when the <see cref="Node"/> is to be updated
    /// </summary>
    /// <param name="delta">The amount of time that has passed since the last update batch call</param>
    /// <returns>Whether the update sequence should be propagated into this <see cref="Node"/>'s children. If this is false, Update handlers for children will also be skipped</returns>
    protected virtual ValueTask<bool> Updating(TimeSpan delta) => ValueTask.FromResult(true);

    /// <summary>
    /// The batch this <see cref="Node"/> should be assigned to
    /// </summary>
    public UpdateBatch UpdateBatch { get; protected init; }

    #endregion

    #region FrameSkip

    internal readonly struct SkipData
    {
        public readonly bool MarkedForSkip;
        public readonly uint Frames;
        public readonly TimeSpan? Time;

        public SkipData(uint frames) : this()
        {
            Frames = frames;
            MarkedForSkip = true;
        }

        public SkipData(TimeSpan time) : this()
        {
            if (time <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(time), "Parameter 'time' must be larger than zero");
            Frames = AssortedExtensions.PrimeNumberNearestToUInt32MaxValue;
            Time = time;
            MarkedForSkip = true;
        }
    }

    internal SkipData SkipDat;

    /// <summary>
    /// Schedules this Node to skip <paramref name="updateFrames"/> update frames starting after the frame after this method is called
    /// </summary>
    /// <remarks>
    /// Reminder: This is NOT the same as the Game's FPS! <see cref="GraphicsManager.FramesPerSecond"/> are different from the update thread's <see cref="Game.AverageDelta"/>
    /// </remarks>
    /// <param name="updateFrames">The amount of frames this node will be skipped for after this method is called</param>
    public void Skip(ushort updateFrames)
    {
        SkipDat = updateFrames is 0 ? default : (new((uint)(Game.FrameCount % updateFrames + updateFrames + 1)));
    }

    /// <summary>
    /// Schedules this Node to skip updating for <paramref name="time"/> starting after the frame after this method is called
    /// </summary>
    /// <param name="time">The amount of frames this node will be skipped for after this method is called</param>
    public void Skip(TimeSpan? time)
    {
        SkipDat = time is not TimeSpan t || t <= TimeSpan.Zero ? default : new(Game.TotalTime + t);
    }

    #endregion

    #endregion

    #region Functional Components

    #region Public Methods

    /// <summary>
    /// Instances and installs a <see cref="FunctionalComponent"/> into this <see cref="Node"/>
    /// </summary>
    /// <remarks>
    /// Due to C# generic constraint limitations, this method uses runtime reflection to check if <typeparamref name="TComponent"/> has a constructor accepting a single <see cref="Node"/> (or this instance's current derived type) as an argument. If it doesn't, this method WILL throw a runtime exception.
    /// </remarks>
    /// <returns>The newly installed <see cref="FunctionalComponent"/></returns>
    /// <typeparam name="TComponent">The type of FunctionalComponent to instance and install</typeparam>
    public TComponent Install<TComponent>() where TComponent : FunctionalComponent
    {
        TComponent comp;
        var ctorQuery = new Type[] { GetType() };

        var ctor = typeof(TComponent).GetConstructor(System.Reflection.BindingFlags.Public, ctorQuery) ??
            typeof(TComponent).GetConstructor(System.Reflection.BindingFlags.Public, NodeConstructorQuery) ??
            throw new InvalidOperationException($"FunctionalComponent of type {typeof(TComponent).FullName} does not have a constructor accepting a single Node or {GetType().Name}");

        comp = (TComponent)ctor.Invoke(null, new object[] { this })!;
        InternalInstall(comp);
        return comp;
    }
    private static readonly Type[] NodeConstructorQuery = new Type[] { typeof(Node) };

    /// <summary>
    /// Instances and installs the <see cref="FunctionalComponent"/> returned by <paramref name="factory"/> into this <see cref="Node"/>
    /// </summary>
    /// <returns>The newly installed <see cref="FunctionalComponent"/></returns>
    /// <typeparam name="TComponent">The type of FunctionalComponent to instance and install</typeparam>
    /// <param name="factory">The method that will instance the component</param>
    public TComponent Install<TComponent>(Func<Node, TComponent> factory) where TComponent : FunctionalComponent
    {
        var comp = factory(this);
        InternalInstall(comp);
        return comp;
    }

    /// <summary>
    /// Uninstalls and disposes of the given component from this <see cref="Node"/> if it was already installed
    /// </summary>
    /// <param name="component">The component to uninstall</param>
    /// <exception cref="ArgumentException">If the component is not installed in this <see cref="Node"/></exception>
    public void Uninstall(FunctionalComponent component)
    {
        if (!ReferenceEquals(component.Owner, this))
            throw new ArgumentException("The specified component is not installed in this Node", nameof(component));

        ComponentUninstalling(component);
        component.InternalUninstall();
        component.Dispose();
    }

    #endregion

    #region Internal

    private void InternalInstall(FunctionalComponent comp)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        comp.ThrowIfDisposed();
        if (!FilterComponent(comp, out var reason))
            throw new FunctionalComponentRejectedException(reason, this, comp);
        if (!comp.FilterNode(this, out reason))
            throw new NodeRejectedException(reason, comp, this);

        ComponentInstalling(comp);
        comp.InternalInstall(this);

        comp.Id = Components.Add(comp);

        ComponentInstalled?.Invoke(this, comp, Game.TotalTime);
    }

    private ComponentList Components = ComponentList.Empty.Clone();

    #endregion

    #region Reaction Methods

    #region Installation

    /// <summary>
    /// This method is called automatically when a <see cref="FunctionalComponent"/> is being installed into this <see cref="Node"/>
    /// </summary>
    protected virtual void ComponentInstalling(FunctionalComponent component) { }

    /// <summary>
    /// This method is called automatically when a <see cref="FunctionalComponent"/> is being uninstalled from this <see cref="Node"/>
    /// </summary>
    protected virtual void ComponentUninstalling(FunctionalComponent component) { }

    /// <summary>
    /// This method is called automatically when a <see cref="FunctionalComponent"/> is about to be updated
    /// </summary>
    /// <param name="component">The component that is about to be updated</param>
    /// <param name="delta">The amount of time that has passed since the last frame started and this one started</param>
    /// <returns><c>true</c> if the component should be updated, <c>false</c> otherwise</returns>
    protected virtual ValueTask<bool> ComponentUpdating(FunctionalComponent component, TimeSpan delta) => ValueTask.FromResult(true);

    #endregion

    #region Filters

    /// <summary>
    /// This method is called automatically before a <see cref="FunctionalComponent"/> is installed into this <see cref="Node"/>
    /// </summary>
    /// <remarks>
    /// The component will proceed to installation and <see cref="ComponentInstalling(FunctionalComponent)"/> after this method returns <c>true</c> and will throw an <see cref="FunctionalComponentRejectedException"/> if it returns <c>false</c>
    /// </remarks>
    /// <param name="component">The component that is to be installed</param>
    /// <param name="reasonForRejection">If this method returns true, describe the rejection here</param>
    protected virtual bool FilterComponent(FunctionalComponent component, [NotNullWhen(false)] out string? reasonForRejection)
    {
        reasonForRejection = null;
        return true;
    }

    #endregion

    #endregion

    #region Events

    /// <summary>
    /// Fired when a <see cref="Node"/> is installed with a <see cref="FunctionalComponent"/>
    /// </summary>
    public event NodeFunctionalComponentInstallEvent? ComponentInstalled;

    /// <summary>
    /// Fired when a <see cref="Node"/> has one of its <see cref="FunctionalComponent"/>s uninstalled
    /// </summary>
    public event NodeFunctionalComponentInstallEvent? ComponentUninstalled;

    #endregion

    #endregion

    #region Services and Dependency Injection

    internal readonly ServiceCollection _nodeServices = new(null);

    /// <summary>
    /// The <see cref="ServiceCollection"/> for this <see cref="Node"/>
    /// </summary>
    /// <remarks>
    /// Services to this <see cref="Node"/> will cascade down from the root <see cref="Scene"/>, if not overriden.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown if this node is not attached to a root <see cref="Scene"/>, directly or indirectly</exception>
    public ServiceCollection Services
    {
        get
        {
            ThrowIfNotAttached();
            return _nodeServices;
        }
    }

    #endregion

    #region Attachment and Node Tree

    #region Public Properties

    /// <summary>
    /// This <see cref="Node"/>'s identifier when attached to a <see cref="Scene"/>
    /// </summary>
    public int Id { get; private set; }

    /// <summary>
    /// This <see cref="Node"/>'s root <see cref="Scene"/>
    /// </summary>
    /// <remarks>
    /// Valid as long as this <see cref="Node"/> is attached to a <see cref="Scene"/>
    /// </remarks>
    public Scene? ParentScene
    {
        get => scene;
        set
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            if (ReferenceEquals(scene, value))
                return;
            scene = value;
            NodeSceneChanged?.Invoke(this, Game.TotalTime, value);
        }
    }
    private Scene? scene;

    #endregion

    #region Public Methods

    /// <summary>
    /// Attaches this <see cref="Node"/> into the given <see cref="Scene"/>
    /// </summary>
    /// <param name="rootScene">The <see cref="Scene"/> to attach into</param>
    public async ValueTask AttachTo(Scene rootScene)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ThrowIfAttached();

        InternalLog?.Information("Attaching to Scene {name}-{type}", rootScene.Name ?? "", rootScene.GetTypeName());
        if (!rootScene.FilterChildNode(this, out var reason))
            throw new ChildNodeRejectedException(reason, rootScene, this);

        await Attaching(rootScene);
        
        lock (rootScene.Sync)
        {
            Id = rootScene.RegisterNodeInScene(this);
        }

        updater = await rootScene.AssignUpdater(this);

        rootScene.AssignToUpdateBatch(this);

        ParentScene = rootScene;

        _nodeServices.SetPrev(rootScene.Services);

        IsReady = true;
    }

    /// <summary>
    /// Detaches this <see cref="Node"/> from its parent <see cref="Scene"/> or <see cref="Node"/>
    /// </summary>
    /// <remarks>
    /// Detaching a <see cref="Node"/> removes it from the upper portion of the tree, which means it naturally also removes the <see cref="Node"/> from any <see cref="Scene"/> it may have been attached to upwards of the tree
    /// </remarks>
    public async ValueTask Detach()
    {
        if (ParentScene is not Scene ps) return;

        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ThrowIfNotAttached();
        IsReady = false;
        InternalLog?.Information("Detaching from {name}-{type}", ps!.Name, ps.GetTypeName());

        await Detaching();
        updater = null;

        foreach (var comp in Components)
            comp.NodeDetachedFromScene();

        ps.ExtractFromUpdateBatch(this);
        ps.Children.Remove(Id);
        Id = -1;

        ParentScene = null;
    }

    #endregion

    #region Filters

    /// <summary>
    /// This method is automatically called when this node is about to be attached to <see cref="Scene"/>
    /// </summary>
    /// <param name="scene">The <see cref="Scene"/> in question</param>
    /// <param name="reasonForDenial">The optional reason for the denial of <paramref name="scene"/></param>
    /// <returns><c>true</c> if this node allows itself to have <paramref name="scene"/> as root. <c>false</c> otherwise, along with an optional reason string in <paramref name="reasonForDenial"/></returns>
    protected virtual bool FilterScene(Scene scene, [NotNullWhen(false)] out string? reasonForDenial)
    {
        reasonForDenial = null;
        return true;
    }

    #endregion

    #region Reaction Methods

    /// <summary>
    /// This method is called automatically when a <see cref="Node"/> is attaching onto a <see cref="Scene"/>
    /// </summary>
    /// <remarks>
    /// Called before <see cref="NodeSceneChanged"/>, or <see cref="NodeSceneChanged"/> are fired
    /// </remarks>
    /// <param name="scene">The <see cref="Scene"/> this <see cref="Node"/> was attached into</param>
    /// <returns>Nothing if async, <see cref="ValueTask.CompletedTask"/> otherwise</returns>
    protected virtual ValueTask Attaching(Scene scene) => ValueTask.CompletedTask;

    /// <summary>
    /// This method is called automatically when a <see cref="Node"/> is detaching from its scene
    /// </summary>
    /// <remarks>
    /// Called before <see cref="NodeSceneChanged"/>, or <see cref="NodeSceneChanged"/>
    /// </remarks>
    protected virtual ValueTask Detaching() => ValueTask.CompletedTask;

    #endregion

    #region Events

    /// <summary>
    /// Fired when the tree this <see cref="Node"/> belongs to is attached to, detached from, or attached into a different a <see cref="Scene"/>
    /// </summary>
    /// <remarks>
    /// A <see cref="Node"/>'s tree is considered attached if this <see cref="Node"/>, or one of this <see cref="Node"/>'s parents is attached to a <see cref="Scene"/>
    /// </remarks>
    public event NodeSceneEvent? NodeSceneChanged;

    #endregion

    #endregion

    #region Processing

    #region Handler Cache (This is only to be used by the parent scene)

    /// <summary>
    /// This belongs to this <see cref="Node"/>'s scene
    /// </summary>
    internal NodeUpdater? updater;

    #endregion

    #region Update

    internal async ValueTask InternalUpdate(TimeSpan delta)
    {
        if (!await Updating(delta))
            return;

        var pool = ArrayPool<ValueTask>.Shared;

#pragma warning disable CA2012 // Just like Roslyn is so kind to warn us about, this code right here has the potential to offer some nasty asynchrony bugs. Be careful here, remember ValueTasks must only ever be consumed once
        {
            int comps = Components.Count;
            ValueTask[] compTasks = pool.Rent(comps);
            try
            {
                int ind = 0;
                for (int i = 0; i < comps; i++)
                {
                    FunctionalComponent comp = Components.Get(i);
                    if (comp.IsReady && await ComponentUpdating(comp, delta))
                        compTasks[ind++] = comp.InternalUpdate(delta);
                }
                for (int i = 0; i < ind; i++)
                    await compTasks[i];
            }
            finally
            {
                pool.Return(compTasks, true);
            }
        }
#pragma warning restore CA2012
    }

    #endregion

    #endregion

    #region Helper Methods

    /// <summary>
    /// Throws a new <see cref="InvalidOperationException"/> if this <see cref="Node"/> is not attached to a <see cref="Scene"/>
    /// </summary>
    protected internal void ThrowIfNotAttached()
    {
        if (ParentScene is null)
            throw new InvalidOperationException("This Node is not attached to a scene");
    }

    /// <summary>
    /// Throws a new <see cref="InvalidOperationException"/> if this <see cref="Node"/> is attached to a <see cref="Scene"/>
    /// </summary>
    protected internal void ThrowIfAttached()
    {
        if (ParentScene is not null)
            throw new InvalidOperationException("This Node is already attached to a scene");
    }

    #endregion

    #region Disposal

    internal override void InternalDispose(bool disposing)
    {
        Components.Clear();
        Components = null!;
        updater = null;
        NodeSceneChanged = null;
        base.InternalDispose(disposing);
    }

    #endregion
}
