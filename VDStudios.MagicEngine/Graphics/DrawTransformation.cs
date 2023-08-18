using System.Numerics;

namespace VDStudios.MagicEngine.Graphics;

/// <summary>
/// Provides parameters and other useful data to be used when drawing in a <see cref="DrawOperation{TGraphicsContext}"/>
/// </summary>
/// <param name="View">The transformation Matrix that represents the object in the world from the viewpoint</param>
/// <param name="Projection">The transformation Matrix that represents the object in the viewpoint from the screen</param>
public readonly record struct DrawTransformation(Matrix4x4 View, Matrix4x4 Projection);
