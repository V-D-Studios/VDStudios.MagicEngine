using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using VDStudios.MagicEngine.Internal;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents encapsulated functionality of a Node. Will be disposed of after its uninstalled
/// </summary>
/// <remarks>
/// <see cref="FunctionalComponent"/>s update in respect to the <see cref="Node"/> they're installed onto. That is, rather than update themselves, <see cref="FunctionalComponent"/> should work to update the <see cref="Node"/> they're installed onto
/// </remarks>
public abstract class FunctionalComponent : GameObject, IDisposable
{
    internal TimeSpan sdl_lastUpdate;
    private IServiceScope? serviceScope;

    internal FunctionalComponent()
    {

    }

    /// <summary>
    /// <c>true</c> if this <see cref="FunctionalComponent"/> is ready and should be updated. <c>false</c> otherwise
    /// </summary>
    /// <remarks>
    /// If this property is <c>false</c>, this <see cref="FunctionalComponent"/> will be skipped. Defaults to <c>true</c> and must be set to <c>false</c> manually if desired
    /// </remarks>
    public bool IsReady { get; protected set; }

    /// <summary>
    /// This method is called automatically when this <see cref="FunctionalComponent"/> is to be updated
    /// </summary>
    /// <remarks>
    /// <see cref="FunctionalComponent"/>s update in respect to the <see cref="Node"/> they're installed onto. That is, rather than update themselves, <see cref="FunctionalComponent"/> should work to update the <see cref="Node"/> they're installed onto
    /// </remarks>
    /// <param name="delta">The amount of time that has passed since the last update batch call</param>
    /// <param name="componentDelta">The amount of time that has passed since the last time this <see cref="FunctionalComponent"/> was updated</param>
    /// <returns></returns>
    protected virtual ValueTask Update(TimeSpan delta, TimeSpan componentDelta) => ValueTask.CompletedTask;

    /// <summary>
    /// Represents the internal Component Index in the currently attached node
    /// </summary>
    public int Id { get; internal set; }

    /// <summary>
    /// The <see cref="Node"/> this <see cref="FunctionalComponent"/> is currently attached to, if any
    /// </summary>
    public Node? AttachedNode { get; private set; }

    internal void InternalInstall(Node node, int index)
    {
        Installing(node);
        AttachedNode = node;
        Index = index;
        node.NodeAttachedToScene += InternalNodeAttachedToSceneEventHandler;
        node.NodeDetached += InternalNodeDetachedHandler;
        InstalledOntoNode?.Invoke(this, Game.TotalTime, node);
    }

    private void InternalNodeDetachedHandler(Node node, TimeSpan timestamp)
    {
        serviceScope?.Dispose();
        NodeDetachedFromScene();
    }

    private void InternalNodeAttachedToSceneEventHandler(Node node, TimeSpan timestamp, Scene scene)
    {
        serviceScope = node.services!.ServiceProvider.CreateScope();
        NodeAttachedToScene(serviceScope.ServiceProvider);
    }

    /// <summary>
    /// Check <see cref="AttachedNode"/> first
    /// </summary>
    internal void InternalUninstall()
    {
        Uninstalling();
        AttachedNode!.NodeAttachedToScene -= InternalNodeAttachedToSceneEventHandler;
        AttachedNode.NodeDetached -= InternalNodeDetachedHandler;
        UninstalledFromNode?.Invoke(this, Game.TotalTime, AttachedNode);
        AttachedNode = null;
    }

    /// <summary>
    /// Uninstalls the current <see cref="FunctionalComponent"/> from its <see cref="Node"/>
    /// </summary>
    public void UninstallFromNode()
    {
        if (AttachedNode is null)
            throw new InvalidOperationException("This FunctionalComponent is not installed in any Node");
        AttachedNode.Uninstall(this);
    }

    /// <summary>
    /// Runs when this component's functionality is attached into <paramref name="node"/>
    /// </summary>
    /// <param name="node">The node this component is currently being attached to</param>
    /// <remarks>
    /// <see cref="AttachedNode"/> will be set after this method is called, and <see cref="Node.FunctionalComponentInstalled"/> will fire after that
    /// </remarks>
    protected virtual void Installing(Node node) { }

    /// <summary>
    /// Runs when this component's <see cref="AttachedNode"/> is attached to a <see cref="Scene"/>
    /// </summary>
    /// <param name="services">The <see cref="Scene"/>'s service provider scoped for <see cref="AttachedNode"/> scoped for this component</param>
    protected virtual void NodeAttachedToScene(IServiceProvider services) { }

    /// <summary>
    /// Runs when this component's <see cref="AttachedNode"/> is detached from a <see cref="Scene"/>
    /// </summary>
    protected virtual void NodeDetachedFromScene() { }

    /// <summary>
    /// Runs when this component's functionality is detached from <see cref="AttachedNode"/>
    /// </summary>
    /// <remarks>
    /// <see cref="AttachedNode"/> will be null'd after this method is called, and <see cref="Node.FunctionalComponentUninstalled"/> will fire after that, and finally the Component will be disposed
    /// </remarks>
    protected virtual void Uninstalling() { }

    #region Events

    /// <summary>
    /// Fired when this <see cref="FunctionalComponent"/>'s <see cref="Index"/> changes
    /// </summary>
    public event FunctionalComponentIndexChangedEvent? IndexChanged;

    /// <summary>
    /// Fired when this <see cref="FunctionalComponent"/> is installed onto a <see cref="Node"/>
    /// </summary>
    public event FunctionalComponentNodeEvent? InstalledOntoNode;

    /// <summary>
    /// Fired when this <see cref="FunctionalComponent"/> is uninstalled from a <see cref="Node"/>
    /// </summary>
    public event FunctionalComponentNodeEvent? UninstalledFromNode;

    #endregion

    #region Helpers

    /// <summary>
    /// Throws a <see cref="NotImplementedException"/> exception if the given method is not implemented
    /// </summary>
    /// <remarks>
    /// This method is useful to fill whichever update method you're not using
    /// </remarks>
    /// <param name="caller">Ignore this argument</param>
    protected void ThrowNotImplemented([CallerMemberName] string? caller = null)
        => throw new NotImplementedException($"Method {caller} is not implemented for this FunctionalComponent");

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
    /// Disposes this <see cref="FunctionalComponent"/>'s resources
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
    }

    private void IntDispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (AttachedNode is not null)
                UninstallFromNode();

            Dispose(disposing);

            serviceScope?.Dispose();
            AttachedNode = null;
            disposedValue = true;
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
    public void Dispose()
    {
        IntDispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
