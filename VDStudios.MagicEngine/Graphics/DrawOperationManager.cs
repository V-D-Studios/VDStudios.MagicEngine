using System.Collections.Concurrent;
using System.Diagnostics;
using VDStudios.MagicEngine.Exceptions;
using VDStudios.MagicEngine.Internal;

namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// Represents a list of <see cref="DrawOperation{TGraphicsContext}"/> objects belonging to a <see cref="Scene"/> and their general behaviour
/// </summary>
public class DrawOperationManager<TGraphicsContext> : GameObject
    where TGraphicsContext : GraphicsContext<TGraphicsContext>
{
    private readonly ConcurrentDictionary<GraphicsManager<TGraphicsContext>, DrawOperationList<TGraphicsContext>> gm_dict = new();

    /// <summary>
    /// Instantiates a new object of type <see cref="DrawOperationManager{TGraphicsContext}"/>
    /// </summary>
    /// <param name="owner">The <see cref="Scene"/> that owns this <see cref="DrawOperationManager{TGraphicsContext}"/></param>
    public DrawOperationManager(Scene owner)
        : base(owner.Game, "Rendering & Game Scene", "Draw Operations")
    {
        Owner = owner;
        Services = new(owner.Services);
    }

    #region Public Properties

    /// <summary>
    /// The <see cref="Scene"/> that owns this <see cref="DrawOperationManager{TGraphicsContext}"/>
    /// </summary>
    public Scene Owner { get; }

    #endregion

    #region Services and Dependency Injection

    /// <summary>
    /// The <see cref="ServiceCollection"/> for this <see cref="Node"/>
    /// </summary>
    /// <remarks>
    /// Services to this <see cref="Node"/> will cascade down from the root <see cref="Scene"/>, if not overriden.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown if this node is not attached to a root <see cref="Scene"/>, directly or indirectly</exception>
    public ServiceCollection Services { get; private set; }

    #endregion

    #region Public Methods

    #region Draw Operations

    /// <summary>
    /// Adds a new <see cref="DrawOperation{TGraphicsContext}"/> of type <typeparamref name="TDrawOp"/> into this <see cref="DrawOperationManager{TGraphicsContext}"/>
    /// </summary>
    /// <typeparam name="TDrawOp">The type of <see cref="DrawOperation{TGraphicsContext}"/> to instantiate and add</typeparam>
    public async ValueTask<TDrawOp> AddDrawOperation<TDrawOp>(TDrawOp dop, uint renderLevel = 0, GraphicsManager<TGraphicsContext>? graphicsManager = null) where TDrawOp : DrawOperation<TGraphicsContext>
    {
        await InternalAddDrawOperation(dop, graphicsManager, renderLevel);
        return dop;
    }

    /// <summary>
    /// Adds a new <see cref="DrawOperation{TGraphicsContext}"/> of type <typeparamref name="TDrawOp"/> into this <see cref="DrawOperationManager{TGraphicsContext}"/>
    /// </summary>
    /// <typeparam name="TDrawOp">The type of <see cref="DrawOperation{TGraphicsContext}"/> to instantiate and add</typeparam>
    public async ValueTask<TDrawOp> AddDrawOperation<TDrawOp>(uint renderLevel = 0, GraphicsManager<TGraphicsContext>? graphicsManager = null) where TDrawOp : DrawOperation<TGraphicsContext>, new()
    {
        var dop = new TDrawOp();
        await InternalAddDrawOperation(dop, graphicsManager, renderLevel);
        return dop;
    }

    /// <summary>
    /// Adds a new <see cref="DrawOperation{TGraphicsContext}"/> of type <typeparamref name="TDrawOp"/> into this <see cref="DrawOperationManager{TGraphicsContext}"/>
    /// </summary>
    /// <typeparam name="TDrawOp">The type of <see cref="DrawOperation{TGraphicsContext}"/> to instantiate and add</typeparam>
    public async ValueTask<TDrawOp> AddDrawOperation<TDrawOp>(Func<TDrawOp> factory, uint renderLevel = 0, GraphicsManager<TGraphicsContext>? graphicsManager = null) where TDrawOp : DrawOperation<TGraphicsContext>
    {
        var dop = factory();
        await InternalAddDrawOperation(dop, graphicsManager, renderLevel);
        return dop;
    }

    #endregion

    /// <summary>
    /// Adds an object representing the <see cref="Node"/>'s drawing operations into the <see cref="IDrawQueueEnqueuer{TGraphicsContext}"/>. This method will be called from the respective <see cref="GraphicsManager{TGraphicsContext}"/>'s thread
    /// </summary>
    /// <remarks>
    /// By default, this method will enqueue <paramref name="operation"/> onto <paramref name="queue"/> with a priority of <c>1</c>
    /// </remarks>
    /// <param name="queue">The queue associated with <paramref name="operation"/> into which to add the draw operations</param>
    /// <param name="operation">A specific registered <see cref="DrawOperation{TGraphicsContext}"/></param>
    public virtual void AddToDrawQueue(IDrawQueueEnqueuer<TGraphicsContext> queue, DrawOperation<TGraphicsContext> operation)
    {
        queue.Enqueue(operation, operation.PreferredPriority);
    }

    /// <summary>
    /// Gets the <see cref="DrawOperation{TGraphicsContext}"/>s that belong to <paramref name="graphicsManager"/>
    /// </summary>
    /// <param name="graphicsManager">The <see cref="GraphicsManager{TGraphicsContext}"/> owning the draw operations</param>
    /// <param name="renderLevel">The render level of the <see cref="DrawOperation{TGraphicsContext}"/>s to be gathered</param>
    public IEnumerable<DrawOperation<TGraphicsContext>> GetDrawOperations(GraphicsManager<TGraphicsContext> graphicsManager, uint renderLevel)
    {
        if (gm_dict.TryGetValue(graphicsManager, out var drawOperations))
        {
            lock (drawOperations)
                foreach (var dop in drawOperations.Enumerate(renderLevel))
                    yield return dop;
        }
        else
            yield break;
    }

    #endregion

    #region Internal

    private async ValueTask InternalAddDrawOperation(DrawOperation<TGraphicsContext> operation, GraphicsManager<TGraphicsContext>? graphicsManager, uint renderLevel)
    {
        graphicsManager
            ??= Game.MainGraphicsManager is GraphicsManager<TGraphicsContext> mgm
            ? mgm
            : throw new InvalidOperationException($"If the Game's MainGraphicsManager is not of type {typeof(GraphicsManager<TGraphicsContext>)}, then a non-null argument MUST be provided");

        GameMismatchException.ThrowIfMismatch(graphicsManager, operation, this);

        InternalLog?.Debug("Adding a new DrawOperation {objName}-{type}", operation.Name ?? "", operation.GetTypeName());

        graphicsManager.ThrowIfRenderLevelNotRegistered(renderLevel);

        operation.SetOwner(this);
        operation.AssignManager(graphicsManager);
        try
        {
            AddingDrawOperation(operation);
            operation.ThrowIfDisposed();
            var dol = gm_dict.GetOrAdd(graphicsManager, gm => new DrawOperationList<TGraphicsContext>(graphicsManager));
            lock (dol)
                dol.Add(operation, renderLevel);
            operation.AboutToDispose += Operation_AboutToDispose;
            await operation.CreateResourcesAsync();
        }
        catch
        {
            operation.Dispose();
            throw;
        }
    }

    private void Operation_AboutToDispose(GameObject sender, TimeSpan timestamp)
    {
        Debug.Assert(sender is DrawOperation<TGraphicsContext>, "Sender GameObject was not a DrawOperation");
        var dop = (sender as DrawOperation<TGraphicsContext>)!;
        Debug.Assert(dop.Manager is not null, "DrawOperation's Manager is unexpectedly null");
        if (gm_dict.TryGetValue(dop.Manager, out var list) is false)
            Debug.Fail($"There is no DrawOperationList for manager {dop.Manager}");
        lock (list)
            list.Remove(dop);
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
/// Represents a <see cref="DrawOperationManager{TGraphicsContext}"/> that accepts a <see cref="DrawQueueSelector"/> delegate method to act in place of inheriting and overriding <see cref="AddToDrawQueue(IDrawQueueEnqueuer{TGraphicsContext}, DrawOperation{TGraphicsContext})"/>
/// </summary>
public sealed class DrawOperationManagerDrawQueueDelegate<TGraphicsContext> : DrawOperationManager<TGraphicsContext>
    where TGraphicsContext : GraphicsContext<TGraphicsContext>
{
    /// <summary>
    /// Represents a method that can add <paramref name="operation"/> appropriately into <paramref name="queue"/>
    /// </summary>
    /// <param name="queue">The <see cref="IDrawQueueEnqueuer{TGraphicsContext}"/> into which to add <paramref name="operation"/></param>
    /// <param name="operation">The operation in question</param>
    public delegate void DrawQueueSelector(IDrawQueueEnqueuer<TGraphicsContext> queue, DrawOperation<TGraphicsContext> operation);

    private readonly DrawQueueSelector _drawQueueSelector;

    /// <inheritdoc/>
    public DrawOperationManagerDrawQueueDelegate(Scene owner, DrawQueueSelector drawQueueSelector) : base(owner)
    {
        ArgumentNullException.ThrowIfNull(drawQueueSelector);
        _drawQueueSelector = drawQueueSelector;
    }

    /// <inheritdoc/>
    public override void AddToDrawQueue(IDrawQueueEnqueuer<TGraphicsContext> queue, DrawOperation<TGraphicsContext> operation)
        => _drawQueueSelector.Invoke(queue, operation);
}
