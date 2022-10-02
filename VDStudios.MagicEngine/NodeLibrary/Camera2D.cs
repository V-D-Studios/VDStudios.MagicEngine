using System.Numerics;
using static System.Formats.Asn1.AsnWriter;
using System.Transactions;
using Veldrid;
using System.Diagnostics.CodeAnalysis;
using MessagePack.Formatters;

namespace VDStudios.MagicEngine.NodeLibrary;

/// <summary>
/// Represents a 2D camera in the node structure
/// </summary>
/// <remarks>
/// This node adjusts the <see cref="DrawOperation.Parameters"/> of all draw operations in this node's children
/// </remarks>
public class Camera2D : Node, IDrawableNode
{
    private class CameraDrawParameters : DrawParameters
    {
        private readonly Camera2D Camera;

        public CameraDrawParameters(Camera2D cam) => Camera = cam;

        public override ValueTask Update(TimeSpan delta, GraphicsManager manager, GraphicsDevice device, CommandList commandList)
        {
            Transformation = new(
                Camera.Interpolator.Interpolate(
                    Transformation.View, 
                    Camera.ViewMatrix, 
                    float.Min(Camera.RateOfChange * (float)delta.TotalMilliseconds, 1)
                ),
                Matrix4x4.Identity
            );
            return base.Update(delta, manager, device, commandList);
        }
    }

    private readonly CameraDrawParameters drawParameters;

    /// <summary>
    /// Instances a new <see cref="Camera2D"/> node
    /// </summary>
    public Camera2D(IInterpolator? interpolator = null)
    {
        interp = interpolator ?? LinearInterpolator.Interpolator;
        DrawOperationManager = new(this);
        drawParameters = new CameraDrawParameters(this);
        Game.MainGraphicsManager.RegisterSharedDrawResource(drawParameters);
        drawParameters.WaitUntilReady();
        DrawOperationManager.CascadeThroughNode(drawParameters);
    }

    /// <summary>
    /// The interpolator for this Camera
    /// </summary>
    public IInterpolator Interpolator
    {
        get => interp;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            interp = value;
        }
    }
    IInterpolator interp;

    /// <summary>
    /// The transformation matrix that represents the current transformation properties in this <see cref="DrawOperation"/>
    /// </summary>
    /// <remarks>
    /// This transformation can be used to represent the current world properties of the drawing operation, for example, it's position and rotation in relation to the world itself
    /// </remarks>
    protected Matrix4x4 ViewMatrix
    {
        get
        {
            if (viewm is not Matrix4x4 t)
            {
                var (scl, scp) = Scale;
                var (rp, rot) = Rotation;
                viewm = t =
                    Matrix4x4.CreateTranslation(new(Position, 0)) *
                    Matrix4x4.CreateScale(new Vector3(scl, 1), new Vector3(scp, 0)) *
                    Matrix4x4.CreateRotationZ(rot, new(rp, 0));
            }
            return t;
        }
    }
    private Matrix4x4? viewm = Matrix4x4.Identity;

    /// <summary>
    /// Describes the current position setting of this <see cref="Camera2D"/>
    /// </summary>
    public Vector2 Position
    {
        get => __position;
        set
        {
            if (value == __position) return;
            __position = value;
            viewm = null;
        }
    }
    private Vector2 __position;

    /// <summary>
    /// Describes the current scale of this <see cref="Camera2D"/>
    /// </summary>
    /// <remarks>
    /// Can also be regarded as the zoom value, if both x and y are the same
    /// </remarks>
    public (Vector2 scale, Vector2 centerpoint) Scale
    {
        get => __scale;
        set
        {
            if (value == __scale) return;
            __scale = value;
            viewm = null;
        }
    }
    private (Vector2 scale, Vector2 centerPoint) __scale;

    /// <summary>
    /// Describes the current rotation of this <see cref="Camera2D"/>
    /// </summary>
    public (Vector2 centerPoint, float radians) Rotation
    {
        get => __rotation;
        set
        {
            if (value == __rotation) return;
            __rotation = value;
            viewm = null;
        }
    }
    private (Vector2 centerPoint, float radians) __rotation;

    /// <summary>
    /// Represents the rate of change per millisecond for this <see cref="Camera2D"/>, a value from 0.0 (no change) to 1.0 (instant change)
    /// </summary>
    /// <remarks>
    /// Depending on the object <see cref="Interpolator"/> is set to, the effects of this value can vary. By default, <see cref="LinearInterpolator.Instance"/> is used, and higher values result in faster transitions
    /// </remarks>
    public float RateOfChange { get; set; }

    /// <inheritdoc/>
    protected override ValueTask<bool> Updating(TimeSpan delta)
    {
        drawParameters.NotifyPendingUpdate();
        return new ValueTask<bool>(true);
    }

    /// <inheritdoc/>
    protected internal override bool FilterChildNode(Node child, [NotNullWhen(false)] out string? reasonForDenial)
    {
        if (child is Camera2D)
        {
            reasonForDenial = "A Camera2D cannot have another Camera2D as a child";
            return false;
        }

        reasonForDenial = null;
        return true;
    }

    /// <inheritdoc/>
    public DrawOperationManager DrawOperationManager { get; }
    
    /// <inheritdoc/>
    public bool SkipDrawPropagation { get; }
}