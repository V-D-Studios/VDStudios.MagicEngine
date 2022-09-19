using System.Numerics;
using Veldrid;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents the color transformation data for a given <see cref="DrawOperation"/>
/// </summary>
public readonly struct ColorTransformation
{
    /// <summary>
    /// A color that will be tinted over every fragment
    /// </summary>
    public Vector4 Tint { get; init; }

    /// <summary>
    /// A color that will be overlayed over every fragment
    /// </summary>
    public Vector4 Overlay { get; init; }

    /// <summary>
    /// The effects that are enabled for this <see cref="ColorTransformation"/>
    /// </summary>
    public ColorEffect Effects { get; init; }

    /// <summary>
    /// Constructs a new instance of type <see cref="ColorTransformation"/>
    /// </summary>
    /// <param name="effects">The effects that will be enabled for the constructed <see cref="ColorTransformation"/></param>
    /// <param name="tint">The color tint that will be applied over the fragments</param>
    /// <param name="overlay">The color that will be overlayed over the fragments</param>
    public ColorTransformation(ColorEffect effects, Vector4 tint = default, Vector4 overlay = default)
    {
        Tint = tint;
        Overlay = overlay;
        Effects = effects;
    }

    /// <summary>
    /// Creates a <see cref="ColorTransformation"/> that tints the fragments with <paramref name="tint"/>
    /// </summary>
    /// <param name="tint">The color to tint the fragments with</param>
    /// <returns>The created <see cref="ColorTransformation"/></returns>
    public static ColorTransformation CreateTint(RgbaFloat tint)
        => new(ColorEffect.Tinted, tint: tint.ToVector4());

    /// <summary>
    /// Creates a <see cref="ColorTransformation"/> that overlays <paramref name="overlay"/> over the fragments
    /// </summary>
    /// <param name="overlay">The color to overlay over the fragments</param>
    /// <returns>The created <see cref="ColorTransformation"/></returns>
    public static ColorTransformation CreateOverlay(RgbaFloat overlay)
        => new(ColorEffect.Overlay, overlay: overlay.ToVector4());

    /// <summary>
    /// Creates a <see cref="ColorTransformation"/> that both tints and overlays the fragments with <paramref name="tint"/> and <paramref name="overlay"/> respectively
    /// </summary>
    /// <param name="tint">The color to tint the fragments with</param>
    /// <param name="overlay">The color to overlay over the fragments</param>
    /// <returns>The created <see cref="ColorTransformation"/></returns>
    public static ColorTransformation CreateTintAndOverlay(RgbaFloat tint, RgbaFloat overlay)
        => new(ColorEffect.Tinted | ColorEffect.Overlay, tint: tint.ToVector4(), overlay: overlay.ToVector4());

    /// <summary>
    /// Creates a <see cref="ColorTransformation"/> that tints the fragments with <paramref name="tint"/>
    /// </summary>
    /// <param name="tint">The color to tint the fragments with</param>
    /// <returns>The created <see cref="ColorTransformation"/></returns>
    public static ColorTransformation CreateTint(Vector4 tint)
        => new(ColorEffect.Tinted, tint: tint);

    /// <summary>
    /// Creates a <see cref="ColorTransformation"/> that overlays <paramref name="overlay"/> over the fragments
    /// </summary>
    /// <param name="overlay">The color to overlay over the fragments</param>
    /// <returns>The created <see cref="ColorTransformation"/></returns>
    public static ColorTransformation CreateOverlay(Vector4 overlay)
        => new(ColorEffect.Overlay, overlay: overlay);

    /// <summary>
    /// Creates a <see cref="ColorTransformation"/> that both tints and overlays the fragments with <paramref name="tint"/> and <paramref name="overlay"/> respectively
    /// </summary>
    /// <param name="tint">The color to tint the fragments with</param>
    /// <param name="overlay">The color to overlay over the fragments</param>
    /// <returns>The created <see cref="ColorTransformation"/></returns>
    public static ColorTransformation CreateTintAndOverlay(Vector4 tint, Vector4 overlay)
        => new(ColorEffect.Tinted | ColorEffect.Overlay, tint: tint, overlay: overlay);
}
