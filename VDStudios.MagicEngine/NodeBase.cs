using Microsoft.Extensions.DependencyInjection;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    private IServiceScope scope;
    internal IServiceProvider ServiceProvider => scope.ServiceProvider;

    #endregion

    #endregion

    internal NodeBase()
    {
        scope = Game.Instance.NewScope();
        Children = NodeList.Empty.Clone(sync);
    }

    /// <summary>
    /// Represents the Children or Subnodes of this <see cref="Node"/>
    /// </summary>
    /// <remarks>
    /// Due to thread safety concerns, the list is locked during reads
    /// </remarks>
    public NodeList Children { get; internal set; }

    #region IDisposable

    private bool disposedValue;

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
        Children = null;
        scope.Dispose();
    }

    /// <summary>
    /// Runs when the object is being disposed. Don't call this! It'll be called automatically! Call <see cref="Dispose()"/> instead
    /// </summary>
    /// <remarks>
    /// This <see cref="Node"/> will dispose of all of its children (and those children of theirs), Detach, drop both <see cref="Parents"/> and <see cref="Children"/>, uninstall and clear its components, and finally clear all of its events after this method returns
    /// </remarks>
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
    internal protected virtual bool FilterChildNode(Node child, [NotNullWhen(false)] out string? reasonForDenial)
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
    internal protected virtual ValueTask<NodeUpdater?> AssignUpdater(Node node) => ValueTask.FromResult<NodeUpdater?>(null);

    /// <summary>
    /// This method is automatically called when a Child node is being attached. It assigns a custom drawer to the child node, or <c>null</c> to use <see cref="HandleChildDraw(IDrawableNode)"/> instead
    /// </summary>
    /// <param name="node">The <see cref="Node"/> that is being attached, and should be assigned a drawer</param>
    /// <returns>The <see cref="NodeDrawer"/> specific to <paramref name="node"/>, or <c>null</c> to use <see cref="HandleChildDraw(IDrawableNode)"/> instead</returns>
    internal protected virtual ValueTask<NodeDrawer?> AssignDrawer(IDrawableNode node) => ValueTask.FromResult<NodeDrawer?>(null);

    #endregion

    #region Default Handlers

    /// <summary>
    /// This method is automatically called when a Child node is about to be updated, and it has no custom handler set
    /// </summary>
    /// <param name="node">The node about to be updated</param>
    /// <returns><c>true</c> if the update sequence should be propagated into <paramref name="node"/>, <c>false</c> otherwise</returns>
    protected virtual ValueTask<bool> HandleChildUpdate(Node node) => ValueTask.FromResult(true);

    /// <summary>
    /// This method is automatically called when a Child node is about to be registered into the Draw Queue, and it has no custom handler set
    /// </summary>
    /// <param name="node">The node about to be registered into the Draw Queue</param>
    /// <returns><c>true</c> if the drawing registration sequence should be propagated into <paramref name="node"/>, <c>false</c> otherwise</returns>
    protected virtual ValueTask<bool> HandleChildDraw(IDrawableNode node) => ValueTask.FromResult(true);

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
