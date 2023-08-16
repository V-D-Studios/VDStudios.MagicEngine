using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using VDStudios.MagicEngine.Graphics.Veldrid.DrawLibrary;
using VDStudios.MagicEngine.Utility;
using Veldrid;

namespace VDStudios.MagicEngine.Graphics.Veldrid.Graphics;

/// <summary>
/// Represents an operation that is ready to be drawn. This object is automatically disposed of when deregistered, and must have a reference to it held by the owning <see cref="IDrawableNode"/>. Otherwise, this object will be collected and disposed of
/// </summary>
/// <remarks>
/// Try to keep an object created from this class cached somewhere in a node, as they incur a number of allocations that should be avoided in a HotPath like the rendering sequence
/// </remarks>
public abstract class DrawOperation : GraphicsObject, IDisposable
{
    private readonly SemaphoreSlim sync = new(1, 1);

    internal GraphicsDevice? Device;

    private static ResourceLayoutBuilder ResourceLayoutFactory() => ResourceLayoutBuilderPool.Rent();
    private static void ResourceLayoutCleaner(ResourceLayoutBuilder resource) => ResourceLayoutBuilderPool.Return(resource);

    private static readonly ObjectPool<ResourceSetBuilder> ResourceSetBuilderPool = new(x => new ResourceSetBuilder(ResourceLayoutFactory, ResourceLayoutCleaner), x => x.Clear(), 2, 5);
    private static readonly ObjectPool<ResourceLayoutBuilder> ResourceLayoutBuilderPool = new(x => new ResourceLayoutBuilder(), x => x.Clear(), 2, 5);

    /// <summary>
    /// Instances a new object of type <see cref="DrawOperation"/>
    /// </summary>
    public DrawOperation() : base("Drawing")
    {
    }

    #region Transformation

    /// <summary>
    /// The <see cref="ResourceLayout"/> used to describe the layout for <see cref="DrawOperation"/> color and vertex transformations
    /// </summary>
    /// <remarks>
    /// This layout is static across <see cref="GraphicsManager"/>s
    /// </remarks>
    public ResourceLayout TransformationLayout => Manager!.DrawOpTransLayout;

    /// <summary>
    /// The <see cref="ResourceSet"/> used to bind the internal buffer 
    /// </summary>
    protected ResourceSet TransformationSet { get; private set; }

    private DeviceBuffer TransformationBuffer { get; set; }

    /// <summary>
    /// Updates the TransformationBuffer with data from <see cref="VertexTransformation"/> if necessary
    /// </summary>
    protected void UpdateTransformationBuffer(CommandList commandList)
    {
        if (PendingVertexTransformationUpdate)
        {
            var tr = VertexTransformation;
            PendingVertexTransformationUpdate = false;
            commandList.UpdateBuffer(TransformationBuffer, VertexTransformationOffset, ref tr);
        }
        if (PendingColorTransformationUpdate)
        {
            var tr = ColorTransformation;
            PendingColorTransformationUpdate = false;
            commandList.UpdateBuffer(TransformationBuffer, ColorTransformationOffset, ref tr);
        }
    }

    /// <summary>
    /// The transformation matrix that represents the current transformation properties in this <see cref="DrawOperation"/>
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
    private bool PendingVertexTransformationUpdate = false;
    private const uint VertexTransformationOffset = 0;

    /// <summary>
    /// The transformation matrix that represents the current transformation properties in this <see cref="DrawOperation"/>
    /// </summary>
    public ColorTransformation ColorTransformation
    {
        get => colTrans;
        set
        {
            colTrans = value;
            PendingColorTransformationUpdate = true;
        }
    }
    private ColorTransformation colTrans;
    private bool PendingColorTransformationUpdate = false;
    private readonly uint ColorTransformationOffset = (uint)Unsafe.SizeOf<Matrix4x4>();

    /// <summary>
    /// Adjusts the transformation parameters and calculates the appropriate transformation matrix for this <see cref="DrawOperation"/>
    /// </summary>
    /// <remarks>
    /// Parameters that are not specified (i.e. left as <c>null</c>) will default to the current transformation setting in this <see cref="DrawOperation"/>
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
        PendingVertexTransformationUpdate = true;
        NotifyPendingGPUUpdate();
        vertrans = null;
    }

    /// <summary>
    /// Describes the current translation setting of this <see cref="DrawOperation"/>
    /// </summary>
    public Vector3 Translation { get; private set; }

    /// <summary>
    /// Describes the current scale setting of this <see cref="DrawOperation"/>
    /// </summary>
    public Vector3 Scale { get; private set; } = Vector3.One;

    /// <summary>
    /// Describes the current rotation setting along the x axis of this <see cref="DrawOperation"/>
    /// </summary>
    /// <remarks>
    /// Where <see cref="Vector4.X"/>, <see cref="Vector4.Y"/> and <see cref="Vector4.Z"/> are the center point, and <see cref="Vector4.W"/> is the actual rotation in <c>radians</c>
    /// </remarks>
    public Vector4 RotationX { get; private set; }

    /// <summary>
    /// Describes the current rotation setting along the y axis of this <see cref="DrawOperation"/>
    /// </summary>
    /// <remarks>
    /// Where <see cref="Vector4.X"/>, <see cref="Vector4.Y"/> and <see cref="Vector4.Z"/> are the center point, and <see cref="Vector4.W"/> is the actual rotation in <c>radians</c>
    /// </remarks>
    public Vector4 RotationY { get; private set; }

    /// <summary>
    /// Describes the current rotation setting along the z axis of this <see cref="DrawOperation"/>
    /// </summary>
    /// <remarks>
    /// Where <see cref="Vector4.X"/>, <see cref="Vector4.Y"/> and <see cref="Vector4.Z"/> are the center point, and <see cref="Vector4.W"/> is the actual rotation in <c>radians</c>
    /// </remarks>
    public Vector4 RotationZ { get; private set; }

    #endregion

    /// <summary>
    /// Represents this <see cref="DrawOperation"/>'s preferred priority. May or may not be honored depending on the <see cref="DrawOperationManager"/>
    /// </summary>
    public float PreferredPriority { get; set; }

    /// <summary>
    /// Represents this <see cref="DrawOperation"/>'s CommandList Group affinity. If this value is 
    /// </summary>
    /// <remarks>
    /// This value will also set this <see cref="DrawOperation"/> in its own draw priority group; which means that it will be drawn over <see cref="DrawOperation"/>s that have a lower <see cref="CommandListGroupAffinity"/> regardless of priority. If in doubt, leave this as <see langword="null"/>
    /// </remarks>
    public uint? CommandListGroupAffinity { get; init; }
    internal int _clga => (int)(CommandListGroupAffinity ?? 0);


    /// <summary>
    /// The owner <see cref="DrawOperationManager"/> of this <see cref="DrawOperation"/>
    /// </summary>
    /// <remarks>
    /// Will throw if this <see cref="DrawOperation"/> is not registered
    /// </remarks>
    public DrawOperationManager Owner => _owner ?? throw new InvalidOperationException("Cannot query the Owner of an unregistered DrawOperation");
    private DrawOperationManager? _owner;
    internal void SetOwner(DrawOperationManager owner)
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

    internal async ValueTask Register(GraphicsManager manager)
    {
        ThrowIfDisposed();
        VerifyManager(manager);

        Registering(manager);

        var device = manager.Device;
        var factory = device.ResourceFactory;
        Device = device;

        var resb = ResourceSetBuilderPool.Rent();
        try
        {
            await CreateResourceSets(device, resb, factory);
            resb.Build(out var sets, out var layouts, factory);
            await CreateResources(device, factory, sets, layouts);
        }
        finally
        {
            ResourceSetBuilderPool.Return(resb);
        }

        Registered();
        NotifyIsReady();
    }

    #endregion

    #region Reaction Methods

    /// <summary>
    /// This method is called automatically when this <see cref="DrawOperation"/> is being registered onto <paramref name="manager"/>
    /// </summary>
    /// <param name="manager">The <see cref="GraphicsManager"/> this <see cref="DrawOperation"/> is being registered onto</param>
    protected virtual void Registering(GraphicsManager manager) { }

    /// <summary>
    /// This method is called automatically when this <see cref="DrawOperation"/> has been registered
    /// </summary>
    protected virtual void Registered() { }

    #endregion

    #endregion

    #region Drawing

    #region Internal

    private bool pendingGpuUpdate = true;

    /// <summary>
    /// Flags this <see cref="DrawOperation"/> as needing to update GPU data before the next <see cref="Draw(TimeSpan, CommandList, GraphicsDevice, FramebufferTargetInfo)"/> call
    /// </summary>
    /// <remarks>
    /// Multiple calls to this method will not result in <see cref="UpdateGPUState(GraphicsDevice, CommandList)"/> being called multiple times
    /// </remarks>
    protected void NotifyPendingGPUUpdate() => pendingGpuUpdate = true;

    internal async ValueTask InternalDraw(TimeSpan delta, CommandList cl, FramebufferTargetInfo target)
    {
        Debug.Assert(target.Target is not null, "The target info does not contain a valid Framebuffer");
        ThrowIfDisposed();
        sync.Wait();
        try
        {
            var device = Device!;

            if (pendingGpuUpdate)
            {
                pendingGpuUpdate = false;
                await UpdateGPUState(device, cl);
            }
            await Draw(delta, cl, device, target).ConfigureAwait(false);
        }
        finally
        {
            sync.Release();
        }
    }

    #endregion

    #region Reaction Methods

    ///// <summary>
    ///// If this property is <c>true</c>, then <see cref="CreateWindowSizedResources(GraphicsDevice, ResourceFactory, Vector4)"/> will be called inmediately upon <see cref="GraphicsManager.Window"/> being resized. This also means that it'll be executed from the VideoThread. Otherwise, if <c>false</c>, then <see cref="CreateWindowSizedResources(GraphicsDevice, ResourceFactory, Vector4)"/> will be called right before the next call to <see cref="Draw(Vector2, CommandList, GraphicsDevice, Framebuffer)"/>, from the owner <see cref="GraphicsManager"/>'s async loop
    ///// </summary>
    ///// <remarks>
    ///// Defaults to <c>true</c>
    ///// </remarks>
    //protected internal bool ReplaceWindowSizeResourcesInmediately { get; init; } = true;

    /// <summary>
    /// Creates the necessary resource sets for this <see cref="DrawOperation"/>
    /// </summary>
    /// <remarks>
    /// This method is called before <see cref="CreateResources(GraphicsDevice, ResourceFactory, ResourceSet[], ResourceLayout[])"/>. The base method at <see cref="DrawOperation"/> creates <see cref="TransformationSet"/> and the appropriate buffer
    /// </remarks>
    /// <param name="factory"><paramref name="device"/>'s <see cref="ResourceFactory"/></param>
    /// <param name="builder">The collection of descriptions that will be used to build the resource sets for this <see cref="DrawOperation"/>. This object is borrowed from a pool and will be cleared and returned after this method returns</param>
    /// <param name="device">The Veldrid <see cref="GraphicsDevice"/> attached to the <see cref="GraphicsManager"/> this <see cref="DrawOperation"/> is registered on</param>
    protected virtual ValueTask CreateResourceSets(GraphicsDevice device, ResourceSetBuilder builder, ResourceFactory factory)
    {
        TransformationBuffer = factory.CreateBuffer(
            new BufferDescription(
                DataStructuring.FitToUniformBuffer((uint)Unsafe.SizeOf<Matrix4x4>() + (uint)Unsafe.SizeOf<ColorTransformation>()),
                BufferUsage.UniformBuffer
            )
        );
        TransformationSet = factory.CreateResourceSet(new ResourceSetDescription(TransformationLayout, TransformationBuffer));
        var vtrans = VertexTransformation;
        var ctrans = ColorTransformation;
        device.UpdateBuffer(TransformationBuffer, VertexTransformationOffset, ref vtrans);
        device.UpdateBuffer(TransformationBuffer, ColorTransformationOffset, ref ctrans);
        builder.InsertLast(TransformationSet, TransformationLayout, out int _, "DrawOperation Base Transformation");

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Creates the necessary resources for this <see cref="DrawOperation"/>
    /// </summary>
    /// <remarks>
    /// This method is called after <see cref="CreateResourceSets(GraphicsDevice, ResourceSetBuilder, ResourceFactory)"/>
    /// </remarks>
    /// <param name="device">The Veldrid <see cref="GraphicsDevice"/> attached to the <see cref="GraphicsManager"/> this <see cref="DrawOperation"/> is registered on</param>
    /// <param name="factory"><paramref name="device"/>'s <see cref="ResourceFactory"/></param>
    /// <param name="resourceSets">The resource sets generated by <see cref="CreateResourceSets(GraphicsDevice, ResourceSetBuilder, ResourceFactory)"/></param>
    /// <param name="resourceLayouts">The resource layouts generated by <see cref="CreateResourceSets(GraphicsDevice, ResourceSetBuilder, ResourceFactory)"/></param>
    protected abstract ValueTask CreateResources(GraphicsDevice device, ResourceFactory factory, ResourceSet[]? resourceSets, ResourceLayout[]? resourceLayouts);

    /// <summary>
    /// The method that will be used to draw the component
    /// </summary>
    /// <remarks>
    /// Calling <see cref="GameObject.ThrowIfDisposed()"/> or <see cref="GameObject.Dispose(bool)"/> from this method WILL ALWAYS cause a deadlock! Remember that <paramref name="commandList"/> is *NOT* thread-safe, but it is used only one <see cref="DrawOperation"/> at a time, managed externally; and <see cref="GraphicsManager"/> will not use it until this method returns.
    /// </remarks>
    /// <param name="delta">The amount of time that has passed since the last draw sequence</param>
    /// <param name="device">The Veldrid <see cref="GraphicsDevice"/> attached to the <see cref="GraphicsManager"/> this <see cref="DrawOperation"/> is registered on</param>
    /// <param name="commandList">The <see cref="CommandList"/> opened specifically for this call. <see cref="CommandList.End"/> will be called AFTER this method returns, so don't call it yourself</param>
    /// <param name="target">Information about the <see cref="Framebuffer"/> that this specific call should be rendered to. Use <see cref="FramebufferTargetInfo.Target"/> with to use with <see cref="CommandList.SetFramebuffer(Framebuffer)"</param>
    protected abstract ValueTask Draw(TimeSpan delta, CommandList commandList, GraphicsDevice device, FramebufferTargetInfo target);

    /// <summary>
    /// This method is called automatically when this <see cref="DrawOperation"/> is going to be drawn for the first time, and after <see cref="NotifyPendingGPUUpdate"/> is called. Whenever applicable, this method ALWAYS goes before <see cref="Draw(TimeSpan, CommandList, GraphicsDevice, FramebufferTargetInfo)"/>
    /// </summary>
    /// <remarks>
    /// Calling <see cref="GameObject.ThrowIfDisposed()"/> or <see cref="GameObject.Dispose(bool)"/> from this method WILL ALWAYS cause a deadlock!
    /// </remarks>
    /// <param name="commandList">The <see cref="CommandList"/> opened specifically for this call. <see cref="CommandList.End"/> will be called AFTER this method returns, so don't call it yourself</param>
    /// <param name="device">The Veldrid <see cref="GraphicsDevice"/> attached to the <see cref="GraphicsManager"/> this <see cref="DrawOperation"/> is registered on</param>
    protected virtual ValueTask UpdateGPUState(GraphicsDevice device, CommandList commandList)
    {
        UpdateTransformationBuffer(commandList);
        return ValueTask.CompletedTask;
    }

    #endregion

    #endregion

    #region Disposal

    internal override void InternalDispose(bool disposing)
    {
        sync.Wait();
        try
        {
            if (Manager is GraphicsManager manager)
            {
                var @lock = manager.LockManagerDrawing();
                try
                {
                    Dispose(disposing);
                }
                finally
                {
                    Device = null;
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
