namespace VDStudios.MagicEngine;

/// <summary>
/// A set of extensions to simplify and ensure safety on otherwise unsafe Size operations
/// </summary>
public static class SizeOf<T> where T : unmanaged
{
    unsafe static SizeOf()
    {
        Size = (uint)sizeof(T);
    }

    /// <summary>
    /// The size in bytes of <typeparamref name="T"/>
    /// </summary>
    public static readonly uint Size;

    /// <summary>
    /// Calculates the buffer size of a buffer holding onto <paramref name="count"/> instances of <typeparamref name="T"/>
    /// </summary>
    public static uint By(uint count) => Size * count;
}