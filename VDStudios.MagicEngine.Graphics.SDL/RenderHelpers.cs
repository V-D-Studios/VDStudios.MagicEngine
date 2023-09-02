using System.Numerics;
using SDL2.NET;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Graphics.SDL;

namespace VDStudios.MagicEngine.SDL.Demo;

/// <summary>
/// A collection of extension methods to perform repetitive tasks related to rendering a texture on screen
/// </summary>
public static class RenderHelpers
{
    /// <summary>
    /// Applies a <see cref="DrawOperation{TGraphicsContext}"/>'s color transformation to <paramref name="texture"/>
    /// </summary>
    /// <remarks>
    /// Due to limitations with SDL, <see cref="ColorEffect.Tinted"/> and <see cref="ColorEffect.Overlay"/> accomplish the same effect, and <see cref="ColorEffect.GrayScale"/> is ignored
    /// </remarks>
    public static void ApplyColor(this DrawOperation<SDLGraphicsContext> dop, Texture texture)
    {
        var color = dop.ColorTransformation;

        if (color.Effects.HasFlag(ColorEffect.Tinted))
            texture.ColorAlpha = new RGBAColor((byte)(color.Tint.X * 255), (byte)(color.Tint.Y * 255), (byte)(color.Tint.Z * 255), (byte)(color.Tint.W * 255));

        if (color.Effects.HasFlag(ColorEffect.Overlay))
            texture.ColorAlpha = new RGBAColor((byte)(color.Overlay.X * 255), (byte)(color.Overlay.Y * 255), (byte)(color.Overlay.Z * 255), (byte)(color.Overlay.W * 255));

        texture.Alpha = (byte)(color.Effects.HasFlag(ColorEffect.OpacityOverride) 
            ? color.Opacity 
            : color.Effects.HasFlag(ColorEffect.OpacityMultiply) 
            ? (texture.Alpha / 255f) * color.Opacity 
            : 1f
        );
    }

    /// <summary>
    /// Creates a rectangle for use as the destination rectangle of a <see cref="Texture.Render(Rectangle?, Rectangle?)"/> call
    /// </summary>
    /// <param name="dop">The <see cref="DrawOperation{TGraphicsContext}"/> from which to fetch the transformation matrices</param>
    /// <param name="sourceSize">The size of the source rectangle, for scaling</param>
    /// <param name="transformation">The <see cref="DrawTransformation"/> parameters</param>
    /// <returns>The <see cref="FloatRectangle"/> to be used as the destination. Consider calling <see cref="ToRectangle(FloatRectangle)"/> on it</returns>
    public static FloatRectangle CreateDestinationRectangle(this DrawOperation<SDLGraphicsContext> dop, Vector2 sourceSize, DrawTransformation transformation)
    {
        var pos = Vector2.Transform(Vector2.Zero, dop.TransformationState.VertexTransformation * transformation.View * transformation.Projection);
        var size = Vector2.Transform(sourceSize, dop.TransformationState.ScaleTransformation * transformation.View * transformation.Projection);
        return new FloatRectangle(pos, size);
    }

    /// <summary>
    /// Creates a rectangle for use as the destination rectangle of a <see cref="Texture.Render(Rectangle?, Rectangle?)"/> call
    /// </summary>
    /// <param name="dop">The <see cref="DrawOperation{TGraphicsContext}"/> from which to fetch the transformation matrices</param>
    /// <param name="sourceSize">The size of the source rectangle, for scaling</param>
    /// <param name="transformation">The <see cref="DrawTransformation"/> parameters</param>
    /// <returns>The <see cref="FloatRectangle"/> to be used as the destination. Consider calling <see cref="ToRectangle(FloatRectangle)"/> on it</returns>
    public static FloatRectangle CreateDestinationRectangle(this DrawOperation<SDLGraphicsContext> dop, Size sourceSize, DrawTransformation transformation)
        => CreateDestinationRectangle(dop, sourceSize.ToVector2(), transformation);

    /// <summary>
    /// Converts a MagicEngine <see cref="FloatRectangle"/> into an SDL <see cref="Rectangle"/>
    /// </summary>
    /// <param name="rec">The <see cref="FloatRectangle"/> to convert</param>
    /// <returns>The converted <see cref="Rectangle"/></returns>
    public static Rectangle ToRectangle(this FloatRectangle rec)
        => new((int)rec.Width, (int)rec.Height, (int)rec.X, (int)rec.Y);

    /// <summary>
    /// Renders a <see cref="Texture"/> using two <see cref="FloatRectangle"/>s instead of SDL's <see cref="Rectangle"/> or <see cref="FRectangle"/>
    /// </summary>
    /// <remarks>The texture is blended with the destination based on its blend mode, color modulation and alpha modulation set with <see cref="Texture.BlendMode"/>, <see cref="Texture.Color"/>, and <see cref="Texture.Alpha"/> respectively.</remarks>
    /// <param name="texture">The <see cref="Texture"/> texture to render</param>
    /// <param name="source">The source rectangle for this operation. The rectangle will be used to capture a portion of the texture. Set to null to use the entire texture.</param>
    /// <param name="destination">The destination rectangle for this operation. The texture will be stretched to fill the given rectangle. Set to null to fill the entire rendering target</param>
    public static void Render(this Texture texture, FloatRectangle? source, FloatRectangle? destination = null)
        => texture.Render(
            source is FloatRectangle s ? new Rectangle((int)s.Width, (int)s.Height, (int)s.X, (int)s.Y) : null,
            destination is FloatRectangle d ? new Rectangle((int)d.Width, (int)d.Height, (int)d.X, (int)d.Y) : null
        );
}
