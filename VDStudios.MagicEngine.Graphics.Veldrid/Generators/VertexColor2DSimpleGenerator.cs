using System.Numerics;
using VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes;

namespace VDStudios.MagicEngine.Graphics.Veldrid.Generators;

/// <summary>
/// A <see cref="IVertexGenerator{TInputVertex, TGraphicsVertex}"/> that injects a single color into every instance of <see cref="VertexColor2D"/> it generates from position info
/// </summary>
public sealed class VertexColor2DSimpleGenerator : IVertexGenerator<Vector2, VertexColor2D>
{
    /// <summary>
    /// Creates a new instance of type <see cref="VertexColor2DSimpleGenerator"/>
    /// </summary>
    public VertexColor2DSimpleGenerator() { }

    /// <summary>
    /// The default instance of <see cref="VertexColor2DSimpleGenerator"/>
    /// </summary>
    /// <remarks>
    /// Be careful when using this instance, as changing either <see cref="ColorFunction"/> or <see cref="DefaultColor"/> may unexpectedly affect any vertex information that is created or regenerated after
    /// </remarks>
    public static VertexColor2DSimpleGenerator Default { get; } = new();

    /// <summary>
    /// A function to generate a color each vertex, ignored if <see langword="null"/>
    /// </summary>
    /// <remarks>
    /// The function's paramters are: arg1: polygon position, arg2: vertex count
    /// </remarks>
    public Func<Vector2, int, RgbaVector>? ColorFunction { get; set; }

    /// <summary>
    /// The default color used when describing the color of a vertex
    /// </summary>
    public RgbaVector DefaultColor { get; set; } = RgbaVector.White;

    /// <inheritdoc/>
    public void Generate(ReadOnlySpan<Vector2> input, Span<VertexColor2D> output)
    {
        if (input.Length != output.Length)
            throw new ArgumentException("input and output length are mismatched", nameof(input));

        if (ColorFunction is Func<Vector2, int, RgbaVector> func)
        {
            for (int i = 0; i < input.Length; i++)
            {
                var vec = input[i];
                output[i] = new VertexColor2D(vec, func(vec, input.Length));
            }
        }
        else
            for (int i = 0; i < input.Length; i++)
                output[i] = new VertexColor2D(input[i], DefaultColor);
    }

    /// <inheritdoc/>
    public uint GetOutputSetAmount(ReadOnlySpan<Vector2> input) => 1;
}
