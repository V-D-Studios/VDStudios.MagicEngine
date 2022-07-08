using Microsoft.Extensions.DependencyInjection;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using VDStudios.MagicEngine.Exceptions;
using VDStudios.MagicEngine.Internal;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents an active object in the <see cref="Game"/>
/// </summary>
/// <remarks>
/// A <see cref="Node"/> can be an entity, a bullet, code to update another <see cref="Node"/>'s values, or any kind of active game object that is not a <see cref="FunctionalComponent"/>. To make a <see cref="Node"/> drawable, have it implement one of <see cref="IDrawableNode"/>. A <see cref="Node"/> is responsible for updating its own <see cref="FunctionalComponent"/>s
/// </remarks>
public abstract class Node : NodeBase
{
    #region Constructors

    /// <summary>
    /// Initializes basic properties and fields to give a <see cref="Node"/> its default functionality
    /// </summary>
    /// <remarks>
    /// Initializes: <see cref="NodeBase.ServiceProvider"/>, <see cref="NodeBase.Children"/>
    /// </remarks>
    protected Node() : base("Game Node Tree")
    {
        DrawableSelf = this as IDrawableNode;
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
    /// <c>true</c> if this <see cref="Node"/> is ready and should be updated and, if it implements <see cref="IDrawableNode"/>, added to the draw queue. <c>false</c> otherwise
    /// </summary>
    /// <remarks>
    /// If this property is <c>false</c>, this <see cref="Node"/> and its children will be skipped, along with any handlers it may have. Defaults to <c>true</c> and must be set to <c>false</c> manually if desired
    /// </remarks>
    public bool IsReady
    {
        get => isReady;
        protected set
        {
            if (isReady == value)
                return;
            isReady = value;
            ReadinessChanged?.Invoke(this, Game.TotalTime, value);
        }
    }
    private bool isReady = true;

    #endregion

    #region Events

    /// <summary>
    /// Fired when this <see cref="Node"/>'s readiness to be updated or drawn changes
    /// </summary>
    /// <remarks>
    /// Specifically, when this <see cref="Node.IsReady"/> changes
    /// </remarks>
    public NodeReadinessChangedEvent? ReadinessChanged;

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
        ThrowIfDisposed();
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

    #region Attachment and Node Tree

    #region Public Properties

    /// <summary>
    /// This <see cref="Node"/>'s identifier when attached as the child of another <see cref="Node"/> or <see cref="Scene"/>
    /// </summary>
    public int Id { get; private set; }

    /// <summary>
    /// This <see cref="Node"/>'s parent <see cref="Node"/> or <see cref="Scene"/>
    /// </summary>
    /// <remarks>
    /// Valid as long as this <see cref="Node"/> is attached to a parent <see cref="Node"/> or <see cref="Scene"/>. Otherwise, null
    /// </remarks>
    public NodeBase? Parent
    {
        get => parent;
        private set
        {
            ThrowIfDisposed();
            if (ReferenceEquals(parent, value))
                return;
            parent = value;
            NodeParentChanged?.Invoke(this, Game.TotalTime, value);
        }
    }
    private NodeBase? parent;

    /// <summary>
    /// Attempts to get the parent of this <see cref="Node"/> as a <see cref="Scene"/>
    /// </summary>
    /// <param name="parent">The parent of this <see cref="Scene"/>, or null if it's a <see cref="Node"/>, or null instead</param>
    /// <returns>Whether the parent is, indeed, a <see cref="Scene"/></returns>
    public bool TryGetParentScene([NotNullWhen(true)] out Scene? parent)
    {
        parent = Parent as Scene;
        return parent != null;
    }

    /// <summary>
    /// Attempts to get the parent of this <see cref="Node"/> as a <see cref="Node"/>
    /// </summary>
    /// <param name="parent">The parent of this <see cref="Node"/>, or null if it's a <see cref="Scene"/>, or null instead</param>
    /// <returns>Whether the parent is, indeed, a <see cref="Node"/></returns>
    public bool TryGetParentNode([NotNullWhen(true)] out Node? parent)
    {
        parent = Parent as Node;
        return parent != null;
    }

    /// <summary>
    /// This <see cref="Node"/>'s root <see cref="Scene"/>, where this <see cref="Node"/>'s parents, or this <see cref="Node"/> itself is ultimately attached to
    /// </summary>
    /// <remarks>
    /// Valid as long as this <see cref="Node"/> is attached to a parent <see cref="Node"/>, its parents are eventually attached to a <see cref="Scene"/>, or this is directly attached to a <see cref="Scene"/>. Otherwise, null
    /// </remarks>
    public Scene? Root
    {
        get => root;
        set
        {
            ThrowIfDisposed();
            if (ReferenceEquals(root, value))
                return;
            root = value;
            NodeTreeSceneChanged?.Invoke(this, Game.TotalTime, value);
        }
    }
    private Scene? root;

    #endregion

    #region Public Methods

    /// <summary>
    /// Attaches this <see cref="Node"/> into the given <see cref="Scene"/>
    /// </summary>
    /// <param name="parent">The <see cref="Scene"/> to attach into</param>
    public async ValueTask AttachTo(Scene parent)
    {
        ThrowIfDisposed();
        ThrowIfAttached();

        if (!parent.FilterChildNode(this, out var reason))
            throw new ChildNodeRejectedException(reason, parent, this);

        await Attaching(parent);
        AttachedToSceneEvent?.Invoke(parent, true);
        
        lock (parent.sync)
            Id = parent.Children.Add(this);

        updater = await parent.AssignUpdater(this);
        if (DrawableSelf is IDrawableNode dn)
            drawer = await parent.AssignDrawer(dn);

        parent.AssignToUpdateBatch(this);

        Root = parent;
        Parent = parent;
    }

    /// <summary>
    /// Attaches this <see cref="Node"/> into a parent <see cref="Node"/>
    /// </summary>
    /// <param name="parent">The parent <see cref="Node"/> to attach this into</param>
    public async ValueTask AttachTo(Node parent)
    {
        ThrowIfDisposed();
        ThrowIfAttached();

        if (!parent.FilterChildNode(this, out var reason))
            throw new ChildNodeRejectedException(reason, parent, this);
        
        if (!FilterParentNode(parent, out reason))
            throw new ParentNodeRejectedException(reason, this, parent);

        await Attaching(parent);

        lock (parent.sync)
            Id = parent.Children.Add(this);

        if (parent.Root is Scene root)
        {
            AttachingToRoot(root, false);
            foreach (var comp in Components)
                comp.NodeAttachedToScene(root);
        }

        parent.AssignToUpdateBatch(this);

        parent.DetachedFromSceneEvent += WhenDetachedFromScene;
        parent.AttachedToSceneEvent += WhenAttachedToScene;
        Parent = parent;
    }

    /// <summary>
    /// Attaches a Child <see cref="Node"/> to this <see cref="Node"/>
    /// </summary>
    /// <param name="child">The child <see cref="Node"/> to attach</param>
    public ValueTask Attach(Node child)
        => child.AttachTo(this);

    /// <summary>
    /// Detaches this <see cref="Node"/> from its parent <see cref="Scene"/> or <see cref="Node"/>
    /// </summary>
    /// <remarks>
    /// Detaching a <see cref="Node"/> removes it from the upper portion of the tree, which means it naturally also removes the <see cref="Node"/> from any <see cref="Scene"/> it may have been attached to upwards of the tree
    /// </remarks>
    public async ValueTask Detach()
    {
        ThrowIfDisposed();
        ThrowIfNotAttached();
        await Detaching();
        drawer = null;
        updater = null;

        foreach (var comp in Components)
            comp.NodeDetachedFromScene();

        if (TryGetParentNode(out var pn))
        {
            pn.ExtractFromUpdateBatch(this);
            pn.Children.Remove(Id);
            Id = -1;
            pn.AttachedToSceneEvent -= WhenAttachedToScene;
            pn.DetachedFromSceneEvent -= WhenDetachedFromScene;
        }
        else if (TryGetParentScene(out var ps))
        {
            ps.ExtractFromUpdateBatch(this);
            ps.Children.Remove(Id);
            Id = -1;
            DetachedFromSceneEvent?.Invoke(ps, true);
        }

        Root = null;
        Parent = null;
    }

    #endregion

    #region Internal

    #region To Scene Root

    private void WhenAttachedToScene(Scene scene, bool isDirectlyAttached)
    {
        if (!FilterSceneRoot(scene, isDirectlyAttached, out var reason)) 
            throw new ParentNodeRejectedException(reason, this, scene);

        AttachingToRoot(scene, isDirectlyAttached);
        foreach (var comp in Components)
            comp.NodeAttachedToScene(scene);
        Root = scene;
        NodeTreeSceneChanged?.Invoke(this, Game.TotalTime, scene);
        AttachedToSceneEvent?.Invoke(scene, false);
    }

    private void WhenDetachedFromScene(Scene scene, bool wasDirectlyAttached)
    {
        DetachingFromRoot(scene, wasDirectlyAttached);
        foreach (var comp in Components)
            comp.NodeDetachedFromScene();
        Root = null;
        NodeTreeSceneChanged?.Invoke(this, Game.TotalTime, null);
        DetachedFromSceneEvent?.Invoke(scene, false);
    }

    internal event Action<Scene, bool>? AttachedToSceneEvent;

    internal event Action<Scene, bool>? DetachedFromSceneEvent;

    #endregion

    #endregion

    #region Filters

    /// <summary>
    /// This method is automatically called when this node is about to be attached to a parent, and should be used to filter what Nodes are allowed to be parents of this <see cref="Node"/>
    /// </summary>
    /// <param name="parent">The node this is about to be attached into</param>
    /// <param name="reasonForDenial">The optional reason for the denial of <paramref name="parent"/></param>
    /// <returns><c>true</c> if this node allows itself to be attached into <paramref name="parent"/>. <c>false</c> otherwise, along with an optional reason string in <paramref name="reasonForDenial"/></returns>
    protected virtual bool FilterParentNode(Node parent, [NotNullWhen(false)] out string? reasonForDenial)
    {
        reasonForDenial = null;
        return true;
    }

    /// <summary>
    /// This method is automatically called when this node is about to be attached to a parent <see cref="Scene"/>, or the tree it belongs to has been attached to a <see cref="Scene"/>, and should be used to filter what <see cref="Scene"/>s are allowed to be <see cref="Root"/> to this <see cref="Node"/>
    /// </summary>
    /// <param name="scene">The <see cref="Scene"/> in question</param>
    /// <param name="isDirectlyAttached">Whether this <see cref="Node"/> is the direct child of <paramref name="scene"/></param>
    /// <param name="reasonForDenial">The optional reason for the denial of <paramref name="scene"/></param>
    /// <returns><c>true</c> if this node allows itself to have <paramref name="scene"/> as root. <c>false</c> otherwise, along with an optional reason string in <paramref name="reasonForDenial"/></returns>
    protected virtual bool FilterSceneRoot(Scene scene, bool isDirectlyAttached, [NotNullWhen(false)] out string? reasonForDenial)
    {
        reasonForDenial = null;
        return true;
    }

    #endregion

    #region Reaction Methods

    /// <summary>
    /// This method is called automatically when a <see cref="Node"/>'s tree is attaching onto a <see cref="Scene"/>
    /// </summary>
    /// <remarks>
    /// Called before <see cref="NodeTreeSceneChanged"/> is fired. A <see cref="Node"/>'s tree is considered attached if this <see cref="Node"/>, or one of this <see cref="Node"/>'s parents is attached to a <see cref="Scene"/>
    /// </remarks>
    /// <param name="root">The <see cref="Scene"/> this tree was attached into</param>
    /// <param name="isDirectlyAttached">Whether this <see cref="Node"/> was directly attached to the <see cref="Scene"/></param>
    protected virtual void AttachingToRoot(Scene root, bool isDirectlyAttached) { }

    /// <summary>
    /// This method is called automatically when a <see cref="Node"/>'s tree is detaching from a <see cref="Scene"/>
    /// </summary>
    /// <remarks>
    /// Called before <see cref="NodeTreeSceneChanged"/> is fired. A <see cref="Node"/>'s tree is considered attached if this <see cref="Node"/>, or one of this <see cref="Node"/>'s parents is attached to a <see cref="Scene"/>
    /// </remarks>
    /// <param name="root">The <see cref="Scene"/> this tree was attached into</param>
    /// <param name="wasDirectlyAttached">Whether this <see cref="Node"/> was directly attached to the <see cref="Scene"/></param>
    protected virtual void DetachingFromRoot(Scene root, bool wasDirectlyAttached) { }

    /// <summary>
    /// This method is called automatically when a <see cref="Node"/> is attaching onto another <see cref="Node"/>
    /// </summary>
    /// <remarks>
    /// Called before <see cref="NodeParentChanged"/> is fired
    /// </remarks>
    /// <param name="parent">The <see cref="Node"/> this <see cref="Node"/> was attached into</param>
    /// <returns>Nothing if async, <see cref="ValueTask.CompletedTask"/> otherwise</returns>
    protected virtual ValueTask Attaching(Node parent) => ValueTask.CompletedTask;

    /// <summary>
    /// This method is called automatically when a <see cref="Node"/> is attaching directly onto a <see cref="Scene"/>
    /// </summary>
    /// <remarks>
    /// Called before <see cref="NodeParentChanged"/>, or <see cref="NodeTreeSceneChanged"/> are fired
    /// </remarks>
    /// <param name="parent">The <see cref="Scene"/> this <see cref="Node"/> was attached into</param>
    /// <returns>Nothing if async, <see cref="ValueTask.CompletedTask"/> otherwise</returns>
    protected virtual ValueTask Attaching(Scene parent) => ValueTask.CompletedTask;

    /// <summary>
    /// This method is called automatically when a <see cref="Node"/> is detaching from its parent
    /// </summary>
    /// <remarks>
    /// Called before <see cref="NodeParentChanged"/>, or <see cref="NodeTreeSceneChanged"/>
    /// </remarks>
    protected virtual ValueTask Detaching() => ValueTask.CompletedTask;

    #endregion

    #region Events

    /// <summary>
    /// Fired when this <see cref="Node"/> is attached to, detached from, or attached into different parent <see cref="Node"/> or <see cref="Scene"/>
    /// </summary>
    public event NodeParentEvent? NodeParentChanged;

    /// <summary>
    /// Fired when the tree this <see cref="Node"/> belongs to is attached to, detached from, or attached into a different a <see cref="Scene"/>
    /// </summary>
    /// <remarks>
    /// A <see cref="Node"/>'s tree is considered attached if this <see cref="Node"/>, or one of this <see cref="Node"/>'s parents is attached to a <see cref="Scene"/>
    /// </remarks>
    public event NodeSceneEvent? NodeTreeSceneChanged;

    #endregion

    #endregion

    #region Child Nodes

    #region Handler Cache (This is only to be used by the parent node)

    /// <summary>
    /// This belongs to this <see cref="Node"/>'s parent
    /// </summary>
    internal NodeUpdater? updater;

    /// <summary>
    /// This belongs to this <see cref="Node"/>'s parent
    /// </summary>
    internal NodeDrawRegistrar? drawer;

    #endregion

    #region Propagation

    #region Update

    internal async ValueTask PropagateUpdate(TimeSpan delta)
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

        await InternalPropagateChildUpdate(delta);
#pragma warning restore CA2012
    }

    #endregion

    #region Draw

    internal async ValueTask PropagateDrawRegistration()
    {
        if (DrawableSelf is not IDrawableNode ds) 
            return;
        
        if (ds.HasPendingRegistrations)
            await ds.RegisterDrawOperations(Game.MainGraphicsManager, Game.ActiveGraphicsManagers);

        if (ds.SkipDrawPropagation)
            return;

#pragma warning disable CA2012 // Just like Roslyn is so kind to warn us about, this code right here has the potential to offer some nasty asynchrony bugs. Be careful here, remember ValueTasks must only ever be consumed once

        var pool = ArrayPool<ValueTask>.Shared;
        int toUpdate = Children.Count;
        ValueTask[] tasks = pool.Rent(toUpdate);
        try
        {
            int ind = 0;
            lock (sync)
            {
                for (int i = 0; i < toUpdate; i++)
                {
                    var child = Children.Get(i);
                    if (child.IsReady)
                        tasks[ind++] = InternalHandleChildDrawRegistration(child);
                }
            }
            for (int i = 0; i < ind; i++)
                await tasks[i];
        }
        finally
        {
            pool.Return(tasks, true);
        }
#pragma warning restore CA2012
    }

    private async ValueTask InternalHandleChildDrawRegistration(Node node)
    {
        if (node.drawer is NodeDrawRegistrar drawer
            ? await drawer.PerformDrawRegistration()
            : node.DrawableSelf is not IDrawableNode n || await HandleChildRegisterDrawOperations(n))
            await node.PropagateDrawRegistration();
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
        if (Parent is null)
            throw new InvalidOperationException("This Node is not attached to anything");
    }

    /// <summary>
    /// Throws a new <see cref="InvalidOperationException"/> if this <see cref="Node"/> is attached to another <see cref="Node"/> or <see cref="Scene"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ThrowIfAttached()
    {
        if (Parent is not null)
            throw new InvalidOperationException("This Node is already attached");
    }

    #endregion

    #region Disposal

    internal override void InternalDispose(bool disposing)
    {
        Components.Clear();
        Components = null!;
        updater = null;
        drawer = null;
        DrawableSelf = null;
        AttachedToSceneEvent = null;
        NodeTreeSceneChanged = null;
        NodeParentChanged = null;
        base.InternalDispose(disposing);
    }

    #endregion
}
