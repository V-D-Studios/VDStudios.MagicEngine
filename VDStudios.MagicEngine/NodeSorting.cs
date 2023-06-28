namespace VDStudios.MagicEngine;

#region Update

/// <summary>
/// Represents a custom update procedure for a <see cref="Node"/>
/// </summary>
/// <remarks>
/// You're not supposed to use this class directly. Instead, use <see cref="NodeUpdater{TNode}"/>
/// </remarks>
public abstract class NodeUpdater
{
    internal NodeUpdater() { }

    internal abstract ValueTask PerformUpdate();
}

/// <summary>
/// Represents a custom update procedure for a <see cref="Node"/>
/// </summary>
/// <typeparam name="TNode">The type of <see cref="Node"/> this Updater will handle</typeparam>
public sealed class NodeUpdater<TNode> : NodeUpdater where TNode : Node
{
    private readonly NodeUpdateHandler<TNode> Handler;
    private readonly TNode HandledNode;

    /// <summary>
    /// Instances a new Custom Update Procedure Handler for <typeparamref name="TNode"/>
    /// </summary>
    /// <param name="handler">The <see cref="Handler"/> to use</param>
    /// <param name="node">The <see cref="HandledNode"/> this handler will update</param>
    public NodeUpdater(NodeUpdateHandler<TNode> handler, TNode node)
    {
        Handler = handler;
        HandledNode = node;
    }

    internal override ValueTask PerformUpdate() => Handler(HandledNode);
}

/// <summary>
/// Represents a method that can be used to handle the update of a <see cref="Node"/> of type <typeparamref name="TNode"/>
/// </summary>
/// <typeparam name="TNode">The type of <see cref="Node"/> this method expects</typeparam>
/// <param name="node">The node in question</param>
/// <returns><c>true</c> if the update sequence should be propagated into <paramref name="node"/>, <c>false</c> otherwise</returns>
public delegate ValueTask NodeUpdateHandler<TNode>(TNode node) where TNode : Node;

#endregion

#region Draw

/// <summary>
/// Represents a custom <see cref="DrawOperation"/> registration procedure for a <see cref="IDrawableNode"/>
/// </summary>
/// <remarks>
/// You're not supposed to use this class directly. Instead, use <see cref="NodeUpdater{TNode}"/>
/// </remarks>
public abstract class NodeDrawRegistrar
{
    internal NodeDrawRegistrar() { }

    internal abstract ValueTask PerformDrawRegistration();
}

/// <summary>
/// Represents a custom <see cref="DrawOperation"/> registration procedure for a <see cref="IDrawableNode"/>
/// </summary>
/// <remarks>
/// This procedure doesn't actually hold any control over the <typeparamref name="TNode"/>'s registration procedure, but can be used to filter nodes to register and propagation
/// </remarks>
/// <typeparam name="TNode">The type of <see cref="Node"/> this Drawer will handle</typeparam>
public sealed class NodeDrawer<TNode> : NodeDrawRegistrar where TNode : Node, IDrawableNode
{
    private readonly NodeDrawHandler<TNode> Handler;
    private readonly TNode HandledNode;

    /// <summary>
    /// Instances a new Custom Drawing Registration Procedure Handler for <typeparamref name="TNode"/>
    /// </summary>
    /// <param name="handler">The <see cref="Handler"/> to use</param>
    /// <param name="node">The <see cref="HandledNode"/> this handler will add to the draw queue</param>
    public NodeDrawer(NodeDrawHandler<TNode> handler, TNode node)
    {
        Handler = handler;
        HandledNode = node;
    }

    internal override ValueTask PerformDrawRegistration() => Handler(HandledNode);
}

/// <summary>
/// Represents a method that can be used to handle the drawing registration of a <see cref="Node"/> of type <typeparamref name="TNode"/>
/// </summary>
/// <typeparam name="TNode">The type of <see cref="Node"/> this method expects</typeparam>
/// <param name="node">The node in question</param>
/// <returns><c>true</c> if the draw sequence should be propagated into <paramref name="node"/>, <c>false</c> otherwise</returns>
public delegate ValueTask NodeDrawHandler<TNode>(TNode node) where TNode : Node, IDrawableNode;

#endregion
