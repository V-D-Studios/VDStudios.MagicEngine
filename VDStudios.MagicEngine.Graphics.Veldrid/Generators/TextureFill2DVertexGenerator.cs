﻿using System.Numerics;
using VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes;

namespace VDStudios.MagicEngine.Graphics.Veldrid.Generators;

/// <summary>
/// A <see cref="IVertexGenerator{TInputVertex, TGraphicsVertex}"/> that creates vertex information to fill a Texture 
/// </summary>
public class TextureFill2DVertexGenerator : IVertexGenerator<Vector2, TextureCoordinate2D>
{
    /// <summary>
    /// Instances a new object of type <see cref="TextureFill2DVertexGenerator"/>
    /// </summary>
    protected TextureFill2DVertexGenerator() { }

    /// <summary>
    /// The default instance of <see cref="TextureFill2DVertexGenerator"/>
    /// </summary>
    public static TextureFill2DVertexGenerator Default { get; } = new();

    /// <inheritdoc/>
    public void Generate(ReadOnlySpan<Vector2> input, Span<TextureCoordinate2D> output)
    {
        if (input.Length != output.Length)
            throw new ArgumentException("input and output spans length mismatch", nameof(input));

        Vector2 offset = default;
        for (int i = 0; i < output.Length; i++)
            offset = Vector2.Min(input[i], offset);
        offset *= -1; // default is 0,0; leaving either 0 (resulting in 0) or only negative numbers to invert

        Vector2 distant = default;
        for (int i = 0; i < output.Length; i++)
            distant = Vector2.Max(distant, offset + input[i]);
        Matrix3x2 trans = Matrix3x2.CreateScale(1 / distant.X, 1 / distant.Y);

        for (int i = 0; i < output.Length; i++)
            output[i] = new(Vector2.Transform((offset - input[output.Length - 1 - i]) * -1, trans));
    }

    /// <inheritdoc/>
    public uint GetOutputSetAmount(ReadOnlySpan<Vector2> input) => 1;
}
