using VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes.Interfaces;

namespace VDStudios.MagicEngine.Graphics.Veldrid.DrawOperations;

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
    /// Obtains the amount of output sets this generator will generate
    /// </summary>
    /// <remarks>
    /// For example, if this method is fed a polygon with 5 <typeparamref name="TInputVertex"/>s, and returns a '2', then an output span for 2 sets of 5 <typeparamref name="TGraphicsVertex"/>s will be generated, that is, 10 total <typeparamref name="TGraphicsVertex"/>s
    /// </remarks>
    /// <param name="input">The span that contains the <typeparamref name="TInputVertex"/> instances</param>
    /// <returns>The amount of sets of <typeparamref name="TGraphicsVertex"/> whose length are equivalent to <paramref name="input"/> that this generator is expected to fill</returns>
    public uint GetOutputSetAmount(ReadOnlySpan<TInputVertex> input);

    /// <summary>
    /// Generates a set of <typeparamref name="TGraphicsVertex"/> into <paramref name="output"/> from <paramref name="input"/>
    /// </summary>
    /// <param name="input">The span that contains the <typeparamref name="TInputVertex"/> instances</param>
    /// <param name="output">The span that will contain the generated <typeparamref name="TGraphicsVertex"/> instances</param>
    public void Generate(ReadOnlySpan<TInputVertex> input, Span<TGraphicsVertex> output);
}
