using Microsoft.Extensions.DependencyInjection;
using VDStudios.MagicEngine.Internal;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents encapsulated functionality of a Node
/// </summary>
public abstract class FunctionalComponent<TNode> : FunctionalComponentBase, IDisposable where TNode : Node
{
    private IServiceScope? serviceScope;

    /// <summary>
    /// The <see cref="Node"/> this <see cref="FunctionalComponent{TNode}"/> is currently attached to, if any
    /// </summary>
    protected TNode? AttachedNode { get; private set; }

    /// <summary>
    /// The current <see cref="IServiceProvider"/> for this <see cref="FunctionalComponent{TNode}"/>
    /// </summary>
    /// <remarks>
    /// This provider will become invalid if this gets detached. Will become valid anew once it's reattached to a <see cref="Node"/>
    /// </remarks>
    public IServiceProvider Services => serviceScope?.ServiceProvider ?? throw new InvalidOperationException("This FunctionalComponent does not have a Service Provider attached. Is it detached?");

    internal void InternalInstall(TNode node, IServiceScope services, int index)
    {
        serviceScope = services;
        Install(node, services.ServiceProvider);
        AttachedNode = node;
        Index = index;
    }

    internal void InternalUninstall()
    {
        serviceScope?.Dispose();
        serviceScope = null;
        Uninstall();
        AttachedNode = null;
    }

    /// <inheritdoc/>
    public override void UninstallFromNode()
    {
        if (AttachedNode is null)
            throw new InvalidOperationException("This FunctionalComponent is not installed in any Node");
        AttachedNode.Uninstall(this);
    }

    /// <summary>
    /// Attaches this component's functionality into <paramref name="node"/>
    /// </summary>
    /// <param name="services">The services of the <see cref="Game"/>, scoped for this Component</param>
    /// <param name="node">The node this component is currently being attached to</param>
    /// <remarks>
    /// <see cref="AttachedNode"/> will be set after this method is called, and <see cref="Node.FunctionalComponentInstalled"/> will fire after that
    /// </remarks>
    protected virtual void Install(TNode node, IServiceProvider services) { }

    /// <summary>
    /// Detaches this component's functionality from <see cref="AttachedNode"/>
    /// </summary>
    /// <remarks>
    /// <see cref="AttachedNode"/> will be null'd after this method is called, and <see cref="Node.FunctionalComponentUninstalled"/> will fire after that
    /// </remarks>
    protected void Uninstall() { }

    #region IDisposable

    private bool disposedValue;
    /// <summary>
    /// Disposes this <see cref="FunctionalComponent{TNode}"/>'s resources
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
    }

    /// <inheritdoc/>
    ~FunctionalComponent()
    {
        Dispose(disposing: false);
    }

    internal override void InternalDispose()
    {
        if (!disposedValue)
        {
            Dispose(true);

            serviceScope?.Dispose();
            disposedValue = true;
        }
    }

    #endregion
}

/// <summary>
/// Represents encapsulated functionality of a Node that accepts any <see cref="Node"/>
/// </summary>
/// <remarks>
/// Remember to implement <see cref="IUpdateableNode"/> or <see cref="IAsyncUpdateableNode"/> to let your component be Updated
/// </remarks>
public abstract class FunctionalComponent : FunctionalComponent<Node>
{

}