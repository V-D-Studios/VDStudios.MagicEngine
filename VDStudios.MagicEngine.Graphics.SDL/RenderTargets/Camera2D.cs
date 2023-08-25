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
    private SDLCamera2D_State goal = new();
    private Matrix4x4 current = Matrix4x4.Identity;

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
        => goal.Transform(translation, scale, rotX, rotY, rotZ);

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
        current = goal.Transformation;
    }

    /// <inheritdoc/>
    public override void BeginFrame(TimeSpan delta, SDLGraphicsContext context)
    {
        if (Target is IWorldMobile2D target)
        {

            goal.Transform(translation: new((context.Window.Size.ToVector2() / 2) - target.Position, 0), new Vector3(1, 1, 1));
        }
        current = Interpolator.Interpolate(current, goal.Transformation, (float)delta.TotalMilliseconds);
        Transformation = new DrawTransformation(current, Manager.WindowView);
    }

    /// <inheritdoc/>
    public override void RenderDrawOperation(TimeSpan delta, SDLGraphicsContext context, DrawOperation<SDLGraphicsContext> drawOperation)
    {
        InvokeDrawOperation(delta, drawOperation, context);
    }

    /// <inheritdoc/>
    public override void EndFrame(SDLGraphicsContext context) { }

    private struct SDLCamera2D_State
    {
        public Matrix4x4 Transformation
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

        public Vector3 Translation { get; private set; }

        public Vector3 Scale { get; private set; } = Vector3.One;

        public Vector4 RotationX { get; private set; }

        public Vector4 RotationY { get; private set; }

        public Vector4 RotationZ { get; private set; }

        public SDLCamera2D_State()
        {
        }

        public void Transform(Vector3? translation = null, Vector3? scale = null, Vector4? rotX = null, Vector4? rotY = null, Vector4? rotZ = null)
        {
            Translation = translation ?? Translation;
            Scale = scale ?? Scale;
            RotationX = rotX ?? RotationX;
            RotationY = rotY ?? RotationY;
            RotationZ = rotZ ?? RotationZ;
            vertrans = null;
        }
    }
}
