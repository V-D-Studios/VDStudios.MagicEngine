using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;
using System.Transactions;

namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// Represents the current state of a transformation <see cref="Matrix4x4"/> given a set of different, singular parameters
/// </summary>
public sealed class TransformationState
{
    /// <summary>
    /// The transformation matrix that represents the current scaling properties in this <see cref="TransformationState"/>
    /// </summary>
    /// <remarks>
    /// <see cref="VertexTransformation"/> already includes this transformation and should not be mixed externally
    /// </remarks>
    public Matrix4x4 ScaleTransformation
    {
        get
        {
            if (scaletrans is not Matrix4x4 t)
                scaletrans = t = Matrix4x4.CreateScale(Scale);
            ScaleTransformationChanged?.Invoke(this);
            return t;
        }
    }
    private Matrix4x4? scaletrans = Matrix4x4.Identity;

    /// <summary>
    /// The transformation matrix that represents the current transformation properties in this <see cref="TransformationState"/>
    /// </summary>
    /// <remarks>
    /// This transformation can be used to represent the current world properties of the drawing operation, for example, it's position and rotation in relation to the world itself
    /// </remarks>
    public Matrix4x4 VertexTransformation
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
                VertexTransformationChanged?.Invoke(this);
            }
            return t;
        }
    }
    private Matrix4x4? vertrans = Matrix4x4.Identity;
    
    /// <summary>
    /// Fired when <see cref="VertexTransformation"/> changes
    /// </summary>
    public event GeneralGameEvent<TransformationState>? VertexTransformationChanged;

    /// <summary>
    /// Fired when <see cref="ScaleTransformation"/> changes
    /// </summary>
    public event GeneralGameEvent<TransformationState>? ScaleTransformationChanged;

    /// <summary>
    /// Adjusts the transformation parameters and calculates the appropriate transformation matrix for this <see cref="TransformationState"/>
    /// </summary>
    /// <remarks>
    /// Parameters that are not specified (i.e. left as <c>null</c>) will default to the current transformation setting in this <see cref="TransformationState"/>
    /// </remarks>
    /// <param name="translation">The translation in worldspace for this operation</param>
    /// <param name="scale">The scale in worldspace for this operation</param>
    /// <param name="rotX">The rotation along the x axis in worldspace for this operation</param>
    /// <param name="rotY">The rotation along the y axis in worldspace for this operation</param>
    /// <param name="rotZ">The rotation along the z axis in worldspace for this operation</param>
    public void Transform(Vector3? translation = null, Vector3? scale = null, Vector4? rotX = null, Vector4? rotY = null, Vector4? rotZ = null)
    {
        if (scale is not null)
        {
            Scale = scale.Value;
            scaletrans = null;
        }
        Translation = translation ?? Translation;
        RotationX = rotX ?? RotationX;
        RotationY = rotY ?? RotationY;
        RotationZ = rotZ ?? RotationZ;
        vertrans = null;
    }

    /// <summary>
    /// Describes the current translation setting of this <see cref="TransformationState"/>
    /// </summary>
    public Vector3 Translation { get; private set; }

    /// <summary>
    /// Describes the current scale setting of this <see cref="TransformationState"/>
    /// </summary>
    public Vector3 Scale { get; private set; } = Vector3.One;

    /// <summary>
    /// Describes the current rotation setting along the x axis of this <see cref="TransformationState"/>
    /// </summary>
    /// <remarks>
    /// Where <see cref="Vector4.X"/>, <see cref="Vector4.Y"/> and <see cref="Vector4.Z"/> are the center point, and <see cref="Vector4.W"/> is the actual rotation in <c>radians</c>
    /// </remarks>
    public Vector4 RotationX { get; private set; }

    /// <summary>
    /// Describes the current rotation setting along the y axis of this <see cref="TransformationState"/>
    /// </summary>
    /// <remarks>
    /// Where <see cref="Vector4.X"/>, <see cref="Vector4.Y"/> and <see cref="Vector4.Z"/> are the center point, and <see cref="Vector4.W"/> is the actual rotation in <c>radians</c>
    /// </remarks>
    public Vector4 RotationY { get; private set; }

    /// <summary>
    /// Describes the current rotation setting along the z axis of this <see cref="TransformationState"/>
    /// </summary>
    /// <remarks>
    /// Where <see cref="Vector4.X"/>, <see cref="Vector4.Y"/> and <see cref="Vector4.Z"/> are the center point, and <see cref="Vector4.W"/> is the actual rotation in <c>radians</c>
    /// </remarks>
    public Vector4 RotationZ { get; private set; }
}