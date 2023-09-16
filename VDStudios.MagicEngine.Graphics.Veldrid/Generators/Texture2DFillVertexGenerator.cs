using System.Numerics;
using VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes;

namespace VDStudios.MagicEngine.Graphics.Veldrid.Generators;

/// <summary>
/// A <see cref="IVertexGenerator{TInputVertex, TGraphicsVertex}"/> that creates vertex information to fill a Texture 
/// </summary>
public class Texture2DFillVertexGenerator : IVertexGenerator<Vector2, TextureCoordinate2D>
{
    /// <summary>
    /// Instances a new object of type <see cref="Texture2DFillVertexGenerator"/>
    /// </summary>
    protected Texture2DFillVertexGenerator() { }

    /// <summary>
    /// The default instance of <see cref="Texture2DFillVertexGenerator"/>
    /// </summary>
    public static Texture2DFillVertexGenerator Default { get; } = new();

    /// <inheritdoc/>
    public void Generate(ReadOnlySpan<Vector2> input, Span<TextureCoordinate2D> output)
    {
        if (input.Length != output.Length)
            throw new ArgumentException("input and output spans length mismatch", nameof(input));

        Vector2 distant = default;
        for (int i = 0; i < output.Length; i++)
            distant = Vector2.Max(distant, Vector2.Abs(input[i]));
        Matrix3x2 trans = Matrix3x2.CreateScale(1 / distant.X, 1 / distant.Y);

        for (int i = 0; i < output.Length; i++)
            output[i] = new(Vector2.Transform(input[output.Length - 1 - i], trans));
    }

    /// <inheritdoc/>
    public uint GetOutputSetAmount(ReadOnlySpan<Vector2> input) => 1;
}
