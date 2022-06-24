using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace VDStudios.MagicEngine;

#region Update

/// <summary>
/// Represents a custom update procedure for a <see cref="IUpdateableNode"/>
/// </summary>
/// <remarks>
/// You're not supposed to use this class directly. Instead, use <see cref="NodeUpdater{TNode}"/>
/// </remarks>
public abstract class NodeUpdater
{
    internal NodeUpdater() { }

    internal abstract ValueTask<bool> PerformUpdate();
}

/// <summary>
/// Represents a custom update procedure for a <see cref="IUpdateableNode"/>
/// </summary>
/// <typeparam name="TNode">The type of <see cref="Node"/> this Updater will handle</typeparam>
public sealed class NodeUpdater<TNode> : NodeUpdater where TNode : Node, IUpdateableNode
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

    internal override ValueTask<bool> PerformUpdate() => Handler(HandledNode);
}

/// <summary>
/// Represents a method that can be used to handle the update of a <see cref="Node"/> of type <typeparamref name="TNode"/>
/// </summary>
/// <typeparam name="TNode">The type of <see cref="Node"/> this method expects</typeparam>
/// <param name="node">The node in question</param>
/// <returns><c>true</c> if the update sequence should be propagated into <paramref name="node"/>, <c>false</c> otherwise</returns>
public delegate ValueTask<bool> NodeUpdateHandler<TNode>(TNode node) where TNode : Node, IUpdateableNode;

#endregion

#region Draw

/// <summary>
/// Represents a custom drawing registration procedure for a <see cref="IDrawableNode"/>
/// </summary>
/// <remarks>
/// You're not supposed to use this class directly. Instead, use <see cref="NodeUpdater{TNode}"/>
/// </remarks>
public abstract class NodeDrawer
{
    internal NodeDrawer() { }

    internal abstract ValueTask<bool> PerformDraw();
}

/// <summary>
/// Represents a custom drawing registration procedure for a <see cref="IDrawableNode"/>
/// </summary>
/// <typeparam name="TNode">The type of <see cref="Node"/> this Drawer will handle</typeparam>
public sealed class NodeDrawer<TNode> : NodeDrawer where TNode : Node, IDrawableNode
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

    internal override ValueTask<bool> PerformDraw() => Handler(HandledNode);
}

/// <summary>
/// Represents a method that can be used to handle the drawing registration of a <see cref="Node"/> of type <typeparamref name="TNode"/>
/// </summary>
/// <typeparam name="TNode">The type of <see cref="Node"/> this method expects</typeparam>
/// <param name="node">The node in question</param>
/// <returns><c>true</c> if the draw sequence should be propagated into <paramref name="node"/>, <c>false</c> otherwise</returns>
public delegate ValueTask<bool> NodeDrawHandler<TNode>(TNode node) where TNode : Node, IDrawableNode;

#endregion
