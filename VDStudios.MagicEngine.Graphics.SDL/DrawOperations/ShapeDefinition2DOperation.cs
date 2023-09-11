using System.Numerics;
using SDL2.NET;
using VDStudios.MagicEngine.Geometry;

namespace VDStudios.MagicEngine.Graphics.SDL.DrawOperations;

/// <summary>
/// An operation that renders a <see cref="ShapeDefinition2D"/>
/// </summary>
public class ShapeDefinition2DOperation : DrawOperation<SDLGraphicsContext>
{
    /// <summary>
    /// Creates a new object of type <see cref="TextOperation"/>
    /// </summary>
    public ShapeDefinition2DOperation(ShapeDefinition2D shape, Game game) : base(game)
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
            if(_shape != value)
            {
                _shape = value;
                NotifyPendingGPUUpdate();
            }
        }
    }
    private ShapeDefinition2D _shape;

    /// <summary>
    /// The color of the rendered shapes
    /// </summary>
    public RGBAColor Color { get; set; } = RgbaVector.DarkRed.ToRGBAColor();

    /// <inheritdoc/>
    protected override ValueTask CreateResourcesAsync()
        => ValueTask.CompletedTask;

    /// <inheritdoc/>
    protected override void CreateGPUResources(SDLGraphicsContext context) { }

    /// <inheritdoc/>
    protected override void Draw(TimeSpan delta, SDLGraphicsContext context, RenderTarget<SDLGraphicsContext> target)
    {
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
    protected override void UpdateGPUState(SDLGraphicsContext context)
    {
        
    }
}
