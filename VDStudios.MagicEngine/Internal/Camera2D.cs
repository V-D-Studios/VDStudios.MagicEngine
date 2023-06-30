using System.Numerics;
using System.Security.Cryptography;
using SDL2.NET;
using Veldrid;

namespace VDStudios.MagicEngine.Internal;

/// <summary>
/// Basic functionality for a 2D Camera
/// </summary>
/// <remarks>
/// This class is for internal use only
/// </remarks>
public abstract class Camera2D : IRenderTarget
{
    /// <inheritdoc/>
    protected readonly DrawParameters drawParameters;
    private bool projectionUpToDate = false;

    /// <inheritdoc/>
    public GraphicsManager Owner { get; }

    /// <summary>
    /// An <see cref="IInterpolator"/> object that interpolates the projection between its current state and its destined state
    /// </summary>
    /// <remarks>
    /// If <see langword="null"/>, no interpolation takes place
    /// </remarks>
    public IInterpolator? Interpolator { get; }

    /// <summary>
    /// The rotation of the camera in radians
    /// </summary>
    public float Rotation
    {
        get => TotalRotation.X;
        set => TotalRotation = TotalRotation with { X = value };
    }

    /// <summary>
    /// The skew of the camera along the X axis
    /// </summary>
    public float SkewX
    {
        get => TotalRotation.Z;
        set => TotalRotation = TotalRotation with { Z = value };
    }

    /// <summary>
    /// The skew of the camera along the Y axis
    /// </summary>
    public float SkewY 
    {
        get => TotalRotation.Y;
        set => TotalRotation = TotalRotation with { Y = value };
    }

    /// <summary>
    /// The total rotation of the camera, includes: <see cref="Rotation"/>, <see cref="SkewX"/> and <see cref="SkewY"/>
    /// </summary>
    public Vector3 TotalRotation
    {
        get => _totalRotation;
        set
        {
            if (_totalRotation != value)
            {
                _totalRotation = value;
                InvalidateProjection();
            }
        }
    }
    private Vector3 _totalRotation;

    /// <summary>
    /// The current zoom level of the camera, or scale
    /// </summary>
    /// <remarks>
    /// No attempt will be made to denounce a zoom level that is less than or equal to 0, which will likely cause an inversion of view or none at all. Use at your own risk
    /// </remarks>
    public float Zoom
    {
        get => _zoom;
        set
        {
            if (_zoom != value)
            {
                _zoom = value;
                InvalidateProjection();
            }
        }
    }
    private float _zoom;

    /// <summary>
    /// The current position of the camera relative to 0,0
    /// </summary>
    /// <remarks>
    /// May change representation depending on the platform
    /// </remarks>
    public Vector2 Position
    {
        get => _pos;
        set
        {
            if (_pos != value)
            {
                _pos = value;
                InvalidateProjection();
            }
        }
    }
    private Vector2 _pos;

    /// <inheritdoc/>
    public Camera2D(GraphicsManager owner, IInterpolator? interpolator)
    {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        drawParameters = new();
        Owner.RegisterSharedDrawResource(drawParameters);
        Interpolator = interpolator;
    }

    /// <inheritdoc/>
    public abstract void GetTarget(GraphicsDevice device, TimeSpan delta, out Framebuffer targetBuffer, out DrawParameters targetParameters);

    /// <inheritdoc/>
    public abstract void CopyToScreen(CommandList managerCommandList, Framebuffer framebuffer, GraphicsDevice device);

    /// <inheritdoc/>
    public abstract bool QueryCopyToScreenRequired(GraphicsDevice device);

    /// <inheritdoc/>
    public abstract void PrepareForDraw(CommandList managerCommandList);

    /// <summary>
    /// Creates a projection matrix from this camera's current parameters
    /// </summary>
    protected virtual Matrix4x4 CreateProjection()
    {
        return Matrix4x4.CreateRotationX(Rotation) *
                    Matrix4x4.CreateTranslation(Position.X, Position.Y, 0) *
                    Matrix4x4.CreateScale(Zoom);
    }

    /// <summary>
    /// Checks if a new projection needs to be calculated and, if so, calls <see cref="CreateProjection"/>, followed by interpolating the matrix if necessary and finally marking the projection to be up to date
    /// </summary>
    /// <param name="delta">The amount of time that has passed since the last frame</param>
    /// <returns>The newly interpolated matrix</returns>
    protected void UpdateProjection(TimeSpan delta)
    {
        if (projectionUpToDate is false)
        {
            var prj = CreateProjection();
            if (Interpolator is IInterpolator interpolator) 
                prj = interpolator.Interpolate(drawParameters.Transformation.Projection, prj, (float)delta.TotalMilliseconds);
            drawParameters.Transformation = drawParameters.Transformation with { Projection = prj };
            projectionUpToDate = true;
        }
    }

    /// <summary>
    /// Invalidates the current projection, causing it to be recalculated by <see cref="CreateProjection"/>
    /// </summary>
    protected void InvalidateProjection()
    {
        projectionUpToDate = false;
    }
}
