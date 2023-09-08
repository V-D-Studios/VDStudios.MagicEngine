using System.Numerics;

namespace VDStudios.MagicEngine.Graphics.Veldrid;

/// <summary>
/// Provides a variety of helper methods to manage GPU related vertices, buffer indices, etc
/// </summary>
public static class GPUHelpers
{
    /// <summary>
    /// The maximum amount of vertices a model can have when using 16 bit indices
    /// </summary>
    public const int MaxUInt16Vertices = 21845;

    private static void ThrowIfTooLarge(int count)
    {
        if (count >= MaxUInt16Vertices)
            throw new NotSupportedException($"Triangulating indices for shapes with {MaxUInt16Vertices} or more vertices is not supported! The shape in question has {count} vertices. The ints used for indices are 16 bits wide, and switching to 32 bits is not supported yet");
    }

    private static void GenerateFoldingPaperIndices<TInt>(TInt count, Span<TInt> indexBuffer, TInt step, byte start = 0) where TInt : unmanaged, IBinaryInteger<TInt>
    {
        int bufind = 0;
        TInt i = TInt.CreateSaturating(start);

        var add = TInt.CreateSaturating(2);
        for (; i < count; i += add)
        {
            indexBuffer[bufind++] = TInt.One;
            indexBuffer[bufind++] = i;
            indexBuffer[bufind++] = count;
        }
    }
}
