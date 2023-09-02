using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using VDStudios.MagicEngine.World2D;

namespace VDStudios.MagicEngine.Graphics.SDL.RenderTargets;

/// <summary>
/// Represents a 2D Camera that can move, rotate, and scale its contents
/// </summary>
public class SDLCamera2D : SDLRenderTarget
{
    private Matrix4x4 current = Matrix4x4.Identity;

    /// <summary>
    /// The current transformation goal for this <see cref="SDLCamera2D"/>
    /// </summary>
    public TransformationState Goal { get; } = new();

    /// <summary>
    /// The current Transformation Matrix that represents this <see cref="SDLCamera2D"/>'s view
    /// </summary>
    public Matrix4x4 CurrentView => current;

    /// <summary>
    /// The <see cref="IInterpolator"/> that will be used to interpolate between the camera's current position and its goal
    /// </summary>
    public IInterpolator Interpolator { get; }

    /// <summary>
    /// A Target for this <see cref="SDLCamera2D"/> to follow
    /// </summary>
    public IWorldMobile2D? Target { get; set; }

    /// <inheritdoc/>
    public SDLCamera2D(SDLGraphicsManager manager, IInterpolator? interpolator = default) : base(manager)
    {
        Interpolator = interpolator ?? LinearInterpolator.Instance;
    }

    /// <summary>
    /// Sets the camera's goal position to the respective position
    /// </summary>
    /// <remarks>
    /// Null parameters are ignored
    /// </remarks>
    /// <param name="translation">The translation in worldspace for this operation</param>
    /// <param name="scale">The scale in worldspace for this operation</param>
    /// <param name="rotX">The rotation along the x axis in worldspace for this operation</param>
    /// <param name="rotY">The rotation along the y axis in worldspace for this operation</param>
    /// <param name="rotZ">The rotation along the z axis in worldspace for this operation</param>
    public void Move(Vector3? translation = null, Vector3? scale = null, Vector4? rotX = null, Vector4? rotY = null, Vector4? rotZ = null)
        => Goal.Transform(translation, scale, rotX, rotY, rotZ);

    /// <summary>
    /// Forces the camera into the respective position
    /// </summary>
    /// <remarks>
    /// Null parameters are ignored
    /// </remarks>
    /// <param name="translation">The translation in worldspace for this operation</param>
    /// <param name="scale">The scale in worldspace for this operation</param>
    /// <param name="rotX">The rotation along the x axis in worldspace for this operation</param>
    /// <param name="rotY">The rotation along the y axis in worldspace for this operation</param>
    /// <param name="rotZ">The rotation along the z axis in worldspace for this operation</param>
    public void Set(Vector3? translation = null, Vector3? scale = null, Vector4? rotX = null, Vector4? rotY = null, Vector4? rotZ = null)
    {
        Move(translation, scale, rotX, rotY, rotZ);
        current = Goal.VertexTransformation;
    }

    /// <inheritdoc/>
    public override void BeginFrame(TimeSpan delta, SDLGraphicsContext context)
    {
        if (Target is IWorldMobile2D target)
            Goal.Transform(translation: new((context.Window.Size.ToVector2() / 4) - target.Position / 2, 0), scale: new Vector3(1, 1, 1));
        current = Interpolator.Interpolate(current, Goal.VertexTransformation, (float)delta.TotalMilliseconds);
        Transformation = new DrawTransformation(current, Manager.WindowView);
    }

    /// <inheritdoc/>
    public override void RenderDrawOperation(TimeSpan delta, SDLGraphicsContext context, DrawOperation<SDLGraphicsContext> drawOperation)
    {
        InvokeDrawOperation(delta, drawOperation, context);
    }

    /// <inheritdoc/>
    public override void EndFrame(SDLGraphicsContext context) { }
}
