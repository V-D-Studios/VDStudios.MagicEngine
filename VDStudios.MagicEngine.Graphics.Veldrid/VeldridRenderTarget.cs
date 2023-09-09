using System.Runtime.CompilerServices;
using VDStudios.MagicEngine.Graphics;
using Veldrid;

namespace VDStudios.MagicEngine.Graphics.Veldrid;

/// <inheritdoc/>
public abstract class VeldridRenderTarget : RenderTarget<VeldridGraphicsContext>
{
    /// <summary>
    /// Creates a new <see cref="VeldridRenderTarget"/> owned by <paramref name="manager"/>
    /// </summary>
    /// <param name="manager">The manager that owns this <see cref="VeldridRenderTarget"/></param>
    /// <exception cref="ArgumentNullException"></exception>
    protected VeldridRenderTarget(VeldridGraphicsManager manager) : base(manager)
    {
        TransformationBuffer = manager.GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(
            DataStructuring.FitToUniformBuffer<DrawTransformation, uint>(),
            BufferUsage.UniformBuffer
        ));
    }

    internal CommandList? cl;

    /// <inheritdoc/>
    public override DrawTransformation Transformation
    {
        get => base.Transformation;
        protected set
        {
            if (base.Transformation != value)
            {
                base.Transformation = value;
                pendingTransUpdate = true;
            }
        }
    }

    bool pendingTransUpdate = true;
    /// <summary>
    /// The transformation buffer for this <see cref="VeldridRenderTarget"/>
    /// </summary>
    public DeviceBuffer TransformationBuffer { get; private set; }

    /// <summary>
    /// The command list for this <see cref="VeldridRenderTarget"/>
    /// </summary>
    public CommandList CommandList => cl ?? throw new InvalidOperationException("Cannot obtain a CommandList for this RenderTarget before a Frame Starts or after a Frame ends");

    /// <summary>
    /// Fetches a <see cref="Framebuffer"/> for this <see cref="RenderTarget{TGraphicsContext}"/>
    /// </summary>
    public abstract Framebuffer GetFramebuffer(VeldridGraphicsContext context);

    /// <inheritdoc/>
    public override void BeginFrame(TimeSpan delta, VeldridGraphicsContext context)
    {
        if (pendingTransUpdate)
            CommandList.UpdateBuffer(TransformationBuffer, 0, Transformation);
    }
}
