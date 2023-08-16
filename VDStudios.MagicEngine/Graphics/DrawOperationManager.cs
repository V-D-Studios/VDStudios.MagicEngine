﻿using System.Collections.Concurrent;
using VDStudios.MagicEngine.Exceptions;
using VDStudios.MagicEngine.Internal;

namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// Represents a list of <see cref="DrawOperation{TGraphicsContext}"/> objects belonging to a <see cref="IDrawableNode{TGraphicsContext}"/> and their general behaviour
/// </summary>
public class DrawOperationManager<TGraphicsContext> : GameObject
    where TGraphicsContext : GraphicsContext<TGraphicsContext>
{
    private readonly ConcurrentDictionary<GraphicsManager<TGraphicsContext>, DrawOperationList<TGraphicsContext>> gm_dict = new();

    /// <summary>
    /// Instantiates a new object of type <see cref="DrawOperationManager{TGraphicsContext}"/>
    /// </summary>
    /// <param name="owner">The <see cref="IDrawableNode{TGraphicsContext}"/> that owns this <see cref="DrawOperationManager{TGraphicsContext}"/></param>
    protected DrawOperationManager(IDrawableNode<TGraphicsContext> owner)
        : base(owner.Game, "Rendering & Game Scene", "Draw Operations")
    {
        if (owner is not Node n)
            throw new InvalidOperationException($"The owner of a DrawOperationManager must be a node");

        Owner = owner;
        nodeOwner = n;
        _serv = new(n._nodeServices);
    }

    private readonly Node nodeOwner;

    #region Public Properties

    /// <summary>
    /// The <see cref="IDrawableNode{TGraphicsContext}"/> that owns this <see cref="DrawOperationManager{TGraphicsContext}"/>
    /// </summary>
    public IDrawableNode<TGraphicsContext> Owner { get; }

    /// <summary>
    /// Represents the <see cref="DrawOperation{TGraphicsContext}"/>s that belong to this <see cref="DrawOperationManager{TGraphicsContext}"/>
    /// </summary>
    /// <remarks>
    /// To remove a <see cref="DrawOperation{TGraphicsContext}"/>, dispose it
    /// </remarks>
    public DrawOperationList<TGraphicsContext> DrawOperations { get; } = new();

    #endregion

    #region Services and Dependency Injection

    /// <summary>
    /// The <see cref="ServiceCollection"/> for this <see cref="Node"/>
    /// </summary>
    /// <remarks>
    /// Services to this <see cref="Node"/> will cascade down from the root <see cref="Scene"/>, if not overriden.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown if this node is not attached to a root <see cref="Scene"/>, directly or indirectly</exception>
    public ServiceCollection Services
    {
        get
        {
            nodeOwner.ThrowIfNotAttached();
            return _serv;
        }
    }
    private readonly ServiceCollection _serv;

    #endregion

    #region Public Methods

    /// <summary>
    /// Adds a new <see cref="DrawOperation{TGraphicsContext}"/> of type <typeparamref name="TDrawOp"/> into this <see cref="DrawOperationManager{TGraphicsContext}"/>
    /// </summary>
    /// <typeparam name="TDrawOp">The type of <see cref="DrawOperation{TGraphicsContext}"/> to instantiate and add</typeparam>
    public TDrawOp AddDrawOperation<TDrawOp>(TDrawOp dop, GraphicsManager<TGraphicsContext>? graphicsManager = null) where TDrawOp : DrawOperation<TGraphicsContext>
    {
        InternalAddDrawOperation(dop, graphicsManager);
        return dop;
    }

    /// <summary>
    /// Adds a new <see cref="DrawOperation{TGraphicsContext}"/> of type <typeparamref name="TDrawOp"/> into this <see cref="DrawOperationManager{TGraphicsContext}"/>
    /// </summary>
    /// <typeparam name="TDrawOp">The type of <see cref="DrawOperation{TGraphicsContext}"/> to instantiate and add</typeparam>
    public TDrawOp AddDrawOperation<TDrawOp>(GraphicsManager<TGraphicsContext>? graphicsManager = null) where TDrawOp : DrawOperation<TGraphicsContext>, new()
    {
        var dop = new TDrawOp();
        InternalAddDrawOperation(dop, graphicsManager);
        return dop;
    }

    /// <summary>
    /// Adds a new <see cref="DrawOperation{TGraphicsContext}"/> of type <typeparamref name="TDrawOp"/> into this <see cref="DrawOperationManager{TGraphicsContext}"/>
    /// </summary>
    /// <typeparam name="TDrawOp">The type of <see cref="DrawOperation{TGraphicsContext}"/> to instantiate and add</typeparam>
    public TDrawOp AddDrawOperation<TDrawOp>(Func<TDrawOp> factory, GraphicsManager<TGraphicsContext>? graphicsManager = null) where TDrawOp : DrawOperation<TGraphicsContext>
    {
        var dop = factory();
        InternalAddDrawOperation(dop, graphicsManager);
        return dop;
    }

    /// <summary>
    /// Adds an object representing the <see cref="Node"/>'s drawing operations into the <see cref="IDrawQueue{TGraphicsContext, TOp}"/>. This method will be called from the respective <see cref="GraphicsManager{TGraphicsContext}"/>'s thread
    /// </summary>
    /// <remarks>
    /// By default, this method will enqueue <paramref name="operation"/> onto <paramref name="queue"/> with a priority of <c>1</c>
    /// </remarks>
    /// <param name="queue">The queue associated with <paramref name="operation"/> into which to add the draw operations</param>
    /// <param name="operation">A specific registered <see cref="DrawOperation{TGraphicsContext}"/></param>
    public virtual void AddToDrawQueue(IDrawQueue<DrawOperation<TGraphicsContext>, TGraphicsContext> queue, DrawOperation<TGraphicsContext> operation)
    {
        queue.Enqueue(operation, operation.PreferredPriority);
    }

    /// <summary>
    /// Gets the <see cref="DrawOperation{TGraphicsContext}"/>s that belong to <paramref name="graphicsManager"/>
    /// </summary>
    /// <param name="graphicsManager">The <see cref="GraphicsManager{TGraphicsContext}"/> owning the draw operations</param>
    public IEnumerable<DrawOperation<TGraphicsContext>> GetDrawOperations(GraphicsManager<TGraphicsContext> graphicsManager)
        => gm_dict.TryGetValue(graphicsManager, out var drawOperations) ? drawOperations : Array.Empty<DrawOperation<TGraphicsContext>>();

    #endregion

    #region Internal

    private void InternalAddDrawOperation(DrawOperation<TGraphicsContext> operation, GraphicsManager<TGraphicsContext>? graphicsManager)
    {
        graphicsManager
            ??= Game.MainGraphicsManager is GraphicsManager<TGraphicsContext> mgm
            ? mgm
            : throw new InvalidOperationException($"If the Game's MainGraphicsManager is not of type {typeof(GraphicsManager<TGraphicsContext>)}, then a non-null argument MUST be provided");

        GameMismatchException.ThrowIfMismatch(graphicsManager, operation, this);

        InternalLog?.Debug("Adding a new DrawOperation {objName}-{type}", operation.Name ?? "", operation.GetTypeName());
        operation.SetOwner(this);
        try
        {
            AddingDrawOperation(operation);
            operation.ThrowIfDisposed();
            DrawOperations.Add(operation);
            gm_dict.GetOrAdd(graphicsManager, gm => new DrawOperationList<TGraphicsContext>()).Add(operation);
            operation.AboutToDispose += Operation_AboutToDispose;
        }
        catch
        {
            operation.Dispose();
            throw;
        }
    }

    private void Operation_AboutToDispose(GameObject sender, TimeSpan timestamp)
    {
        if (sender is not DrawOperation<TGraphicsContext> dop) throw new InvalidOperationException($"Sender GameObject was not a DrawOperation");
        DrawOperations.Remove(dop);
    }

    #endregion

    #region Reaction Methods

    /// <summary>
    /// This method is called automatically when a new <see cref="DrawOperation{TGraphicsContext}"/> is being added onto this <see cref="DrawOperationManager{TGraphicsContext}"/>
    /// </summary>
    /// <param name="operation">The operation being added</param>
    protected virtual void AddingDrawOperation(DrawOperation<TGraphicsContext> operation) { }

    #endregion
}

/// <summary>
/// Represents a <see cref="DrawOperationManager{TGraphicsContext}"/> that accepts a <see cref="DrawQueueSelector"/> delegate method to act in place of inheriting and overriding <see cref="AddToDrawQueue(IDrawQueue{DrawOperation{TGraphicsContext}, TGraphicsContext}, DrawOperation{TGraphicsContext})"/>
/// </summary>
public sealed class DrawOperationManagerDrawQueueDelegate<TGraphicsContext> : DrawOperationManager<TGraphicsContext>
    where TGraphicsContext : GraphicsContext<TGraphicsContext>
{
    /// <summary>
    /// Represents a method that can add <paramref name="operation"/> appropriately into <paramref name="queue"/>
    /// </summary>
    /// <param name="queue">The <see cref="IDrawQueue{TGraphicsContext, T}"/> into which to add <paramref name="operation"/></param>
    /// <param name="operation">The operation in question</param>
    public delegate void DrawQueueSelector(IDrawQueue<DrawOperation<TGraphicsContext>, TGraphicsContext> queue, DrawOperation<TGraphicsContext> operation);

    private readonly DrawQueueSelector _drawQueueSelector;

    /// <inheritdoc/>
    public DrawOperationManagerDrawQueueDelegate(IDrawableNode<TGraphicsContext> owner, DrawQueueSelector drawQueueSelector) : base(owner)
    {
        ArgumentNullException.ThrowIfNull(drawQueueSelector);
        _drawQueueSelector = drawQueueSelector;
    }

    /// <inheritdoc/>
    public override void AddToDrawQueue(IDrawQueue<DrawOperation<TGraphicsContext>, TGraphicsContext> queue, DrawOperation<TGraphicsContext> operation)
        => _drawQueueSelector.Invoke(queue, operation);
}