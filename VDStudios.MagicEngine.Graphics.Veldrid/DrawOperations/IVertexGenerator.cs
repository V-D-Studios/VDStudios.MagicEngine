using VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes.Interfaces;

namespace VDStudios.MagicEngine.Graphics.Veldrid.DrawOperations;
#warning NOTE: For multi-shape renderers, disallow changing the shape or adding more shapes

/// <summary>
/// Represents an object that is capable of generating a set of <typeparamref name="TGraphicsVertex"/> from a set of <typeparamref name="TInputVertex"/>
/// </summary>
/// <typeparam name="TGraphicsVertex">The graphics vertex info generated from a <typeparamref name="TInputVertex"/></typeparam>
/// <typeparam name="TInputVertex">The input data to generate <typeparamref name="TGraphicsVertex"/></typeparam>
public interface IVertexGenerator<TInputVertex, TGraphicsVertex>
    where TGraphicsVertex : unmanaged, IVertexType<TGraphicsVertex>
    where TInputVertex : unmanaged
{
    /// <summary>
    /// Generates a set of <typeparamref name="TGraphicsVertex"/> into <paramref name="output"/> from <paramref name="input"/>
    /// </summary>
    /// <param name="input">The span that contains the <typeparamref name="TInputVertex"/> instances</param>
    /// <param name="output">The span that will contain the generated <typeparamref name="TGraphicsVertex"/> instances</param>
    public void Generate(ReadOnlySpan<TInputVertex> input, Span<TGraphicsVertex> output);
}
