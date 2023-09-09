using System.Numerics;
using VDStudios.MagicEngine.Geometry;

namespace VDStudios.MagicEngine.Graphics.Veldrid.DrawOperations;

/// <summary>
/// An operation that renders a <see cref="ShapeDefinition2D"/>
/// </summary>
public class Shape2DRenderer : VeldridDrawOperation
{
    /// <summary>
    /// Creates a new object of type <see cref="TextOperation"/>
    /// </summary>
    public Shape2DRenderer(ShapeDefinition2D shape, Game game) : base(game)
    {
        _shape = shape;
    }

    /// <summary>
    /// The shape that will be Rendered
    /// </summary>
    public ShapeDefinition2D Shape
    {
        get => _shape;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (_shape != value)
            {
                _shape = value;
                lastver = _shape.Version;
                NotifyPendingGPUUpdate();
            }
        }
    }
    private ShapeDefinition2D _shape;
    private int lastver = 0;

    /// <inheritdoc/>
    protected override ValueTask CreateResourcesAsync()
        => ValueTask.CompletedTask;

    /// <inheritdoc/>
    protected override void CreateGPUResources(VeldridGraphicsContext context) { }

    /// <inheritdoc/>
    protected override void Draw(TimeSpan delta, VeldridGraphicsContext context, RenderTarget<VeldridGraphicsContext> target)
    {
        if (lastver != _shape.Version)
        {
            ForceGPUUpdate(context);
            lastver = _shape.Version;
        }

        var sh = Shape;
        Span<Vector2> vertices = stackalloc Vector2[sh.Count];
        sh.AsSpan().CopyTo(vertices);

        if (sh is RectangleDefinition rectangle)
        {
            context.Renderer.DrawRectangle(rectangle.ToFloatRectangle().ToRectangle(), Color);
        }
        else if (sh is ElipseDefinition elipse)
        {
            throw new NotSupportedException();
        }
        else if (sh is CircleDefinition circle)
        {
            throw new NotSupportedException();
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    /// <inheritdoc/>
    protected override void UpdateGPUState(VeldridGraphicsContext context)
    {

    }
}
