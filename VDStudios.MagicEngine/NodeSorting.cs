using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine;

#region Helper Classes

#region Asynchronous Draw

/// <summary>
/// Represents a custom update procedure for a <see cref="IAsyncDrawableNode"/>; not furniture
/// </summary>
/// <remarks>
/// You're not supposed to use this class directly. Instead, use <see cref="NodeAsynchronousDrawer{TNode}"/>
/// </remarks>
public abstract class NodeAsynchronousDrawer
{
    internal NodeAsynchronousDrawer() { }

    internal abstract Task<bool> PerformDraw();
}

/// <summary>
/// Represents a custom update procedure for a <see cref="IAsyncDrawableNode"/>; not furniture
/// </summary>
/// <typeparam name="TNode">The type of <see cref="Node"/> this Updater will handle</typeparam>
public sealed class NodeAsynchronousDrawer<TNode> : NodeAsynchronousDrawer where TNode : Node, IAsyncDrawableNode
{
    private readonly NodeAsynchronousDrawHandler<TNode> Handler;
    private readonly TNode HandledNode;

    /// <summary>
    /// Instances a new Custom Update Procedure Handler for <typeparamref name="TNode"/>
    /// </summary>
    /// <param name="handler">The <see cref="Handler"/> to use</param>
    /// <param name="node">The <see cref="HandledNode"/> this handler will update</param>
    public NodeAsynchronousDrawer(NodeAsynchronousDrawHandler<TNode> handler, TNode node)
    {
        Handler = handler;
        HandledNode = node;
    }

    internal override Task<bool> PerformDraw() => Handler(HandledNode);
}

/// <summary>
/// Represents a method that can be used to handle the asynchronous drawing registration of a <see cref="Node"/> of type <typeparamref name="TNode"/>
/// </summary>
/// <typeparam name="TNode">The type of <see cref="Node"/> this method expects</typeparam>
/// <param name="node">The node in question</param>
/// <returns><c>true</c> if the update sequence should be propagated into <paramref name="node"/>, <c>false</c> otherwise</returns>
public delegate Task<bool> NodeAsynchronousDrawHandler<TNode>(TNode node) where TNode : Node, IAsyncDrawableNode;

#endregion

#region Synchronous Draw

/// <summary>
/// Represents a custom update procedure for a <see cref="IDrawableNode"/>; not furniture
/// </summary>
/// <remarks>
/// You're not supposed to use this class directly. Instead, use <see cref="NodeSynchronousDrawer{TNode}"/>
/// </remarks>
public abstract class NodeSynchronousDrawer
{
    internal NodeSynchronousDrawer() { }

    internal abstract bool PerformDraw();
}

/// <summary>
/// Represents a custom update procedure for a <see cref="IDrawableNode"/>; not furniture
/// </summary>
/// <typeparam name="TNode">The type of <see cref="Node"/> this Updater will handle</typeparam>
public sealed class NodeSynchronousDrawer<TNode> : NodeSynchronousDrawer where TNode : Node, IDrawableNode
{
    private readonly NodeSynchronousDrawHandler<TNode> Handler;
    private readonly TNode HandledNode;

    /// <summary>
    /// Instances a new Custom Update Procedure Handler for <typeparamref name="TNode"/>
    /// </summary>
    /// <param name="handler">The <see cref="Handler"/> to use</param>
    /// <param name="node">The <see cref="HandledNode"/> this handler will update</param>
    public NodeSynchronousDrawer(NodeSynchronousDrawHandler<TNode> handler, TNode node)
    {
        Handler = handler;
        HandledNode = node;
    }

    internal override bool PerformDraw() => Handler(HandledNode);
}

/// <summary>
/// Represents a method that can be used to handle the drawing registration of a <see cref="Node"/> of type <typeparamref name="TNode"/>
/// </summary>
/// <typeparam name="TNode">The type of <see cref="Node"/> this method expects</typeparam>
/// <param name="node">The node in question</param>
/// <returns><c>true</c> if the update sequence should be propagated into <paramref name="node"/>, <c>false</c> otherwise</returns>
public delegate bool NodeSynchronousDrawHandler<TNode>(TNode node) where TNode : Node, IDrawableNode;

#endregion

#region Asynchronous Update

/// <summary>
/// Represents a custom update procedure for a <see cref="IAsyncUpdateableNode"/>
/// </summary>
/// <remarks>
/// You're not supposed to use this class directly. Instead, use <see cref="NodeAsynchronousUpdater{TNode}"/>
/// </remarks>
public abstract class NodeAsynchronousUpdater
{
    internal NodeAsynchronousUpdater() { }

    internal abstract Task<bool> PerformUpdate();
}

/// <summary>
/// Represents a custom update procedure for a <see cref="IAsyncUpdateableNode"/>
/// </summary>
/// <typeparam name="TNode">The type of <see cref="Node"/> this Updater will handle</typeparam>
public sealed class NodeAsynchronousUpdater<TNode> : NodeAsynchronousUpdater where TNode : Node, IAsyncUpdateableNode
{
    private readonly NodeAsynchronousUpdateHandler<TNode> Handler;
    private readonly TNode HandledNode;

    /// <summary>
    /// Instances a new Custom Update Procedure Handler for <typeparamref name="TNode"/>
    /// </summary>
    /// <param name="handler">The <see cref="Handler"/> to use</param>
    /// <param name="node">The <see cref="HandledNode"/> this handler will update</param>
    public NodeAsynchronousUpdater(NodeAsynchronousUpdateHandler<TNode> handler, TNode node)
    {
        Handler = handler;
        HandledNode = node;
    }

    internal override Task<bool> PerformUpdate() => Handler(HandledNode);
}

/// <summary>
/// Represents a method that can be used to handle the asynchronous update of a <see cref="Node"/> of type <typeparamref name="TNode"/>
/// </summary>
/// <typeparam name="TNode">The type of <see cref="Node"/> this method expects</typeparam>
/// <param name="node">The node in question</param>
/// <returns><c>true</c> if the update sequence should be propagated into <paramref name="node"/>, <c>false</c> otherwise</returns>
public delegate Task<bool> NodeAsynchronousUpdateHandler<TNode>(TNode node) where TNode : Node, IAsyncUpdateableNode;

#endregion

#region Synchronous Update

/// <summary>
/// Represents a custom update procedure for a <see cref="IUpdateableNode"/>
/// </summary>
/// <remarks>
/// You're not supposed to use this class directly. Instead, use <see cref="NodeSynchronousUpdater{TNode}"/>
/// </remarks>
public abstract class NodeSynchronousUpdater
{
    internal NodeSynchronousUpdater() { }

    internal abstract bool PerformUpdate();
}

/// <summary>
/// Represents a custom update procedure for a <see cref="IUpdateableNode"/>
/// </summary>
/// <typeparam name="TNode">The type of <see cref="Node"/> this Updater will handle</typeparam>
public sealed class NodeSynchronousUpdater<TNode> : NodeSynchronousUpdater where TNode : Node, IUpdateableNode
{
    private readonly NodeSynchronousUpdateHandler<TNode> Handler;
    private readonly TNode HandledNode;

    /// <summary>
    /// Instances a new Custom Update Procedure Handler for <typeparamref name="TNode"/>
    /// </summary>
    /// <param name="handler">The <see cref="Handler"/> to use</param>
    /// <param name="node">The <see cref="HandledNode"/> this handler will update</param>
    public NodeSynchronousUpdater(NodeSynchronousUpdateHandler<TNode> handler, TNode node)
    {
        Handler = handler;
        HandledNode = node;
    }

    internal override bool PerformUpdate() => Handler(HandledNode);
}

/// <summary>
/// Represents a method that can be used to handle the update of a <see cref="Node"/> of type <typeparamref name="TNode"/>
/// </summary>
/// <typeparam name="TNode">The type of <see cref="Node"/> this method expects</typeparam>
/// <param name="node">The node in question</param>
/// <returns><c>true</c> if the update sequence should be propagated into <paramref name="node"/>, <c>false</c> otherwise</returns>
public delegate bool NodeSynchronousUpdateHandler<TNode>(TNode node) where TNode : Node, IUpdateableNode;

#endregion

#endregion
