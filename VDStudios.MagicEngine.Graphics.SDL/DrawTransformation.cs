using System.Numerics;
using System.Runtime.InteropServices;

namespace VDStudios.MagicEngine.Graphics.SDL;

/// <summary>
/// Provides parameters and other useful data to be used when drawing in a <see cref="DrawOperation{TGraphicsContext}"/>
/// </summary>
/// <remarks>
/// This struct is laid out in memory in the following order: <see cref="View"/>, <see cref="Projection"/>, <see cref="ViewTranslation"/>, <see cref="ViewScale"/>. Packed in 4 bytes
/// </remarks>
/// <param name="View">The transformation Matrix that represents the object in the world from the viewpoint</param>
/// <param name="Projection">The transformation Matrix that represents the object in the viewpoint from the screen</param>
/// <param name="ViewScale">The scale across each axis in <paramref name="View"/></param>
/// <param name="ViewTranslation">The translation across each axis in <paramref name="View"/></param>
[StructLayout(LayoutKind.Sequential, Pack = sizeof(float))]
public readonly record struct DrawTransformation(Matrix4x4 View, Matrix4x4 Projection, Vector3 ViewTranslation, Vector3 ViewScale);
