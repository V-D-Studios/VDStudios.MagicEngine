using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDStudios.MagicEngine.Exceptions;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents encapsulated functionality of a Node. Will be disposed of after its uninstalled
/// </summary>
/// <remarks>
/// <see cref="FunctionalComponent"/>s update in respect to the <see cref="Node"/> they're installed onto. That is, rather than update themselves, <see cref="FunctionalComponent"/> should work to update the <see cref="Node"/> they're installed onto
/// </remarks>
public abstract class FunctionalComponent : GameObject
{
    #region Construction

    /// <summary>
    /// Instances a new <see cref="FunctionalComponent"/> and installs it onto <see cref="Node"/>
    /// </summary>
    /// <param name="node"></param>
    public FunctionalComponent(Node node) : base("Node Functionality", "Update")
    {
        Owner = node;
        scope = node.ServiceProvider.CreateScope();
    }

    #endregion

    #region Services

    private readonly IServiceScope scope;

    /// <summary>
    /// Represents a Service Provider scoped for this component's owner scoped for this component
    /// </summary>
    public IServiceProvider Services => scope.ServiceProvider;

    #endregion

    #region Node Tree

    #region Reaction Methods

    /// <summary>
    /// This method is automatically called when this component's <see cref="Owner"/> is attached to a <see cref="Scene"/>
    /// </summary>
    /// <param name="scene">The <see cref="Scene"/> this component's node is being attached to</param>
    protected internal virtual void NodeAttachedToScene(Scene scene) { }

    /// <summary>
    /// This method is automatically called when this component's <see cref="Owner"/> is detached from a <see cref="Scene"/>
    /// </summary>
    protected internal virtual void NodeDetachedFromScene() { }

    #endregion

    #endregion

    #region Installation

    #region Public Properties

    /// <summary>
    /// Represents the internal Component Index in the currently attached node
    /// </summary>
    public int Id { get; internal set; }

    /// <summary>
    /// The <see cref="Node"/> this <see cref="FunctionalComponent"/> is currently attached to, if any
    /// </summary>
    public Node Owner { get; }

    #endregion

    #region Reaction Methods

    /// <summary>
    /// This method is called automatically when this component's functionality is attached into <paramref name="node"/>
    /// </summary>
    /// <param name="node">The node this component is currently being attached to</param>
    /// <remarks>
    /// <see cref="Owner"/> will be set after this method is called, and <see cref="Node.ComponentInstalled"/> will fire after that
    /// </remarks>
    protected internal virtual void Installing(Node node) { }

    /// <summary>
    /// This method is called automatically when this component's functionality is detached from <see cref="Owner"/>
    /// </summary>
    /// <remarks>
    /// <see cref="Owner"/> will be null'd after this method is called, and <see cref="Node.ComponentUninstalled"/> will fire after that, and finally the Component will be disposed
    /// </remarks>
    protected internal virtual void Uninstalling() { }

    #endregion

    #region Filters

    /// <summary>
    /// This method is called automatically before this <see cref="FunctionalComponent"/> is installed onto <paramref name="node"/>
    /// </summary>
    /// <param name="node">The node this component is going to be installed onto</param>
    /// <param name="reasonForRejection">If this method returns true, describe the rejection here</param>
    protected internal virtual bool FilterNode(Node node, [NotNullWhen(false)] out string? reasonForRejection)
    {
        reasonForRejection = null;
        return true;
    }

    #endregion

    #region Internal

    internal void InternalInstall(Node node) 
    {
        Installing(node);
    }

    internal void InternalUninstall()
    {
        Uninstalling();
    }

    #endregion

    #endregion

    #region Update

    #region Public Properties

    /// <summary>
    /// <c>true</c> if this <see cref="FunctionalComponent"/> is ready and should be updated. <c>false</c> otherwise
    /// </summary>
    /// <remarks>
    /// If this property is <c>false</c>, this <see cref="FunctionalComponent"/> will be skipped. Defaults to <c>true</c> and must be set to <c>false</c> manually if desired
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
    /// Fired when this <see cref="FunctionalComponent"/>'s readiness to be updated changes
    /// </summary>
    /// <remarks>
    /// Specifically, when this <see cref="FunctionalComponent.IsReady"/> changes
    /// </remarks>
    public FunctionalComponentReadinessChangedEvent? ReadinessChanged;

    #endregion

    #region Reaction Methods

    /// <summary>
    /// This method is called automatically when this <see cref="FunctionalComponent"/> is to be updated
    /// </summary>
    /// <remarks>
    /// <see cref="FunctionalComponent"/>s update in respect to the <see cref="Node"/> they're installed onto. That is, rather than update themselves, <see cref="FunctionalComponent"/> should work to update the <see cref="Node"/> they're installed onto
    /// </remarks>
    /// <param name="delta">The amount of time that has passed since the last update batch call</param>
    /// <param name="componentDelta">The amount of time that has passed since the last time this <see cref="FunctionalComponent"/> was updated. Will be zero the first time the component is updated</param>
    /// <returns></returns>
    protected virtual ValueTask Update(TimeSpan delta, TimeSpan componentDelta) => ValueTask.CompletedTask;

    #endregion

    #region Internal

    private Stopwatch stopwatch = new();

    internal async ValueTask InternalUpdate(TimeSpan delta)
    {
        await Update(delta, stopwatch.Elapsed);
        stopwatch.Restart();
    }

    #endregion

    #endregion

    #region IDisposable

    /// <summary>
    /// Throws a new <see cref="ObjectDisposedException"/> if this <see cref="Node"/> has already been disposed of
    /// </summary>
    protected internal void ThrowIfDisposed()
    {
        if (disposedValue)
            throw new ObjectDisposedException(GetType().FullName);
    }

    private bool disposedValue;

    /// <summary>
    /// Disposes this <see cref="FunctionalComponent"/>'s resources. FunctionalComponents don't explicitly implement <see cref="IDisposable"/> due to the fact that they are automatically disposed when uninstalled from their <see cref="Node"/>
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
    }

    private void IntDispose(bool disposing)
    {
        if (!disposedValue)
        {
            scope.Dispose();
            stopwatch = null;
            disposedValue = true;

            Dispose(disposing);
        }
    }

    /// <inheritdoc/>
    ~FunctionalComponent()
    {
        IntDispose(false);
    }

    /// <summary>
    /// Disposes this <see cref="FunctionalComponent"/>'s resources
    /// </summary>
    internal void Dispose()
    {
        IntDispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
