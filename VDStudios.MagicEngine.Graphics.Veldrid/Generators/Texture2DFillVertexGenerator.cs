using System.Numerics;
using VDStudios.MagicEngine.Graphics.Veldrid.DrawOperations;
using VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes;

namespace VDStudios.MagicEngine.Graphics.Veldrid.Generators;

/// <summary>
/// A <see cref="IVertexGenerator{TInputVertex, TGraphicsVertex}"/> that creates vertex information to fill a Texture 
/// </summary>
public class Texture2DFillVertexGenerator : IVertexGenerator<Vector2, VertexTextureColor2D>
{
    /// <summary>
    /// The default instance of <see cref="Texture2DFillVertexGenerator"/>
    /// </summary>
    /// <remarks>
    /// Be careful when using this instance, as changing either <see cref="ColorFunction"/> or <see cref="DefaultColor"/> may unexpectedly affect any vertex information that is created or regenerated after
    /// </remarks>
    public static Texture2DFillVertexGenerator Default { get; } = new();

    /// <summary>
    /// A function to generate a color each vertex, ignored if <see langword="null"/>
    /// </summary>
    /// <remarks>
    /// The function's paramters are: arg1: polygon position; arg2: texture coordinate
    /// </remarks>
    public Func<Vector2, Vector2, RgbaVector>? ColorFunction { get; set; }

    /// <summary>
    /// The default color used when describing the color of a vertex
    /// </summary>
    public RgbaVector DefaultColor { get; set; }

    /// <inheritdoc/>
    public void Generate(ReadOnlySpan<Vector2> input, Span<VertexTextureColor2D> output)
    {
        if (input.Length != output.Length)
            throw new ArgumentException("input and output spans length mismatch", nameof(input));

        Vector2 distant = default;
        for (int i = 0; i < output.Length; i++)
            distant = Vector2.Max(distant, Vector2.Abs(input[i]));
        Matrix3x2 trans = Matrix3x2.CreateScale(1 / distant.X, 1 / distant.Y);

        if (ColorFunction is Func<Vector2, Vector2, RgbaVector> colorFunction)
        {
            for (int i = 0; i < output.Length; i++)
            {
                var tex = Vector2.Transform(input[output.Length - 1 - i], trans);
                var pol = input[i];
                output[i] = new(pol, tex, colorFunction(pol, tex));
            }
            return;
        }

        var color = DefaultColor;
        for (int i = 0; i < output.Length; i++)
            output[i] = new(input[i], Vector2.Transform(input[output.Length - 1 - i], trans), color);
    }
}
