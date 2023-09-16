using System.Numerics;
using VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes;

namespace VDStudios.MagicEngine.Graphics.Veldrid.Generators;

/// <summary>
/// A <see cref="IVertexGenerator{TInputVertex, TGraphicsVertex}"/> that creates <see cref="Vertex2D"/> instances directly out of a <see cref="Vector2"/> without any modification
/// </summary>
/// <remarks>
/// This is a singleton class, and its instance can be accessed through <see cref="Instance"/>
/// </remarks>
public sealed class Vertex2DSimpleGenerator : IVertexGenerator<Vector2, Vertex2D>
{
    private Vertex2DSimpleGenerator() { }

    /// <summary>
    /// The singleton instance of <see cref="Vertex2DSimpleGenerator"/>
    /// </summary>
    public static Vertex2DSimpleGenerator Instance { get; } = new();

    /// <inheritdoc/>
    public void Generate(ReadOnlySpan<Vector2> input, Span<Vertex2D> output)
    {
        if (input.Length != output.Length)
            throw new ArgumentException("input and output length are mismatched", nameof(input));

        for (int i = 0; i < input.Length; i++)
                output[i] = new Vertex2D(input[i]);
    }

    /// <inheritdoc/>
    public uint GetOutputSetAmount(ReadOnlySpan<Vector2> input) => 1;
}
