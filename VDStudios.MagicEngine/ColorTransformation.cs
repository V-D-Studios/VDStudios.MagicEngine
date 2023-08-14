using System.Numerics;
using System.Runtime.InteropServices;
using VDStudios.MagicEngine.Graphics;
using Veldrid;

namespace VDStudios.MagicEngine;

/// <summary>
/// Provides an assortment of extensions for <see cref="ColorTransformation"/>
/// </summary>
public static class ColorTransformationExtensions
{
    /// <summary>
    /// Ensures <see cref="ColorEffect.OpacityOverride"/> is not set and sets <see cref="ColorEffect.OpacityMultiply"/>
    /// </summary>
    /// <param name="effect"></param>
    public static ColorEffect WithOpacityMultiply(this ColorEffect effect)
        => (effect & (ColorEffect)0xFFFFFFF7) | ColorEffect.OpacityMultiply;

    /// <summary>
    /// Returns a new <see cref="ColorTransformation"/> that has the specified opacity value set
    /// </summary>
    /// <param name="trans"></param>
    /// <param name="opacity">The opacity value to set</param>
    /// <param name="ovrride">If <see langword="true"/>, <see cref="ColorEffect.OpacityOverride"/> will be used instead of <see cref="ColorEffect.OpacityMultiply"/></param>
    /// <returns></returns>
    public static ColorTransformation WithOpacity(this in ColorTransformation trans, float opacity, bool ovrride = false)
        => trans with
        {
            Opacity = opacity,
            Effects = ovrride ? trans.Effects | ColorEffect.OpacityOverride : trans.Effects.WithOpacityMultiply()
        };

    /// <summary>
    /// Returns a new <see cref="ColorTransformation"/> that tints the fragments with <paramref name="tint"/>
    /// </summary>
    /// <param name="tint">The color to tint the fragments with</param>
    /// <param name="trans"></param>
    /// <returns>The created <see cref="ColorTransformation"/></returns>
    public static ColorTransformation WithTint(this in ColorTransformation trans, RgbaFloat tint)
        => trans with
        {
            Tint = tint.ToVector4(),
            Effects = ColorEffect.Tinted | ColorEffect.GrayScale | trans.Effects
        };

    /// <summary>
    /// Returns a new <see cref="ColorTransformation"/> that overlays <paramref name="overlay"/> over the fragments
    /// </summary>
    /// <param name="overlay">The color to overlay over the fragments</param>
    /// <param name="trans"></param>
    /// <returns>The created <see cref="ColorTransformation"/></returns>
    public static ColorTransformation WithOverlay(this in ColorTransformation trans, RgbaFloat overlay)
        => trans with 
        {
            Effects = ColorEffect.Overlay | trans.Effects,
            Overlay = overlay.ToVector4()
        };

    /// <summary>
    /// Returns a new <see cref="ColorTransformation"/> that tints the fragments with <paramref name="tint"/>
    /// </summary>
    /// <param name="tint">The color to tint the fragments with</param>
    /// <param name="trans"></param>
    /// <returns>The created <see cref="ColorTransformation"/></returns>
    public static ColorTransformation WithTint(this in ColorTransformation trans, Vector4 tint)
        => trans with 
        {
            Tint = tint,
            Effects = ColorEffect.Tinted | ColorEffect.GrayScale | trans.Effects
        };

    /// <summary>
    /// Returns a new <see cref="ColorTransformation"/> that overlays <paramref name="overlay"/> over the fragments
    /// </summary>
    /// <param name="overlay">The color to overlay over the fragments</param>
    /// <param name="trans"></param>
    /// <returns>The created <see cref="ColorTransformation"/></returns>
    public static ColorTransformation WithOverlay(this in ColorTransformation trans, Vector4 overlay)
        => trans with 
        {
            Effects = ColorEffect.Overlay | trans.Effects,
            Overlay = overlay
        };
}

/// <summary>
/// Represents the color transformation data for a given <see cref="DrawOperation"/>
/// </summary>
[StructLayout(LayoutKind.Sequential)]
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
    /// Overrides the alpha value of the fragment
    /// </summary>
    public float Opacity { get; init; } = 1;

    /// <summary>
    /// Deconstructs this <see cref="ColorTransformation"/> in the following order: <see cref="Tint"/>, <see cref="Overlay"/>, <see cref="Opacity"/> and <see cref="Effects"/>
    /// </summary>
    public void Deconstruct(out Vector4 tint, out Vector4 overlay, out float opacity, out ColorEffect effects)
    {
        tint = Tint;
        overlay = Overlay;
        opacity = Opacity;
        effects = Effects;
    }

    /// <summary>
    /// Constructs a new instance of type <see cref="ColorTransformation"/>
    /// </summary>
    /// <param name="effects">The effects that will be enabled for the constructed <see cref="ColorTransformation"/></param>
    /// <param name="tint">The color tint that will be applied over the fragments</param>
    /// <param name="overlay">The color that will be overlayed over the fragments</param>
    /// <param name="opacity">The alpha value that will override the fragment's alpha</param>
    public ColorTransformation(ColorEffect effects, Vector4 tint = default, Vector4 overlay = default, float opacity = default)
    {
        Tint = tint;
        Overlay = overlay;
        Effects = effects;
        Opacity = opacity;
    }

    /// <summary>
    /// Creates a <see cref="ColorTransformation"/> that overrides the opacity of the fragments
    /// </summary>
    /// <param name="opacity">The opacity the fragments will have</param>
    /// <param name="ovrride">If <see langword="true"/>, <see cref="ColorEffect.OpacityOverride"/> will be used instead of <see cref="ColorEffect.OpacityMultiply"/></param>
    /// <returns>The created <see cref="ColorTransformation"/></returns>
    public static ColorTransformation CreateOpacity(float opacity, bool ovrride = false)
        => new(ovrride ? ColorEffect.OpacityOverride : ColorEffect.OpacityMultiply, opacity: opacity);

    /// <summary>
    /// Creates a <see cref="ColorTransformation"/> that tints the fragments with <paramref name="tint"/>
    /// </summary>
    /// <param name="tint">The color to tint the fragments with</param>
    /// <returns>The created <see cref="ColorTransformation"/></returns>
    public static ColorTransformation CreateTint(RgbaFloat tint)
        => new(ColorEffect.Tinted | ColorEffect.GrayScale, tint: tint.ToVector4());

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
        => new(ColorEffect.Tinted | ColorEffect.GrayScale | ColorEffect.Overlay, tint: tint.ToVector4(), overlay: overlay.ToVector4());

    /// <summary>
    /// Creates a <see cref="ColorTransformation"/> that tints the fragments with <paramref name="tint"/>
    /// </summary>
    /// <param name="tint">The color to tint the fragments with</param>
    /// <returns>The created <see cref="ColorTransformation"/></returns>
    public static ColorTransformation CreateTint(Vector4 tint)
        => new(ColorEffect.Tinted | ColorEffect.GrayScale, tint: tint);

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
