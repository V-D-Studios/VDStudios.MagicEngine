using Microsoft.Extensions.DependencyInjection;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using VDStudios.MagicEngine.Internal;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents the base class for both <see cref="Node"/> and <see cref="Scene"/>. This class cannot be instanced or inherited directly
/// </summary>
public abstract class NodeBase : GameObject, IDisposable
{
    internal readonly object sync = new();

    #region Service Providers

    #region Fields

    /// <summary>
    /// Scene should ignore this, and let it remain null
    /// </summary>
    internal IDrawableNode? DrawableSelf;

    private readonly IServiceScope scope;
    internal IServiceProvider ServiceProvider => scope.ServiceProvider;

    #endregion

    #endregion

    internal NodeBase(string facility) : base(facility, "Update")
    {
        scope = Game.Instance.NewScope();
        Children = NodeList.Empty.Clone();
    }

    /// <summary>
    /// Represents the Children or Subnodes of this <see cref="Node"/>
    /// </summary>
    /// <remarks>
    /// Due to thread safety concerns, the list is locked during reads
    /// </remarks>
    public NodeList Children { get; internal set; }

    #region IDisposable

    private readonly bool disposedValue;

    /// <summary>
    /// Disposes of this <see cref="Node"/> and all of its currently attached children
    /// </summary>
    public void Dispose()
    {
        ThrowIfDisposed();
        Dispose(disposing: true);
        InternalDispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Always call base.Dispose AFTER your own disposal code
    /// </summary>
    /// <param name="disposing"></param>
    internal virtual void InternalDispose(bool disposing)
    {
        foreach (var child in Children)
            child.Dispose();
        Children = null!;
        scope.Dispose();
    }

    /// <summary>
    /// Runs when the object is being disposed. Don't call this! It'll be called automatically! Call <see cref="Dispose()"/> instead
    /// </summary>
    protected virtual void Dispose(bool disposing) { }

    #endregion

    #region Child Nodes

    #region Filters

    /// <summary>
    /// This method is automatically called when a Child node is about to be attached, and should be used to filter what Nodes are allowed to be children of this <see cref="Node"/>
    /// </summary>
    /// <param name="child">The node about to be attached</param>
    /// <param name="reasonForDenial">The optional reason for the denial of <paramref name="child"/></param>
    /// <returns><c>true</c> if the child node is allowed to be attached into this <see cref="Node"/>. <c>false</c> otherwise, along with an optional reason string in <paramref name="reasonForDenial"/></returns>
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

    /// <summary>
    /// This method is automatically called when a Child node is being attached. It assigns a custom drawer to the child node, or <c>null</c> to use <see cref="HandleChildRegisterDrawOperations(IDrawableNode)"/> instead
    /// </summary>
    /// <param name="node">The <see cref="Node"/> that is being attached, and should be assigned a drawer</param>
    /// <returns>The <see cref="NodeDrawRegistrar"/> specific to <paramref name="node"/>, or <c>null</c> to use <see cref="HandleChildRegisterDrawOperations(IDrawableNode)"/> instead</returns>
    protected internal virtual ValueTask<NodeDrawRegistrar?> AssignDrawer(IDrawableNode node) => ValueTask.FromResult<NodeDrawRegistrar?>(null);

    #endregion

    #region Default Handlers

    /// <summary>
    /// This method is automatically called when a Child node is about to be updated, and it has no custom handler set
    /// </summary>
    /// <param name="node">The node about to be updated</param>
    /// <returns><c>true</c> if the update sequence should be propagated into <paramref name="node"/>, <c>false</c> otherwise</returns>
    protected virtual ValueTask<bool> HandleChildUpdate(Node node) => ValueTask.FromResult(true);

    /// <summary>
    /// This method is automatically called when a Child node is about to be queried for <see cref="DrawOperation"/>s to register, and it has no custom handler set
    /// </summary>
    /// <param name="node">The node about to be queried</param>
    /// <returns><c>true</c> if the drawing registration sequence should be propagated into <paramref name="node"/>, <c>false</c> otherwise</returns>
    protected virtual ValueTask<bool> HandleChildRegisterDrawOperations(IDrawableNode node) => ValueTask.FromResult(true);

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

    internal async ValueTask InternalHandleChildDrawRegistration(Node node)
    {
        if (node.drawer is NodeDrawRegistrar drawer
            ? await drawer.PerformDrawRegistration()
            : node.DrawableSelf is not IDrawableNode n || await HandleChildRegisterDrawOperations(n))
            await node.PropagateDrawRegistration();
    }

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
        if (node.updater is NodeUpdater updater
            ? await updater.PerformUpdate()
            : await HandleChildUpdate(node))
            await node.PropagateUpdate(delta);
    }
    
    internal async ValueTask InternalPropagateChildUpdate(TimeSpan delta)
    {
        var pool = ArrayPool<ValueTask>.Shared;
        int toUpdate = Children.Count;
        ValueTask[] tasks = pool.Rent(toUpdate);
        try
        {
            int ind = 0;
            lock (sync)
            {
                for (int bi = 0; bi < UpdateBatchCollection.BatchCount; bi++)
                    for (int ti = UpdateSynchronicityBatch.BatchCount - 1; ti >= 0; ti--)
                    {
                        var batch = UpdateBatches[(UpdateBatch)bi, (AsynchronousTendency)ti];
                        if (batch is not null and { Count: > 0 })
                            foreach (var child in batch)
                                if (child.IsReady)
                                    tasks[ind++] = InternalHandleChildUpdate(child, delta);
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

    #region Helpers

    /// <summary>
    /// Throws a new <see cref="ObjectDisposedException"/> if this <see cref="Node"/> has already been disposed of
    /// </summary>
    protected internal void ThrowIfDisposed()
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

    #endregion
}
