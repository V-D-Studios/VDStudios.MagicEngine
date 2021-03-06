namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a list of <see cref="DrawOperation"/> objects belonging to a <see cref="IDrawableNode"/> and their general behaviour
/// </summary>
public class DrawOperationManager
{
    /// <summary>
    /// Instantiates a new object of type <see cref="DrawOperationManager"/>
    /// </summary>
    /// <param name="owner">The <see cref="IDrawableNode"/> that owns this <see cref="DrawOperationManager"/></param>
    /// <param name="graphicsManagerSelector">The method that this <see cref="DrawOperationManager"/> will use to select an appropriate <see cref="GraphicsManager"/> to register a new <see cref="DrawOperation"/> onto</param>
    public DrawOperationManager(IDrawableNode owner, DrawOperationGraphicsManagerSelector? graphicsManagerSelector = null)
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
    public void CascadeThroughNode(DataDependency<DrawParameters> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        cascadedParameters = parameters;
        ProcessNewDrawData(parameters);
        foreach (var child in ((Node)Owner).Children)
            if (child is IDrawableNode dn) 
                dn.DrawOperationManager.CascadeThroughNode(parameters); 
    }
    internal DataDependency<DrawParameters>? cascadedParameters;

    /// <summary>
    /// Adds a new <see cref="DrawOperation"/> of type <typeparamref name="TDrawOp"/> into this <see cref="DrawOperationManager"/>
    /// </summary>
    /// <typeparam name="TDrawOp">The type of <see cref="DrawOperation"/> to instantiate and add</typeparam>
    public TDrawOp AddDrawOperation<TDrawOp>() where TDrawOp : DrawOperation, new()
    {
        var dop = new TDrawOp();
        dop.SetOwner(this);
        AddDrawOperation(dop);
        return dop;
    }

    /// <summary>
    /// Adds a new <see cref="DrawOperation"/> of type <typeparamref name="TDrawOp"/> into this <see cref="DrawOperationManager"/>
    /// </summary>
    /// <typeparam name="TDrawOp">The type of <see cref="DrawOperation"/> to instantiate and add</typeparam>
    public TDrawOp AddDrawOperation<TDrawOp>(Func<TDrawOp> factory) where TDrawOp : DrawOperation
    {
        var dop = factory();
        dop.SetOwner(this);
        AddDrawOperation(dop);
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
        queue.Enqueue(operation, 1);
    }

    #endregion

    #region Internal

    internal void ProcessNewDrawData(DataDependency<DrawParameters> parameters)
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
        foreach (var dop in DrawOperations.RegistrationBuffer)
        {
            GraphicsManager manager = GraphicsManagerSelector is DrawOperationGraphicsManagerSelector selector ? selector(main, allManagers, dop, Owner, this) : main;
            await manager.RegisterOperation(dop);
        }
        HasPendingRegistrations = false;
    }

    private void AddDrawOperation(DrawOperation operation)
    {
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
    /// This method is called automatically when this <see cref="DrawOperationManager"/> is receiving new <see cref="DrawParameters"/> for its <see cref="DrawOperation"/>s
    /// </summary>
    /// <param name="drawParameters">The parameters received</param>
    /// <param name="operation">The operation to assign the parameters into</param>
    protected virtual void UpdateOperationDrawParameters(DataDependency<DrawParameters> drawParameters, DrawOperation operation)
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