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
    /// Creates a new instance of type <see cref="Texture2DFillVertexGenerator"/>
    /// </summary>
    public Texture2DFillVertexGenerator(TexturedShape2DRenderer renderer)
    {
        Renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }
    
    /// <summary>
    /// The <see cref="TexturedShape2DRenderer"/> this <see cref="Texture2DFillVertexGenerator"/> belongs to
    /// </summary>
    public TexturedShape2DRenderer Renderer { get; }

    /// <inheritdoc/>
    public void Generate(ReadOnlySpan<Vector2> input, Span<VertexTextureColor2D> output)
    {
#error Not Implemented
        throw new NotImplementedException();
    }
}
