using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using VDStudios.MagicEngine.Internal;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents an active object in the <see cref="Game"/>
/// </summary>
/// <remarks>
/// A <see cref="Node"/> can be an entity, a bullet, code to update another <see cref="Node"/>'s values, or any kind of active game object that is not a <see cref="FunctionalComponent"/>. To make a <see cref="Node"/> updateable, have it implement one of <see cref="IUpdateableNode"/> or <see cref="IAsyncUpdateableNode"/>. To make a <see cref="Node"/> drawable, have it implement one of <see cref="IDrawableNode"/> or <see cref="IAsyncDrawableNode"/>
/// </remarks>
public abstract class Node : GameObject, IDisposable
{
    #region private

    private readonly object sync = new();

    private IServiceScope? services;

    private event Action<Node, int>? ParentDetachedEvent;
    private event Action<Node, Node, int>? ParentAttachedEvent;

    private void InternalParentDetached(Node parent, int parentIndex)
    {
        Root = null;
        ParentDetached(parent, parentIndex);
    }

    private void InternalParentAttached(Node parent, Node grandParent, int parentIndex)
    {
        Root = parent.Root;
        ParentAttached(parent, grandParent, parentIndex);
    }

    private void DetachNoLock()
    {
        ThrowIfDisposed();
        ThrowIfNotAttached();
        var prevParent = Parent!;

        if (Parent is Node parent)
        {
            services?.Dispose();
            int ind = Index;
            Index = -1;

            parent.Children.Remove(ind);

            for (int i = 0; i < parent.Children.Count; i++)
                parent.Children[i].Index = i;

            parent.ParentAttachedEvent -= InternalParentAttached;
            parent.ParentAttachedEvent -= ParentAttachedEvent;

            parent.ParentDetachedEvent -= InternalParentDetached;
            parent.ParentDetachedEvent -= ParentDetachedEvent;
        }

        if (Root is Scene root)
        {
#error not wired
        }

        Root = null;
        Parent = null;
        Parents = NodeList.Empty;
    }

    #endregion

    /// <summary>
    /// Instances and initializes a new <see cref="Node"/>
    /// </summary>
    public Node()
    {
        Parents = NodeList.Empty;
        Children = NodeList.Empty.Clone(sync);
    }

    /// <summary>
    /// Attaches this <see cref="Node"/> to a parent
    /// </summary>
    /// <remarks>
    /// This method checks for circular references
    /// </remarks>
    public void Attach(Node parent)
    {
        ThrowIfDisposed();
        lock (sync)
        {
            ThrowIfAttached();
            for (int i = 0; i < Parents.Count; i++)
                if (ReferenceEquals(parent.Parents[i], this))
                    throw new ArgumentException("Can't attach a node to one of its children", nameof(parent));
            Root = parent.Root;

            services = parent.services?.ServiceProvider.CreateScope();
            Index = parent.Children.Count;
            parent.Children.Add(this);

            parent.ParentAttachedEvent += ParentAttached;
            parent.ParentAttachedEvent += ParentAttachedEvent;

            parent.ParentDetachedEvent += ParentDetached;
            parent.ParentDetachedEvent += ParentDetachedEvent;

            Parents = parent.Parents.Clone(sync);
            Parents.Add(parent);
        }

        Attached(parent, Index, services?.ServiceProvider);
        NodeAttached?.Invoke(this, Game.TotalTime);
        ParentAttachedEvent?.Invoke(this, parent, Depth);
    }

    /// <summary>
    /// Attaches this <see cref="Node"/> to a <see cref="Scene"/>
    /// </summary>
    /// <param name="root"></param>
    public void Attach(Scene root)
    {
        ThrowIfDisposed();
        lock (sync)
        {
            ThrowIfAttached();
            Root = root;

#error Add Update, UpdateAsync, Draw and DrawAsync events in the same manner to wire. Each node should attach and detach directly to Root

            Depth = 0;
        }
        NodeDetached?.Invoke(this, Game.TotalTime);
        ParentDetachedEvent?.Invoke(this, Depth);
    }

    /// <summary>
    /// Detaches this <see cref="Node"/> from its parent
    /// </summary>
    public void Detach()
    {
        lock (sync)
            DetachNoLock();
    }

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
            throw new InvalidOperationException("This Node is not already attached");
    }

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
    /// Valid (not null) as long as this <see cref="Node"/> is attached
    /// </remarks>
    public Scene? Root
    {
        get => ThrowIfDisposed(root);
        private set
        {
            if (ReferenceEquals(root, value))
                return;
            
            if (value is null)
            {
                var prev = root;
                root = value;
                NodeDetachedFromScene?.Invoke(this, Game.TotalTime, prev!);
            }

            root = value!;
            NodeAttachedToScene?.Invoke(this, Game.TotalTime, root);
        }
    }
    private Scene? root;

    private List<IFunctionalComponent> Components { get; }

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
    /// This <see cref="Node"/>'s Index in its parents
    /// </summary>
    /// <remarks>
    /// Valid for as long as this <see cref="Node"/> is attached. -1 means a <see cref="Node"/> is not attached
    /// </remarks>
    public int Index 
    { 
        get => ThrowIfDisposed(index); 
        private set
        {
            if (index == value)
                return;
            var prev = index;
            index = value;
            IndexChanged?.Invoke(this, Game.TotalTime, prev, value);
        }
    }
    private int index;

    /// <summary>
    /// Runs when a <see cref="Node"/> is attached to a parent <see cref="Node"/>
    /// </summary>
    /// <param name="parent">The parent <see cref="Node"/></param>
    /// <param name="index">The <see cref="Index"/> of this <see cref="Node"/> in its parent</param>
    /// <param name="services">The services for this <see cref="Game"/> scoped for this node. If this node gets detached, the services will be invalidated and will need to be obtained again once its attached again. May be null if the parent <see cref="Node"/> is not attached</param>
    /// <remarks>
    /// This method is called before <see cref="NodeAttached"/> is fired
    /// </remarks>
    protected virtual void Attached(Node parent, int index, IServiceProvider? services) { }

    /// <summary>
    /// Runs when a <see cref="Node"/> is attached to a parent <see cref="Scene"/>
    /// </summary>
    /// <param name="parent">The parent <see cref="Scene"/></param>
    /// <param name="index">The <see cref="Index"/> of this <see cref="Node"/> in its parent</param>
    /// <param name="services">The services for this <see cref="Game"/> scoped for this node. If this node gets detached, the services will be invalidated and will need to be obtained again once its attached again. May be null if the parent <see cref="Node"/> is not attached</param>
    /// <remarks>
    /// This method is called before <see cref="NodeAttached"/> is fired
    /// </remarks>
    protected virtual void Attached(Scene parent, int index, IServiceProvider? services) { }

    /// <summary>
    /// Runs when a <see cref="Node"/> is detached from its parent
    /// </summary>
    /// <remarks>
    /// Services adquired through this <see cref="Node"/>'s <see cref="IServiceProvider"/> will become invalid after this method returns. This method is called before <see cref="NodeDetached"/> is fired
    /// </remarks>
    protected virtual void Detached() { }

    /// <summary>
    /// Runs when a <see cref="Node"/>'s parent is detached from its parent
    /// </summary>
    /// <param name="parentIndex">The index of the parent <see cref="Node"/></param>
    /// <param name="parent">The parent <see cref="Node"/> that was detached</param>
    protected virtual void ParentDetached(Node parent, int parentIndex) { }

    /// <summary>
    /// Runs when a <see cref="Node"/>'s parent is attached into another parent
    /// </summary>
    /// <param name="parent">The parent <see cref="Node"/> that was attached</param>
    /// <param name="grandParent">The new parent of <paramref name="parent"/></param>
    /// <param name="parentIndex">The index of <paramref name="parent"/>. <paramref name="grandParent"/>'s index should be <paramref name="parentIndex"/> + 1</param>
    protected virtual void ParentAttached(Node parent, Node grandParent, int parentIndex) { }

    /// <summary>
    /// Runs when this <see cref="Node"/>'s Index changes
    /// </summary>
    /// <param name="oldIndex">The previous <see cref="Index"/></param>
    /// <param name="newIndex">The new <see cref="Index"/></param>
    protected virtual void NodeIndexChanged(int oldIndex, int newIndex) { }

    #region Events

    /// <summary>
    /// Fired when this <see cref="Node"/> is attached to a <see cref="Root"/> <see cref="Scene"/>
    /// </summary>
    public event NodeSceneEvent? NodeAttachedToScene;

    /// <summary>
    /// Fired when this <see cref="Node"/> is detached from its <see cref="Root"/> <see cref="Scene"/>
    /// </summary>
    public event NodeSceneEvent? NodeDetachedFromScene;

    /// <summary>
    /// Fired when this <see cref="Node"/> is attached to either a parent <see cref="Node"/> or <see cref="Scene"/>
    /// </summary>
    public event NodeEvent? NodeAttached;

    /// <summary>
    /// Fired when this <see cref="Node"/> is detached from its parent <see cref="Node"/> or <see cref="Scene"/>
    /// </summary>
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
                    Children[i].Dispose();

                DetachNoLock();

                Parents = null!;
                Children = null!;

                ParentDetachedEvent = null;
                ParentAttachedEvent = null;
                NodeAttachedToScene = null;
                NodeDetachedFromScene = null;
                NodeAttached = null;
                NodeDetached = null;
                IndexChanged = null;
                FunctionalComponentInstalled = null;
                FunctionalComponentUninstalled = null;

                disposedValue = true;
            }
        }
    }

    /// <summary>
    /// Runs when the object is being disposed. Don't call this! It'll be called automatically! Call <see cref="Dispose()"/> instead
    /// </summary>
    /// <remarks>
    /// This <see cref="Node"/> will dispose of all of its children (and those children of theirs), Detach, drop both <see cref="Parents"/> and <see cref="Children"/>, and finally clear all of its events after this method returns
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
