namespace VDStudios.MagicEngine.Utility;

/// <summary>
/// An <see cref="int"/> comparer that inverts the standard comparison
/// </summary>
/// <remarks>
/// Useful for Descending ordering
/// </remarks>
public sealed class InvertedIntComparer : IComparer<int>
{
    private InvertedIntComparer() { }

    /// <inheritdoc/>
    public int Compare(int x, int y) => x.CompareTo(y);

    /// <summary>
    /// The singleton instance of this comparer
    /// </summary>
    public static InvertedIntComparer Comparer { get; } = new();
}
