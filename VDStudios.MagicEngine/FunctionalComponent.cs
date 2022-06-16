using Microsoft.Extensions.DependencyInjection;
using VDStudios.MagicEngine.Internal;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents encapsulated functionality of a Node
/// </summary>
/// <remarks>
/// Remember to implement <see cref="IUpdateable"/> or <see cref="IUpdateableAsync"/> to let your component be Updated
/// </remarks>
public abstract class FunctionalComponent<TNode> : GameObject, IDisposable, IFunctionalComponent where TNode : Node
{
    private IServiceScope? serviceScope;

    /// <summary>
    /// The <see cref="Node"/> this <see cref="FunctionalComponent"/> is currently attached to, if any
    /// </summary>
    protected TNode? AttachedNode { get; private set; }

    /// <summary>
    /// Represents the internal Component Index in the currently attached node
    /// </summary>
    public int Index { get; private set; }

    /// <summary>
    /// The current <see cref="IServiceProvider"/> for this <see cref="FunctionalComponent"/>
    /// </summary>
    /// <remarks>
    /// This provider will become invalid if this gets detached. Will become valid anew once it's reattached to a <see cref="Node"/>
    /// </remarks>
    public IServiceProvider Services => serviceScope?.ServiceProvider ?? throw new InvalidOperationException("This FunctionalComponent does not have a Service Provider attached. Is it detached?");

    internal void InternalAttach(TNode node, IServiceScope services, int index)
    {
        serviceScope = services;
        Attach(node, services.ServiceProvider);
        AttachedNode = node;
        Index = index;
    }

    internal void InternalDetach()
    {
        serviceScope?.Dispose();
        Detach();
        AttachedNode = null;
    }
    void IFunctionalComponent.InternalDetach() => InternalDetach();

    /// <summary>
    /// Attaches this component's functionality into <paramref name="node"/>
    /// </summary>
    /// <param name="services">The services of the <see cref="Game"/>, scoped for this Component</param>
    /// <param name="node">The node this component is currently being attached to</param>
    /// <remarks>
    /// <see cref="AttachedNode"/> will be set after this method is called, and <see cref="Node.FunctionalComponentAttached"/> will fire after that
    /// </remarks>
    protected virtual void Attach(TNode node, IServiceProvider services) { }

    /// <summary>
    /// Detaches this component's functionality from <see cref="AttachedNode"/>
    /// </summary>
    /// <remarks>
    /// <see cref="AttachedNode"/> will be null'd after this method is called, and <see cref="Node.FunctionalComponentDetached"/> will fire after that
    /// </remarks>
    protected virtual void Detach() { }

    #region IDisposable

    private bool disposedValue;
    /// <summary>
    /// Disposes this <see cref="FunctionalComponent"/>'s resources. If overriding, make sure to to call base.Dispose()
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            serviceScope?.Dispose();
            disposedValue = true;
        }
    }

    /// <inheritdoc/>
    ~FunctionalComponent()
    {
        Dispose(disposing: false);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}

/// <summary>
/// Represents encapsulated functionality of a Node that accepts any <see cref="Node"/>
/// </summary>
/// <remarks>
/// Remember to implement <see cref="IUpdateable"/> or <see cref="IUpdateableAsync"/> to let your component be Updated
/// </remarks>
public abstract class FunctionalComponent : FunctionalComponent<Node>
{

}