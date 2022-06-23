using Microsoft.Extensions.DependencyInjection;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using VDStudios.MagicEngine.Exceptions;
using VDStudios.MagicEngine.Internal;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents an active object in the <see cref="Game"/>
/// </summary>
/// <remarks>
/// A <see cref="Node"/> can be an entity, a bullet, code to update another <see cref="Node"/>'s values, or any kind of active game object that is not a <see cref="FunctionalComponent"/>. To make a <see cref="Node"/> updateable, have it implement one of <see cref="IUpdateableNode"/> or <see cref="IAsyncUpdateableNode"/>. To make a <see cref="Node"/> drawable, have it implement one of <see cref="IDrawableNode"/> or <see cref="IAsyncDrawableNode"/>. A <see cref="Node"/> is responsible for updating its own <see cref="FunctionalComponent"/>
/// </remarks>
public abstract class Node : GameObject, IDisposable
{
    #region Assorted Node Fields

    private readonly object sync = new();

    internal IServiceScope? services;

    private event Action<Scene, bool>? AttachedToSceneEvent;

    #endregion

    #region Construction

    /// <summary>
    /// Instances and initializes a new <see cref="Node"/>
    /// </summary>
    public Node()
    {
        Parents = NodeList.Empty;
        Children = NodeList.Empty.Clone(sync);
    }

    #endregion

    #region Scene Handlers

    #region Internally Managed Methods

    private void AttachedToScene(Scene root, bool isDirectParent)
    {
        services = root.Services.CreateScope();
        Attached(root, Index, isDirectParent, services.ServiceProvider);
        Root = root;
        AttachedToSceneEvent?.Invoke(root, false);
    }

    #endregion

    #endregion

    #region Internally Managed Properties

    #region Public API Properties

    /// <summary>
    /// Represents this <see cref="Node"/>'s parent
    /// </summary>
    /// <remarks>
    /// Valid as long as this <see cref="Node"/> is attached. If this <see cref="Node"/> is attached directly to a <see cref="Scene"/>, this property will be null
    /// </remarks>
    public Node? Parent { get; private set; }

    /// <summary>
    /// Represents the Root <see cref="Scene"/> for this <see cref="Node"/>
    /// </summary>
    /// <remarks>
    /// Valid (not null) as long as this <see cref="Node"/> is attached to a root <see cref="Scene"/> at any point up the tree
    /// </remarks>
    public Scene? Root
    {
        get => ThrowIfDisposed(root);
        private set
        {
            if (ReferenceEquals(root, value))
                return;
            
            if (value is null)
                root = value;

            root = value!;
            AttachedToSceneEvent?.Invoke(root, false);
            NodeAttachedToScene?.Invoke(this, Game.TotalTime, root);
        }
    }
    private Scene? root;

    /// <summary>
    /// Represents the Children or Subnodes of this <see cref="Node"/>
    /// </summary>
    /// <remarks>
    /// Due to thread safety concerns, the list is locked during reads
    /// </remarks>
    public NodeList Children { get; private set; }

    /// <summary>
    /// Represents the Parents or Supernodes of this <see cref="Node"/>. The lower the number, the closer this parent is to the root
    /// </summary>
    /// <remarks>
    /// Due to thread safety concerns, the list is locked during reads
    /// </remarks>
    public NodeList Parents { get; private set; }

    /// <summary>
    /// The depth of this <see cref="Node"/> in the <see cref="Scene"/>'s Node tree
    /// </summary>
    /// <remarks>
    /// Represents the amount of parents this <see cref="Node"/> has
    /// </remarks>
    public int Depth { get; private set; }

    /// <summary>
    /// <c>true</c> if this <see cref="Node"/> is already attached to another <see cref="Node"/> or a <see cref="Scene"/>; <c>false</c> otherwise.
    /// </summary>
    public bool IsAttached => Index == -1;

    /// <summary>
    /// This <see cref="Node"/>'s Index in its parents. This property is subject to change when another child is detached
    /// </summary>
    /// <remarks>
    /// Valid for as long as this <see cref="Node"/> is attached. -1 means a <see cref="Node"/> is not attached
    /// </remarks>
    public int Index 
    { 
        get => ThrowIfDisposed(index); 
        internal set
        {
            if (index == value)
                return;
            var prev = index;
            index = value;
            IndexChanged?.Invoke(this, Game.TotalTime, prev, value);
        }
    }
    private int index = -1;

    #endregion

    #region Node (Protected) API Properties

    /// <summary>
    /// Represents an internal list of this <see cref="Node"/>'s synchronous components
    /// </summary>
    /// <remarks>
    /// A node is entirely responsible for updating its own components
    /// </remarks>
    protected IReadOnlyList<SynchronousFunctionalComponent> SynchronousComponents => _syncComponents;
    private List<SynchronousFunctionalComponent> _syncComponents = new();

    /// <summary>
    /// Represents an internal list of this <see cref="Node"/>'s asynchronous components
    /// </summary>
    /// <remarks>
    /// A node is entirely responsible for updating its own components
    /// </remarks>
    protected IReadOnlyList<AsynchronousFunctionalComponent> AsynchronousComponents => _asyncComponents;
    private List<AsynchronousFunctionalComponent> _asyncComponents = new();

    #endregion

    #endregion

    #region Self (Node) Handlers

    /// <summary>
    /// Runs when this <see cref="Node"/>'s Index changes
    /// </summary>
    /// <param name="oldIndex">The previous <see cref="Index"/></param>
    /// <param name="newIndex">The new <see cref="Index"/></param>
    protected virtual void NodeIndexChanged(int oldIndex, int newIndex) { }

    #endregion

    #region Child Node Handlers

    #region Sorters

    /// <summary>
    /// Sorts <paramref name="node"/> into being handled by either a custom update handler or by <see cref="UpdateSynchronousNode(IUpdateableNode)"/>
    /// </summary>
    /// <param name="node">The <see cref="IUpdateableNode"/> node to sort</param>
    /// <returns><c>null</c> to signal the use of <see cref="UpdateSynchronousNode(IUpdateableNode)"/>, a <see cref="NodeSynchronousUpdater{TNode}"/> of the appropriate type otherwise</returns>
    protected virtual NodeSynchronousUpdater? SortNode(IUpdateableNode node) => null;

    /// <summary>
    /// Sorts <paramref name="node"/> into being handled by either a custom update handler or by <see cref="UpdateAsynchronousNode(IAsyncUpdateableNode)"/>
    /// </summary>
    /// <param name="node">The <see cref="IAsyncUpdateableNode"/> node to sort</param>
    /// <returns><c>null</c> to signal the use of <see cref="UpdateAsynchronousNode(IAsyncUpdateableNode)"/>, a <see cref="NodeAsynchronousUpdater{TNode}"/> of the appropriate type otherwise</returns>
    protected virtual NodeAsynchronousUpdater? SortNode(IAsyncUpdateableNode node) => null;

    /// <summary>
    /// Sorts <paramref name="node"/> into being handled by either a custom draw handler or by <see cref="DrawSynchronousNode(IDrawableNode)"/>
    /// </summary>
    /// <param name="node">The <see cref="IDrawableNode"/> node to sort</param>
    /// <returns><c>null</c> to signal the use of <see cref="DrawSynchronousNode(IDrawableNode)"/>, a <see cref="NodeSynchronousDrawer{TNode}"/> of the appropriate type otherwise</returns>
    protected virtual NodeSynchronousDrawer? SortNode(IDrawableNode node) => null;

    /// <summary>
    /// Sorts <paramref name="node"/> into being handled by either a custom draw handler or by <see cref="DrawAsynchronousNode(IAsyncDrawableNode)"/>
    /// </summary>
    /// <param name="node">The <see cref="IAsyncDrawableNode"/> node to sort</param>
    /// <returns><c>null</c> to signal the use of <see cref="DrawAsynchronousNode(IAsyncDrawableNode)"/>, a <see cref="NodeSynchronousDrawer{TNode}"/> of the appropriate type otherwise</returns>
    protected virtual NodeAsynchronousDrawer? SortNode(IAsyncDrawableNode node) => null;

    #endregion

    #region Default Handlers

    /// <summary>
    /// The default handler for <see cref="IUpdateableNode"/>s
    /// </summary>
    /// <param name="node">The node to be updated</param>
    /// <returns><c>true</c> if the update sequence should be propagated into <paramref name="node"/>, <c>false</c> otherwise</returns>
    protected virtual bool UpdateSynchronousNode(IUpdateableNode node) => true;

    /// <summary>
    /// The default handler for <see cref="IAsyncUpdateableNode"/>s
    /// </summary>
    /// <param name="node">The node to be updated</param>
    /// <returns><c>true</c> if the update sequence should be propagated into <paramref name="node"/>, <c>false</c> otherwise</returns>
    protected virtual Task<bool> UpdateAsynchronousNode(IAsyncUpdateableNode node) => AssortedHelpers.TrueResult;

    /// <summary>
    /// The default handler for <see cref="IDrawableNode"/>s
    /// </summary>
    /// <param name="node">The node to be updated</param>
    /// <returns><c>true</c> if the update sequence should be propagated into <paramref name="node"/>, <c>false</c> otherwise</returns>
    protected virtual bool DrawSynchronousNode(IDrawableNode node) => true;

    /// <summary>
    /// The default handler for <see cref="IAsyncDrawableNode"/>s
    /// </summary>
    /// <param name="node">The node to be updated</param>
    /// <returns><c>true</c> if the update sequence should be propagated into <paramref name="node"/>, <c>false</c> otherwise</returns>
    protected virtual Task<bool> DrawAsynchronousNode(IAsyncDrawableNode node) => AssortedHelpers.TrueResult;

    #endregion

    #region Sorted Node Lists and data

    [Flags]
    internal enum Sorted : byte
    {
        NotSorted = 0,
        Update = 1 << 0,
        AsyncUpdate = 1 << 1,
        Draw = 1 << 2,
        AsyncDraw = 1 << 3,
    }

    internal Sorted SortedInto;
    internal NodeSynchronousUpdater? UpdateHandler;
    internal NodeAsynchronousUpdater? AsyncUpdateHandler;
    internal NodeAsynchronousDrawer? AsyncDrawHandler;
    internal NodeSynchronousDrawer? DrawHandler;

    #endregion

    #region Internally Managed Methods

    #region Child Perform Update

    #endregion

    internal async Task PropagateUpdate()
    {
        bool propagate = true;

        if (SortedInto.HasFlag(Sorted.AsyncUpdate))
            propagate = await (AsyncUpdateHandler?.PerformUpdate()
                            ?? Parent?.UpdateAsynchronousNode((IAsyncUpdateableNode)this)
                            ?? Root!.UpdateAsynchronousNode((IAsyncUpdateableNode)this));
        else if (SortedInto.HasFlag(Sorted.Update))
            propagate = UpdateHandler?.PerformUpdate()
                        ?? Parent?.UpdateSynchronousNode((IUpdateableNode)this)
                        ?? Root!.UpdateSynchronousNode((IUpdateableNode)this);

        if (!propagate)
            return;

        var pool = ArrayPool<Task>.Shared;
        int toUpdate = Children.Count;
        Task[] tasks = pool.Rent(toUpdate);
        try
        {
            lock (sync)
            {
                for (int i = 0; i < toUpdate; i++)
                    tasks[i] = Children.Get(i).PropagateUpdate();
            }
            for (int i = 0; i < toUpdate; i++)
                await tasks[i];
        }
        finally
        {
            pool.Return(tasks, true);
        }
    }

    internal async Task PropagateDraw()
    {
        bool propagate = true;
        if (SortedInto.HasFlag(Sorted.AsyncDraw))
            propagate = await (AsyncDrawHandler?.PerformDraw()
                            ?? Parent?.DrawAsynchronousNode((IAsyncDrawableNode)this)
                            ?? Root!.DrawAsynchronousNode((IAsyncDrawableNode)this));
        else if (SortedInto.HasFlag(Sorted.Draw))
            propagate = DrawHandler?.PerformDraw()
                        ?? Parent?.DrawSynchronousNode((IDrawableNode)this)
                        ?? Root!.DrawSynchronousNode((IDrawableNode)this);

        if (!propagate)
            return;

        var pool = ArrayPool<Task>.Shared;
        int toAddToDrawQueue = Children.Count;
        Task[] tasks = pool.Rent(toAddToDrawQueue);
        try
        {
            lock (sync)
            {
                for (int i = 0; i < toAddToDrawQueue; i++)
                    tasks[i] = Children.Get(i).PropagateDraw();
            }
            for (int i = 0; i < toAddToDrawQueue; i++)
                await tasks[i];
        }
        finally
        {
            pool.Return(tasks, true);
        }
    }

    private void InternalSort(Node node)
    {
        Sorted sorted = 0;
        if (node is IAsyncUpdateableNode aun)
        {
            node.AsyncUpdateHandler = SortNode(aun);
            sorted |= Sorted.AsyncUpdate;
        }
        else if (node is IUpdateableNode sun)
        {
            node.UpdateHandler = SortNode(sun);
            sorted |= Sorted.Update;
        }

        if (node is IAsyncDrawableNode adn)
        {
            node.AsyncDrawHandler = SortNode(adn);
            sorted |= Sorted.AsyncDraw;
        }
        else if (node is IDrawableNode sdn)
        {
            node.DrawHandler = SortNode(sdn);
            sorted |= Sorted.Draw;
        }
        node.SortedInto = sorted;
    }

    #endregion

    #region Attachment

    #region Public Methods

    /// <summary>
    /// Attaches <paramref name="child"/> to a this. This method is used for collection initialization syntax and is the same as calling <see cref="Attach(Node)"/>
    /// </summary>
    /// <remarks>
    /// This method checks for circular references
    /// </remarks>
    /// <param name="child">The child to attach</param>
    public void Add(Node child) => Attach(child);

    /// <summary>
    /// Attaches <paramref name="child"/> to a this
    /// </summary>
    /// <remarks>
    /// This method checks for circular references
    /// </remarks>
    /// <param name="child">The child to attach</param>
    public void Attach(Node child)
    {
        lock (sync)
            child.AttachTo(this);
    }

    /// <summary>
    /// Attaches this <see cref="Node"/> to a parent
    /// </summary>
    /// <remarks>
    /// This method checks for circular references
    /// </remarks>
    public void AttachTo(Node parent)
    {
        parent.ThrowIfDisposed();
        ThrowIfDisposed();
        if (ReferenceEquals(this, parent))
            throw new ArgumentException("A node can't be attached to itself", nameof(parent));
        lock (sync)
        {
            ThrowIfAttached();
            for (int i = 0; i < Parents.Count; i++)
                if (ReferenceEquals(parent.Parents[i], this))
                    throw new ArgumentException("Can't attach a node to one of its children", nameof(parent));

            Index = parent.Children.Count;
            parent.Children.Add(this);

            parent.AttachedToSceneEvent += AttachedToScene;

            Parents = parent.Parents.Clone(sync);
            Parents.Add(parent);
        }

        Root = parent.Root;

        Attached(parent, Index);
        NodeAttached?.Invoke(this, Game.TotalTime);
        parent.InternalSort(this);
    }

    internal void AttachToScene(Scene root)
    {
        ThrowIfDisposed();
        lock (sync)
        {
            ThrowIfAttached();
            Depth = 0;
        }
        Index = root.Nodes.Count;
        AttachedToScene(root, true);
        Root = root;
        NodeAttached?.Invoke(this, Game.TotalTime);
        root.InternalSort(this);
    }

    /// <summary>
    /// Attaches this <see cref="Node"/> to a <see cref="Scene"/>
    /// </summary>
    /// <param name="root"></param>
    public void AttachTo(Scene root) 
        => root.AttachNode(this);

    /// <summary>
    /// Detaches this <see cref="Node"/> from its parent
    /// </summary>
    public void Detach()
    {
        ThrowIfNotAttached();
        lock (sync)
            DetachNoLock();
        Detached();
    }

    #endregion

    #region Virtual Custom Handlers

    /// <summary>
    /// Runs when a <see cref="Node"/> is attached to a parent <see cref="Node"/>
    /// </summary>
    /// <param name="parent">The parent <see cref="Node"/></param>
    /// <param name="index">The <see cref="Index"/> of this <see cref="Node"/> in its parent</param>
    /// <remarks>
    /// This method is called before <see cref="NodeAttached"/> is fired
    /// </remarks>
    protected virtual void Attached(Node parent, int index) { }

    /// <summary>
    /// Runs when a <see cref="Node"/> is attached to a parent <see cref="Scene"/>
    /// </summary>
    /// <param name="root">The parent <see cref="Scene"/></param>
    /// <param name="index">The <see cref="Index"/> of this <see cref="Node"/> in its parent</param>
    /// <param name="services">The services for this <see cref="Game"/> scoped for this node. If this node gets detached, the services will be invalidated and will need to be obtained again once its attached again. May be null if the parent <see cref="Node"/> is not attached</param>
    /// <param name="isDirectParent">Whether or not this scene is the direct parent of this <see cref="Node"/></param>
    /// <remarks>
    /// This method is called before <see cref="NodeAttached"/> is fired
    /// </remarks>
    protected virtual void Attached(Scene root, int index, bool isDirectParent, IServiceProvider? services) { }

    /// <summary>
    /// Runs when a <see cref="Node"/> is detached from its parent
    /// </summary>
    /// <remarks>
    /// Services adquired through this <see cref="Node"/>'s <see cref="IServiceProvider"/> will become invalid after this method returns. This method is called before <see cref="NodeDetached"/> is fired
    /// </remarks>
    protected virtual void Detached() { }

    #endregion

    #region Internally Managed Methods

    private void DetachNoLock()
    {
        if (Index == -1)
            return;
        ThrowIfDisposed();
        var prevParent = Parent!;

        if (Parent is Node parent)
        {
            parent.AttachedToSceneEvent -= AttachedToScene;
            services?.Dispose();
            int ind = Index;
            Index = -1;

            parent.Children.Remove(ind);

            for (int i = 0; i < parent.Children.Count; i++)
                parent.Children.Get(i).Index = i;
        }

        if (Root is Scene root)
        {
            if (Parent is null)
                root.InternalDetachNode(this);
            Index = -1;
        }

        UpdateHandler = null;
        AsyncUpdateHandler = null;
        AsyncDrawHandler = null;
        DrawHandler = null;

        Root = null;
        Parent = null;
        Parents = NodeList.Empty;
    }

    #endregion

    #endregion

    #endregion

    #region FunctionalComponents

    #region Public Methods

    /// <summary>
    /// Instances and installs a <see cref="FunctionalComponent"/> into this <see cref="Node"/>
    /// </summary>
    /// <returns>The newly installed <see cref="FunctionalComponent"/></returns>
    /// <typeparam name="TComponent">The type of FunctionalComponent to instance and install</typeparam>
    public TComponent Install<TComponent>() where TComponent : FunctionalComponent, new()
    {
        var comp = new TComponent();
        InternalInstall(comp);
        return comp;
    }

    /// <summary>
    /// Instances and installs the <see cref="FunctionalComponent"/> returned by <paramref name="factory"/> into this <see cref="Node"/>
    /// </summary>
    /// <returns>The newly installed <see cref="FunctionalComponent"/></returns>
    /// <typeparam name="TComponent">The type of FunctionalComponent to instance and install</typeparam>
    /// <param name="factory">The method that will instance the component</param>
    public TComponent Install<TComponent>(Func<TComponent> factory) where TComponent : FunctionalComponent
    {
        var comp = factory();
        InternalInstall(comp);
        return comp;
    }

    /// <summary>
    /// Installs <paramref name="component"/> into this <see cref="Node"/>
    /// </summary>
    /// <returns>The newly installed <see cref="FunctionalComponent"/></returns>
    /// <param name="component">The component to install</param>
    public void Install(FunctionalComponent component)
        => InternalInstall(component);

    /// <summary>
    /// Uninstalls the given component from this <see cref="Node"/> if it was already installed
    /// </summary>
    /// <param name="component">The component to uninstall</param>
    /// <exception cref="ArgumentException">If the component is not installed in this <see cref="Node"/></exception>
    public void Uninstall(FunctionalComponent component)
    {
        if (!ReferenceEquals(component.AttachedNode, this))
            throw new ArgumentException("The specified component is not installed in this Node", nameof(component));

        ComponentUninstalling(component);

        component.InternalUninstall();

        if (component is AsynchronousFunctionalComponent afc)
        {
            _asyncComponents.RemoveAt(afc.Index);
            for (int i = 0; i < _asyncComponents.Count; i++)
            {
                var c = _asyncComponents[i];
                if (i != c.Index)
                    c.Index = i;
            }
        }
        else if (component is SynchronousFunctionalComponent sfc)
        {
            _syncComponents.RemoveAt(sfc.Index);
            for (int i = 0; i < _syncComponents.Count; i++)
            {
                var c = _syncComponents[i];
                if (i != c.Index)
                    c.Index = i;
            }
        }

        FunctionalComponentUninstalled?.Invoke(this, component, Game.TotalTime);
    }

    #endregion

    #region Internally Managed Methods

    private void InternalInstall(FunctionalComponent component)
    {
        if (!ComponentFilter(component, out var rfj))
            throw new FunctionalComponentRejectedException(rfj ?? "Unknown reason", this, component);

        ComponentInstalling(component);

        if (component is AsynchronousFunctionalComponent afc)
        {
            component.Index = _asyncComponents.Count;
            _asyncComponents.Add(afc);
        }
        else if (component is SynchronousFunctionalComponent sfc)
        {
            component.Index = _syncComponents.Count;
            _syncComponents.Add(sfc);
        }

        FunctionalComponentInstalled?.Invoke(this, component, Game.TotalTime);
    }

    #endregion

    #region Virtual User Handlers

    #region Installation

    /// <summary>
    /// Runs when a <see cref="FunctionalComponent"/> is being installed into this <see cref="Node"/>
    /// </summary>
    /// <remarks>
    /// The component will be sorted into either <see cref="SynchronousComponents"/> or <see cref="AsynchronousComponents"/> after this method returns.
    /// </remarks>
    protected virtual void ComponentInstalling(FunctionalComponent component) { }

    /// <summary>
    /// Runs when a <see cref="FunctionalComponent"/> is being uninstalled from this <see cref="Node"/>
    /// </summary>
    /// <remarks>
    /// The component will be taken out of either <see cref="SynchronousComponents"/> or <see cref="AsynchronousComponents"/> after this method returns.
    /// </remarks>
    protected virtual void ComponentUninstalling(FunctionalComponent component) { }

    #endregion

    #region Other

    /// <summary>
    /// Runs before a <see cref="FunctionalComponent"/> is installed into this <see cref="Node"/>
    /// </summary>
    /// <remarks>
    /// The component will proceed to installation and <see cref="ComponentInstalling(FunctionalComponent)"/> after this method returns <c>true</c> and will throw an <see cref="FunctionalComponentRejectedException"/> if it returns <c>false</c>
    /// </remarks>
    /// <param name="component">The component that is to be installed</param>
    /// <param name="reasonForRejection">If this method returns true, describe the rejection here</param>
    protected virtual bool ComponentFilter(FunctionalComponent component, [NotNullWhen(false)] out string? reasonForRejection) 
    {
        reasonForRejection = null;
        return true;
    }

    #endregion

    #endregion

    #endregion

    #region Helper Methods

    /// <summary>
    /// Throws a new <see cref="InvalidOperationException"/> if this <see cref="Node"/> is not attached to another <see cref="Node"/> or <see cref="Scene"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ThrowIfNotAttached()
    {
        if (Index is -1)
            throw new InvalidOperationException("This Node is not attached to anything");
    }

    /// <summary>
    /// Throws a new <see cref="ObjectDisposedException"/> if this <see cref="Node"/> has already been disposed of
    /// </summary>
    protected void ThrowIfDisposed()
    {
        if (disposedValue)
            throw new ObjectDisposedException(GetType().FullName);
    }

    /// <summary>
    /// Throws a new <see cref="ObjectDisposedException"/> if this <see cref="Node"/> has already been disposed of, otherwise, returns the passed value
    /// </summary>
    /// <remarks>
    /// This method is useful to ensure disposed safety in an expression body
    /// </remarks>
    [return: NotNullIfNotNull("v")]
    protected T? ThrowIfDisposed<T>(T? v) => disposedValue ? throw new ObjectDisposedException(GetType().FullName) : v;

    /// <summary>
    /// Throws a new <see cref="InvalidOperationException"/> if this <see cref="Node"/> is attached to another <see cref="Node"/> or <see cref="Scene"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ThrowIfAttached()
    {
        if (Index != -1)
            throw new InvalidOperationException("This Node is already attached");
    }

    #endregion

    #region Events

    /// <summary>
    /// Fired when this <see cref="Node"/> is attached to a <see cref="Root"/> <see cref="Scene"/>
    /// </summary>
    public event NodeSceneEvent? NodeAttachedToScene;

    /// <summary>
    /// Fired when this <see cref="Node"/> is attached to either a parent <see cref="Node"/> or <see cref="Scene"/>
    /// </summary>
    public event NodeEvent? NodeAttached;

    /// <summary>
    /// Fired when this <see cref="Node"/> is detached from its parent <see cref="Node"/> or <see cref="Scene"/>
    /// </summary>
    /// <remarks>
    /// When a <see cref="Node"/> is detached from its parent, it is also detached from the <see cref="Scene"/>
    /// </remarks>
    public event NodeEvent? NodeDetached;

    /// <summary>
    /// Fired when this <see cref="Node"/>'s <see cref="Index"/> changes
    /// </summary>
    public event NodeIndexChangedEvent? IndexChanged;

    /// <summary>
    /// Fired when a <see cref="Node"/> is installed with a <see cref="FunctionalComponent"/>
    /// </summary>
    public event NodeFunctionalComponentInstallEvent? FunctionalComponentInstalled;

    /// <summary>
    /// Fired when a <see cref="Node"/> has one of its <see cref="FunctionalComponent"/>s uninstalled
    /// </summary>
    public event NodeFunctionalComponentInstallEvent? FunctionalComponentUninstalled;

    #endregion

    #region IDisposable

    private bool disposedValue;

    private void InternalDispose(bool disposing)
    {
        lock (sync)
        {
            if (!disposedValue)
            {
                Dispose(disposing);

                for (int i = 0; i < Children.Count; i++)
                    Children.Get(i).Dispose();

                DetachNoLock();

                Parents = null!;
                Children = null!;

                AttachedToSceneEvent = null;
                NodeAttachedToScene = null;
                NodeAttached = null;
                NodeDetached = null;
                IndexChanged = null;
                FunctionalComponentInstalled = null;
                FunctionalComponentUninstalled = null;

                for (int i = 0; i < AsynchronousComponents.Count; i++)
                    _asyncComponents[i].UninstallFromNode();
                _asyncComponents = null!;
                for (int i = 0; i < SynchronousComponents.Count; i++)
                    _syncComponents[i].UninstallFromNode();
                _syncComponents = null!;

                disposedValue = true;
            }
        }
    }

    /// <summary>
    /// Runs when the object is being disposed. Don't call this! It'll be called automatically! Call <see cref="Dispose()"/> instead
    /// </summary>
    /// <remarks>
    /// This <see cref="Node"/> will dispose of all of its children (and those children of theirs), Detach, drop both <see cref="Parents"/> and <see cref="Children"/>, uninstall and clear its components, and finally clear all of its events after this method returns
    /// </remarks>
    protected virtual void Dispose(bool disposing) { }

    /// <summary>
    /// Disposes of this <see cref="Node"/> and all of its currently attached children
    /// </summary>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
