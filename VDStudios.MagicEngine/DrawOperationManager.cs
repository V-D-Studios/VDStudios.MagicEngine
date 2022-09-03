﻿namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a list of <see cref="DrawOperation"/> objects belonging to a <see cref="IDrawableNode"/> and their general behaviour
/// </summary>
public class DrawOperationManager : GameObject
{
    /// <summary>
    /// Instantiates a new object of type <see cref="DrawOperationManager"/>
    /// </summary>
    /// <param name="owner">The <see cref="IDrawableNode"/> that owns this <see cref="DrawOperationManager"/></param>
    /// <param name="graphicsManagerSelector">The method that this <see cref="DrawOperationManager"/> will use to select an appropriate <see cref="GraphicsManager"/> to register a new <see cref="DrawOperation"/> onto</param>
    public DrawOperationManager(IDrawableNode owner, DrawOperationGraphicsManagerSelector? graphicsManagerSelector = null)
        : this(owner, graphicsManagerSelector, "Draw Operations") { }

    /// <summary>
    /// Instantiates a new object of type <see cref="DrawOperationManager"/>
    /// </summary>
    /// <param name="owner">The <see cref="IDrawableNode"/> that owns this <see cref="DrawOperationManager"/></param>
    /// <param name="graphicsManagerSelector">The method that this <see cref="DrawOperationManager"/> will use to select an appropriate <see cref="GraphicsManager"/> to register a new <see cref="DrawOperation"/> onto</param>
    protected DrawOperationManager(IDrawableNode owner, DrawOperationGraphicsManagerSelector? graphicsManagerSelector, string area) : base("Rendering & Game Scene", area)
    {
        if (owner is not Node)
            throw new InvalidOperationException($"The owner of a DrawOperationManager must be a node");
        Owner = owner;
        GraphicsManagerSelector = graphicsManagerSelector;
    }

    #region Public Properties

    /// <summary>
    /// Represents the method that this <see cref="DrawOperationManager"/> will use to select an appropriate <see cref="GraphicsManager"/> to register a new <see cref="DrawOperation"/> onto
    /// </summary>
    public DrawOperationGraphicsManagerSelector? GraphicsManagerSelector { get; set; }

    /// <summary>
    /// The <see cref="IDrawableNode"/> that owns this <see cref="DrawOperationManager"/>
    /// </summary>
    public IDrawableNode Owner { get; }

    /// <summary>
    /// Represents the <see cref="DrawOperation"/>s that belong to this <see cref="DrawOperationManager"/>
    /// </summary>
    /// <remarks>
    /// To remove a <see cref="DrawOperation"/>, dispose it
    /// </remarks>
    public DrawOperationList DrawOperations { get; } = new();

    /// <summary>
    /// Tells the engine whether there are <see cref="DrawOperation"/>s waiting to be registered. This will be set to <c>true</c> when a new <see cref="DrawOperation"/> is added to <see cref="DrawOperations"/>
    /// </summary>
    public bool HasPendingRegistrations { get; private set; }

    #endregion

    #region Public Methods

    /// <summary>
    /// Passes <paramref name="parameters"/> down through <see cref="Owner"/>'s children, to replace the <see cref="DataDependencySource{T}"/> they reference
    /// </summary>
    public void CascadeThroughNode(DrawParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        InternalLog?.Debug("Cascading {typeName} through the owning Node {objName}'s children", nameof(DrawParameters), Owner.Name);
        cascadedParameters = parameters;
        ProcessNewDrawData(parameters);
        foreach (var child in ((Node)Owner).Children)
            if (child is IDrawableNode dn) 
                dn.DrawOperationManager.CascadeThroughNode(parameters); 
    }
    internal DrawParameters? cascadedParameters;

    /// <summary>
    /// Adds a new <see cref="DrawOperation"/> of type <typeparamref name="TDrawOp"/> into this <see cref="DrawOperationManager"/>
    /// </summary>
    /// <typeparam name="TDrawOp">The type of <see cref="DrawOperation"/> to instantiate and add</typeparam>
    public TDrawOp AddDrawOperation<TDrawOp>(TDrawOp dop) where TDrawOp : DrawOperation
    {
        InternalAddDrawOperation(dop);
        return dop;
    }

    /// <summary>
    /// Adds a new <see cref="DrawOperation"/> of type <typeparamref name="TDrawOp"/> into this <see cref="DrawOperationManager"/>
    /// </summary>
    /// <typeparam name="TDrawOp">The type of <see cref="DrawOperation"/> to instantiate and add</typeparam>
    public TDrawOp AddDrawOperation<TDrawOp>() where TDrawOp : DrawOperation, new()
    {
        var dop = new TDrawOp();
        InternalAddDrawOperation(dop);
        return dop;
    }

    /// <summary>
    /// Adds a new <see cref="DrawOperation"/> of type <typeparamref name="TDrawOp"/> into this <see cref="DrawOperationManager"/>
    /// </summary>
    /// <typeparam name="TDrawOp">The type of <see cref="DrawOperation"/> to instantiate and add</typeparam>
    public TDrawOp AddDrawOperation<TDrawOp>(Func<TDrawOp> factory) where TDrawOp : DrawOperation
    {
        var dop = factory();
        InternalAddDrawOperation(dop);
        return dop;
    }

    /// <summary>
    /// Adds an object representing the <see cref="Node"/>'s drawing operations into the <see cref="IDrawQueue{TOp}"/>. This method will be called from the respective <see cref="GraphicsManager"/>'s thread
    /// </summary>
    /// <remarks>
    /// By default, this method will enqueue <paramref name="operation"/> onto <paramref name="queue"/> with a priority of <c>1</c>
    /// </remarks>
    /// <param name="queue">The queue associated with <paramref name="operation"/> into which to add the draw operations</param>
    /// <param name="operation">A specific registered <see cref="DrawOperation"/></param>
    public virtual void AddToDrawQueue(IDrawQueue<DrawOperation> queue, DrawOperation operation)
    {
        queue.Enqueue(operation, operation.PreferredPriority);
    }

    #endregion

    #region Internal

    internal void ProcessNewDrawData(DrawParameters parameters)
    {
        foreach (var dop in DrawOperations)
            UpdateOperationDrawParameters(parameters, dop);
    }

    /// <summary>
    /// This method will make no attempts at maintaining concurrency. The caller is responsible for waiting on <see cref="DrawOperationList.RegistrationSync"/> and releasing
    /// </summary>
    /// <remarks>
    /// <see cref="HasPendingRegistrations"/> is set to false before this method returns
    /// </remarks>
    internal async Task RegisterDrawOperations(GraphicsManager main, IReadOnlyList<GraphicsManager> allManagers)
    {
        InternalLog?.Debug("Registering {count} DrawOperations", DrawOperations.RegistrationBuffer.Count);
        foreach (var dop in DrawOperations.RegistrationBuffer)
        {
            InternalLog?.Verbose("Registering DrawOperation {objName}-{type}", dop.Name ?? "", dop.GetTypeName());
            GraphicsManager manager = GraphicsManagerSelector is DrawOperationGraphicsManagerSelector selector ? selector(main, allManagers, dop, Owner, this) : main;
            await manager.RegisterOperation(dop);
        }
        HasPendingRegistrations = false;
    }

    private void InternalAddDrawOperation(DrawOperation operation)
    {
        InternalLog?.Debug("Adding a new DrawOperation {objName}-{type}", operation.Name ?? "", operation.GetTypeName());
        operation.SetOwner(this);
        try
        {
            AddingDrawOperation(operation);
            if (operation.disposedValue)
                throw new ObjectDisposedException(nameof(operation));
            DrawOperations.Add(operation);
            DrawOperations.RegistrationSync.Wait();
            try
            {
                DrawOperations.RegistrationBuffer.Add(operation);
                HasPendingRegistrations = true;
            }
            finally
            {
                DrawOperations.RegistrationSync.Release();
            }
            operation.AboutToDispose += Operation_AboutToDispose;
        }
        catch
        {
            operation.Dispose();
            throw;
        }
    }

    private void Operation_AboutToDispose(DrawOperation sender, TimeSpan timestamp)
    {
        DrawOperations.Remove(sender);
    }

    #endregion

    #region Reaction Methods

    /// <summary>
    /// This method is called automatically when this <see cref="DrawOperationManager"/> is receiving new <see cref="DrawTransformation"/> for its <see cref="DrawOperation"/>s
    /// </summary>
    /// <param name="drawParameters">The parameters received</param>
    /// <param name="operation">The operation to assign the parameters into</param>
    protected virtual void UpdateOperationDrawParameters(DrawParameters drawParameters, DrawOperation operation)
    {
        operation.ReferenceParameters = drawParameters;
    }

    /// <summary>
    /// This method is called automatically when a new <see cref="DrawOperation"/> is being added onto this <see cref="DrawOperationManager"/>
    /// </summary>
    /// <param name="operation">The operation being added</param>
    protected virtual void AddingDrawOperation(DrawOperation operation) { }

    #endregion
}

/// <summary>
/// Represents a <see cref="DrawOperationManager"/> that accepts a <see cref="DrawQueueSelector"/> delegate method to act in place of inheriting and overriding <see cref="DrawOperationManager.AddToDrawQueue(IDrawQueue{DrawOperation}, DrawOperation)"/>
/// </summary>
public sealed class DrawOperationManagerDrawQueueDelegate : DrawOperationManager
{
    /// <summary>
    /// Represents a method that can add <paramref name="operation"/> appropriately into <paramref name="queue"/>
    /// </summary>
    /// <param name="queue">The <see cref="IDrawQueue{T}"/> into which to add <paramref name="operation"/></param>
    /// <param name="operation">The operation in question</param>
    public delegate void DrawQueueSelector(IDrawQueue<DrawOperation> queue, DrawOperation operation);

    private readonly DrawQueueSelector _drawQueueSelector;

    /// <inheritdoc/>
    public DrawOperationManagerDrawQueueDelegate(IDrawableNode owner, DrawQueueSelector drawQueueSelector, DrawOperationGraphicsManagerSelector? graphicsManagerSelector = null) : base(owner, graphicsManagerSelector)
    {
        ArgumentNullException.ThrowIfNull(drawQueueSelector);
        _drawQueueSelector = drawQueueSelector;
    }

    /// <inheritdoc/>
    public override void AddToDrawQueue(IDrawQueue<DrawOperation> queue, DrawOperation operation)
        => _drawQueueSelector.Invoke(queue, operation);
}