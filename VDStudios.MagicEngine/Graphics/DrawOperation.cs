﻿using System.Diagnostics;
using System.Numerics;

namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// Represents an operation that is ready to be drawn. This object is automatically deregistered when disposed of
/// </summary>
/// <remarks>
/// Try to keep an object created from this class cached somewhere in a node, as they incur a number of allocations that should be avoided in a HotPath like the rendering sequence
/// </remarks>
public abstract class DrawOperation<TGraphicsContext> : GraphicsObject<TGraphicsContext>
    where TGraphicsContext : GraphicsContext<TGraphicsContext>
{
    #region Transformation

    /// <summary>
    /// This <see cref="DrawOperation{TGraphicsContext}"/>'s current transformation state
    /// </summary>
    public TransformationState TransformationState { get; }

    /// <summary>
    /// Creates a new <see cref="TransformationState"/> for this <see cref="DrawOperation{TGraphicsContext}"/>
    /// </summary>
    /// <remarks>
    /// This method is called only once during construction
    /// </remarks>
    protected virtual TransformationState CreateTransformationState()
        => new();

    /// <summary>
    /// The transformation matrix that represents the current transformation properties in this <see cref="DrawOperation{TGraphicsContext}"/>
    /// </summary>
    public ColorTransformation ColorTransformation
    {
        get => colTrans;
        set
        {
            colTrans = value;
            NotifyPendingGPUUpdate();
            ColorTransformationChanged?.Invoke(this, Game.TotalTime);
        }
    }
    private ColorTransformation colTrans;

    /// <summary>
    /// Fired when <see cref="ColorTransformation"/> changes
    /// </summary>
    public event DrawOperationEvent<TGraphicsContext>? ColorTransformationChanged;

    /// <summary>
    /// Fired when <see cref="TransformationState"/> <see cref="TransformationState.VertexTransformation"/> changes
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="TransformationState.VertexTransformation"/>, this event belongs specifically to <see cref="DrawOperation{TGraphicsContext}"/>
    /// </remarks>
    public event DrawOperationEvent<TGraphicsContext>? VertexTransformationChanged;

    /// <summary>
    /// Fired when <see cref="TransformationState"/> <see cref="TransformationState.ScaleTransformation"/> changes
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="TransformationState.ScaleTransformation"/>, this event belongs specifically to <see cref="DrawOperation{TGraphicsContext}"/>
    /// </remarks>
    public event DrawOperationEvent<TGraphicsContext>? ScaleTransformationChanged;

    #endregion

    #region Construction

    private readonly SemaphoreSlim sync = new(1, 1);

    /// <summary>
    /// Instances a new object of type <see cref="DrawOperation{TGraphicsContext}"/>
    /// </summary>
    public DrawOperation(Game game) : base(game, "Drawing")
    {
        TransformationState = CreateTransformationState();

        TransformationState.ScaleTransformationChanged += t =>
        {
            NotifyPendingGPUUpdate();
            ScaleTransformationChanged?.Invoke(this, Game.TotalTime);
        };

        TransformationState.VertexTransformationChanged += t =>
        {
            NotifyPendingGPUUpdate();
            VertexTransformationChanged?.Invoke(this, Game.TotalTime);
        };
    }

    /// <summary>
    /// The owner <see cref="DrawOperationManager{TGraphicsContext}"/> of this <see cref="DrawOperation{TGraphicsContext}"/>
    /// </summary>
    /// <remarks>
    /// Will throw if this <see cref="DrawOperation{TGraphicsContext}"/> is not registered
    /// </remarks>
    public DrawOperationManager<TGraphicsContext> Owner => _owner ?? throw new InvalidOperationException("Cannot query the Owner of an unregistered DrawOperation{TGraphicsContext}");
    private DrawOperationManager<TGraphicsContext>? _owner;
    internal void SetOwner(DrawOperationManager<TGraphicsContext> owner)
    {
        lock (sync)
        {
            if (_owner is not null)
                throw new InvalidOperationException("This DrawOperation already has an owner.");
            _owner = owner;
        }
    }

    #endregion

    #region Registration

    #region Internal

    internal void Register(GraphicsManager<TGraphicsContext> manager)
    {
        ThrowIfDisposed();
        VerifyManager(manager);

        Registering(manager);

        Registered();
    }

    #endregion

    #region Reaction Methods

    /// <summary>
    /// This method is called automatically when this <see cref="DrawOperation{TGraphicsContext}"/> is being registered onto <paramref name="manager"/>
    /// </summary>
    /// <param name="manager">The <see cref="GraphicsManager{TGraphicsContext}"/> this <see cref="DrawOperation{TGraphicsContext}"/> is being registered onto</param>
    protected virtual void Registering(GraphicsManager<TGraphicsContext> manager) { }

    /// <summary>
    /// This method is called automatically when this <see cref="DrawOperation{TGraphicsContext}"/> has been registered
    /// </summary>
    protected virtual void Registered() { }

    #endregion

    #endregion

    #region Drawing

    /// <summary>
    /// Whether or not this <see cref="DrawOperation{TGraphicsContext}"/> is active 
    /// </summary>
    /// <remarks>
    /// If <see langword="false"/>, then Drawing and resource updating for this operation is skipped
    /// </remarks>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Fired when <see cref="IsActive"/> changes
    /// </summary>

    public event DrawOperationEvent<TGraphicsContext>? IsActiveChanged;

    /// <summary>
    /// Represents this <see cref="DrawOperation{TGraphicsContext}"/>'s preferred priority. May or may not be honored depending on the <see cref="DrawOperationManager{TGraphicsContext}"/>
    /// </summary>
    public float PreferredPriority { get; set; }

    #region Internal

    private bool gpuResourcesCreated = false;
    private bool pendingGpuUpdate = true;

    /// <summary>
    /// Flags this <see cref="DrawOperation{TGraphicsContext}"/> as needing to update GPU data before the next <see cref="Draw(TimeSpan, TGraphicsContext, RenderTarget{TGraphicsContext})"/> call
    /// </summary>
    /// <remarks>
    /// Multiple calls to this method will not result in <see cref="UpdateGPUState(TGraphicsContext)"/> being called multiple times
    /// </remarks>
    protected void NotifyPendingGPUUpdate() => pendingGpuUpdate = true;

    /// <summary>
    /// Forces this <see cref="DrawOperation{TGraphicsContext}"/> to update its GPU state immediately
    /// </summary>
    /// <remarks>
    /// Automatically unflags this <see cref="DrawOperation{TGraphicsContext}"/> from needing to be updated again
    /// </remarks>
    /// <param name="context">The <typeparamref name="TGraphicsContext"/> this <see cref="DrawOperation{TGraphicsContext}"/> uses</param>
    protected void ForceGPUUpdate(TGraphicsContext context)
    {
        pendingGpuUpdate = false;
        UpdateGPUState(context);
    }

    internal void InternalDraw(TimeSpan delta, TGraphicsContext context, RenderTarget<TGraphicsContext> target)
    {
        Debug.Assert(context is not null, "The GraphicsContext is unexpectedly null");
        ThrowIfDisposed();
        ThrowIfExternalExceptionPresent();
        if (IsActive is false) return;

        sync.Wait();
        try
        {
            if (gpuResourcesCreated is false)
            {
                gpuResourcesCreated = true;
                CreateGPUResources(context);
            } 

            if (pendingGpuUpdate)
                ForceGPUUpdate(context);
            Draw(delta, context, target);
        }
        finally
        {
            sync.Release();
        }
    }

    #endregion

    #region Reaction Methods

    /// <summary>
    /// Creates the necessary resources for this <see cref="DrawOperation{TGraphicsContext}"/> that can be safely loaded or created from the CPU or a background thread
    /// </summary>
    /// <remarks>
    /// This method is only called once, during registration
    /// </remarks>
    protected internal abstract ValueTask CreateResourcesAsync();
#warning Consider adding UpdateCPUState as well, for asynchronous resource reloading

    /// <summary>
    /// Creates the necessary resource sets for this <see cref="DrawOperation{TGraphicsContext}"/>
    /// </summary>
    protected abstract void CreateGPUResources(TGraphicsContext context);

    /// <summary>
    /// The method that will be used to draw the component
    /// </summary>
    /// <param name="delta">The amount of time that has passed since the last draw sequence</param>
    /// <param name="context">The <typeparamref name="TGraphicsContext"/> for this <see cref="DrawOperation{TGraphicsContext}"/></param>
    /// <param name="target">The <see cref="RenderTarget{TGraphicsContext}"/> for this Draw operation</param>
    protected abstract void Draw(TimeSpan delta, TGraphicsContext context, RenderTarget<TGraphicsContext> target);

    /// <summary>
    /// This method is called automatically when this <see cref="DrawOperation{TGraphicsContext}"/> is going to be drawn for the first time, and after <see cref="NotifyPendingGPUUpdate"/> is called. Whenever applicable, this method ALWAYS goes before <see cref="Draw(TimeSpan, TGraphicsContext, RenderTarget{TGraphicsContext})"/>
    /// </summary>
    /// <param name="context">The <typeparamref name="TGraphicsContext"/> for this <see cref="DrawOperation{TGraphicsContext}"/></param>
    protected abstract void UpdateGPUState(TGraphicsContext context);

    #endregion

    #endregion

    #region Disposal

    internal override void InternalDispose(bool disposing)
    {
        sync.Wait();
        try
        {
            if (Manager is GraphicsManager<TGraphicsContext> manager)
            {
                var @lock = manager.LockManagerDrawing();
                try
                {
                    Dispose(disposing);
                }
                finally
                {
                    _owner = null!;
                    @lock.Dispose();
                }
            }
        }
        finally
        {
            sync.Release();
        }
    }

    #endregion
}
