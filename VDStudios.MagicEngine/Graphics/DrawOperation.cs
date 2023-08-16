using System.Diagnostics;
using System.Numerics;

namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// Represents an operation that is ready to be drawn. This object is automatically disposed of when deregistered, and must have a reference to it held by the owning <see cref="IDrawableNode{TGraphicsContext}"/>. Otherwise, this object will be collected and disposed of
/// </summary>
/// <remarks>
/// Try to keep an object created from this class cached somewhere in a node, as they incur a number of allocations that should be avoided in a HotPath like the rendering sequence
/// </remarks>
public abstract class DrawOperation<TGraphicsContext> : GraphicsObject<TGraphicsContext>, IDisposable
    where TGraphicsContext : GraphicsContext<TGraphicsContext>
{
    private readonly SemaphoreSlim sync = new(1, 1);

    /// <summary>
    /// Instances a new object of type <see cref="DrawOperation{TGraphicsContext}"/>
    /// </summary>
    public DrawOperation() : base("Drawing")
    {
    }

    #region Transformation

    /// <summary>
    /// The transformation matrix that represents the current transformation properties in this <see cref="DrawOperation{TGraphicsContext}"/>
    /// </summary>
    /// <remarks>
    /// This transformation can be used to represent the current world properties of the drawing operation, for example, it's position and rotation in relation to the world itself
    /// </remarks>
    protected Matrix4x4 VertexTransformation
    {
        get
        {
            if (vertrans is not Matrix4x4 t)
            {
                var translation = Translation;
                var scl = Scale;
                var (cpxx, cpxy, cpxz, rotx) = RotationX;
                var (cpyx, cpyy, cpyz, roty) = RotationY;
                var (cpzx, cpzy, cpzz, rotz) = RotationZ;
                vertrans = t =
                    Matrix4x4.CreateTranslation(translation) *
                    Matrix4x4.CreateScale(scl) *
                    Matrix4x4.CreateRotationX(rotx, new(cpxx, cpxy, cpxz)) *
                    Matrix4x4.CreateRotationY(roty, new(cpyx, cpyy, cpyz)) *
                    Matrix4x4.CreateRotationZ(rotz, new(cpzx, cpzy, cpzz));
            }
            return t;
        }
    }
    private Matrix4x4? vertrans = Matrix4x4.Identity;

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
        }
    }
    private ColorTransformation colTrans;

    /// <summary>
    /// Adjusts the transformation parameters and calculates the appropriate transformation matrix for this <see cref="DrawOperation{TGraphicsContext}"/>
    /// </summary>
    /// <remarks>
    /// Parameters that are not specified (i.e. left as <c>null</c>) will default to the current transformation setting in this <see cref="DrawOperation{TGraphicsContext}"/>
    /// </remarks>
    /// <param name="translation">The translation in worldspace for this operation</param>
    /// <param name="scale">The scale in worldspace for this operation</param>
    /// <param name="rotX">The rotation along the x axis in worldspace for this operation</param>
    /// <param name="rotY">The rotation along the y axis in worldspace for this operation</param>
    /// <param name="rotZ">The rotation along the z axis in worldspace for this operation</param>
    public void Transform(Vector3? translation = null, Vector3? scale = null, Vector4? rotX = null, Vector4? rotY = null, Vector4? rotZ = null)
    {
        Translation = translation ?? Translation;
        Scale = scale ?? Scale;
        RotationX = rotX ?? RotationX;
        RotationY = rotY ?? RotationY;
        RotationZ = rotZ ?? RotationZ;
        NotifyPendingGPUUpdate();
        vertrans = null;
    }

    /// <summary>
    /// Describes the current translation setting of this <see cref="DrawOperation{TGraphicsContext}"/>
    /// </summary>
    public Vector3 Translation { get; private set; }

    /// <summary>
    /// Describes the current scale setting of this <see cref="DrawOperation{TGraphicsContext}"/>
    /// </summary>
    public Vector3 Scale { get; private set; } = Vector3.One;

    /// <summary>
    /// Describes the current rotation setting along the x axis of this <see cref="DrawOperation{TGraphicsContext}"/>
    /// </summary>
    /// <remarks>
    /// Where <see cref="Vector4.X"/>, <see cref="Vector4.Y"/> and <see cref="Vector4.Z"/> are the center point, and <see cref="Vector4.W"/> is the actual rotation in <c>radians</c>
    /// </remarks>
    public Vector4 RotationX { get; private set; }

    /// <summary>
    /// Describes the current rotation setting along the y axis of this <see cref="DrawOperation{TGraphicsContext}"/>
    /// </summary>
    /// <remarks>
    /// Where <see cref="Vector4.X"/>, <see cref="Vector4.Y"/> and <see cref="Vector4.Z"/> are the center point, and <see cref="Vector4.W"/> is the actual rotation in <c>radians</c>
    /// </remarks>
    public Vector4 RotationY { get; private set; }

    /// <summary>
    /// Describes the current rotation setting along the z axis of this <see cref="DrawOperation{TGraphicsContext}"/>
    /// </summary>
    /// <remarks>
    /// Where <see cref="Vector4.X"/>, <see cref="Vector4.Y"/> and <see cref="Vector4.Z"/> are the center point, and <see cref="Vector4.W"/> is the actual rotation in <c>radians</c>
    /// </remarks>
    public Vector4 RotationZ { get; private set; }

    #endregion

    /// <summary>
    /// Represents this <see cref="DrawOperation{TGraphicsContext}"/>'s preferred priority. May or may not be honored depending on the <see cref="DrawOperationManager{TGraphicsContext}"/>
    /// </summary>
    public float PreferredPriority { get; set; }

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

    #region Registration

    #region Internal

    internal void Register(GraphicsManager<TGraphicsContext> manager)
    {
        ThrowIfDisposed();
        VerifyManager(manager);

        Registering(manager);

        Registered();
        NotifyIsReady();
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

    #region Internal

    private bool pendingGpuUpdate = true;

    /// <summary>
    /// Flags this <see cref="DrawOperation{TGraphicsContext}"/> as needing to update GPU data before the next <see cref="Draw(TimeSpan, TGraphicsContext)"/> call
    /// </summary>
    /// <remarks>
    /// Multiple calls to this method will not result in <see cref="UpdateGPUState(TGraphicsContext)"/> being called multiple times
    /// </remarks>
    protected void NotifyPendingGPUUpdate() => pendingGpuUpdate = true;

    internal async ValueTask InternalDraw(TimeSpan delta, TGraphicsContext context)
    {
        Debug.Assert(context is not null, "The GraphicsContext is unexpectedly null");
        ThrowIfDisposed();
        sync.Wait();
        try
        {
            if (pendingGpuUpdate)
            {
                pendingGpuUpdate = false;
                await UpdateGPUState(context);
            }
            await Draw(delta, context).ConfigureAwait(false);
        }
        finally
        {
            sync.Release();
        }
    }

    #endregion

    #region Reaction Methods

    /// <summary>
    /// Creates the necessary resource sets for this <see cref="DrawOperation{TGraphicsContext}"/>
    /// </summary>
    protected abstract ValueTask CreateResources(TGraphicsContext context);

    /// <summary>
    /// The method that will be used to draw the component
    /// </summary>
    /// <param name="delta">The amount of time that has passed since the last draw sequence</param>
    /// <param name="context">The <typeparamref name="TGraphicsContext"/> for this <see cref="DrawOperation{TGraphicsContext}"/></param>
    protected abstract ValueTask Draw(TimeSpan delta, TGraphicsContext context);

    /// <summary>
    /// This method is called automatically when this <see cref="DrawOperation{TGraphicsContext}"/> is going to be drawn for the first time, and after <see cref="NotifyPendingGPUUpdate"/> is called. Whenever applicable, this method ALWAYS goes before <see cref="Draw(TimeSpan, TGraphicsContext)"/>
    /// </summary>
    /// <param name="context">The <typeparamref name="TGraphicsContext"/> for this <see cref="DrawOperation{TGraphicsContext}"/></param>
    protected abstract ValueTask UpdateGPUState(TGraphicsContext context);

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
