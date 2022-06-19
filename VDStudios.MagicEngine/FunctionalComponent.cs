using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using VDStudios.MagicEngine.Internal;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents encapsulated functionality of a Node. Will be disposed of after its uninstalled
/// </summary>
/// <remarks>
/// User code can't directly inherit from this class. See <see cref="SynchronousFunctionalComponent"/> or <see cref="AsynchronousFunctionalComponent"/> instead
/// </remarks>
public abstract class FunctionalComponent : GameObject, IDisposable
{
    internal TimeSpan sdl_lastUpdate;
    private IServiceScope? serviceScope;

    internal FunctionalComponent(bool isAsync)
    {
        IsAsync = isAsync;
    }

    /// <summary>
    /// Represents the internal Component Index in the currently attached node
    /// </summary>
    public int Index
    {
        get => index;
        set
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
    /// Whether or not this <see cref="FunctionalComponent"/> is asynchronous
    /// </summary>
    /// <remarks>
    /// This property is <c>init</c> only and will not change after its constructed
    /// </remarks>
    public bool IsAsync { get; }

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

        GC.SuppressFinalize(this);
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
    }

    #endregion
}

/// <summary>
/// Represents encapsulated functionality of a Node that is not updated
/// </summary>
/// <remarks>
/// <see cref="NonUpdatingFunctionalComponent"/> are not internally added to a <see cref="Node"/>'s list, and must be held manually in a field, property or collection. Remember that components are disposed of after they're uninstalled
/// </remarks>
public abstract class NonUpdatingFunctionalComponent : FunctionalComponent
{
    /// <summary>
    /// Instances and initiates a NonUpdatingFunctionalComponent
    /// </summary>
    /// <remarks>
    /// Remember to toss <see cref="IServiceProvider"/> dependent code into <see cref="FunctionalComponent.Installing(Node)"/>
    /// </remarks>
    public NonUpdatingFunctionalComponent(bool isAsync) : base(isAsync) { }
}

/// <summary>
/// Represents encapsulated functionality of a Node that is updated asynchronously
/// </summary>
/// <remarks>
/// Remember that components are disposed of after they're uninstalled
/// </remarks>
public abstract class AsynchronousFunctionalComponent : FunctionalComponent
{
    /// <summary>
    /// Instances and initiates a AsynchronousFunctionalComponent
    /// </summary>
    /// <remarks>
    /// Remember to toss <see cref="IServiceProvider"/> dependent code into <see cref="FunctionalComponent.Installing(Node)"/>
    /// </remarks>
    protected AsynchronousFunctionalComponent() : base(true) { }

    /// <summary>
    /// This method is called after <see cref="UpdateAsync(TimeSpan)"/> and is where your code goes
    /// </summary>
    /// <param name="gameDelta">The amount of time that has passed since the last Update frame in the Game</param>
    /// <param name="componentDelta">The amount of time that has passed since the last time this component was updated. Will be 0 the first time it's updated</param>
    protected abstract ValueTask UpdateComponentAsync(TimeSpan gameDelta, TimeSpan componentDelta);

    /// <summary>
    /// Updates the current <see cref="FunctionalComponent"/>. Not all components are asynchronous, see <see cref="FunctionalComponent.IsAsync"/> to decide which method to call. 
    /// </summary>
    /// <param name="gameDelta">The amount of time that has passed since the last Update frame in the Game</param>
    public ValueTask UpdateAsync(TimeSpan gameDelta)
    {
        if (sdl_lastUpdate == default)
            return UpdateComponentAsync(gameDelta, TimeSpan.Zero);

        var ot = sdl_lastUpdate;
        sdl_lastUpdate = Game.TotalTime;
        return UpdateComponentAsync(gameDelta, ot - sdl_lastUpdate);
    }
}

/// <summary>
/// Represents encapsulated functionality of a Node that is updated synchronously
/// </summary>
/// <remarks>
/// Remember that components are disposed of after they're uninstalled
/// </remarks>
public abstract class SynchronousFunctionalComponent : FunctionalComponent
{
    /// <summary>
    /// Instances and initiates a SynchronousFunctionalComponent
    /// </summary>
    /// <remarks>
    /// Remember to toss <see cref="IServiceProvider"/> dependent code into <see cref="FunctionalComponent.Installing(Node)"/>
    /// </remarks>
    protected SynchronousFunctionalComponent() : base(false) { }

    /// <summary>
    /// Updates the current <see cref="FunctionalComponent"/>. Not all components are synchronous: see <see cref="FunctionalComponent.IsAsync"/> to decide which method to call. 
    /// </summary>
    /// <param name="gameDelta">The amount of time that has passed since the last Update frame in the Game</param>
    public void Update(TimeSpan gameDelta)
    {
        if (sdl_lastUpdate == default)
            UpdateComponent(gameDelta, TimeSpan.Zero);

        var ot = sdl_lastUpdate;
        sdl_lastUpdate = Game.TotalTime;
        UpdateComponent(gameDelta, ot - sdl_lastUpdate);
    }

    /// <summary>
    /// This method is called after <see cref="Update(TimeSpan)"/> and is where your code goes
    /// </summary>
    /// <param name="gameDelta">The amount of time that has passed since the last Update frame in the Game</param>
    /// <param name="componentDelta">The amount of time that has passed since the last time this component was updated. Will be 0 the first time it's updated</param>
    protected abstract void UpdateComponent(TimeSpan gameDelta, TimeSpan componentDelta);
}