using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDStudios.MagicEngine.DrawLibrary;

namespace VDStudios.MagicEngine;

/// <summary>
/// Provides extensions and helper methods to help in the binary reading/writing and serialization/deserialization of <b><see langword="unmanaged"/></b> <see langword="struct"/>s
/// </summary>
/// <remarks>
/// Only <see langword="unmanaged"/> types are permitted, as this is not serialization, but rather reading/writing of RAW binary data. <see langword="unsafe"/> types are not recommended, as pointers may become corrupt
/// </remarks>
public static class StructBytes
{
    #region Generic

    /// <summary>
    /// Reads the bytes that make up <paramref name="obj"/> and writes them into <paramref name="output"/>
    /// </summary>
    /// <typeparam name="T">The <b><see langword="unmanaged"/></b> type of <paramref name="obj"/></typeparam>
    /// <param name="obj">The object whose bytes will be read</param>
    /// <param name="output">The stream into which the bytes will be written</param>
    public static unsafe void WriteBytesInto<T>(in T obj, Stream output) where T : unmanaged
    {
        fixed (T* dptr = &obj)
        {
            Span<byte> dbytes = new(dptr, sizeof(T));
            output.Write(dbytes);
        }
    }

    /// <summary>
    /// Reads the bytes that make up <paramref name="obj"/> into <paramref name="output"/>
    /// </summary>
    /// <typeparam name="T">The <b><see langword="unmanaged"/></b> type of <paramref name="obj"/></typeparam>
    /// <param name="obj">The object whose bytes will be read</param>
    /// <param name="output">The span into which the output bytes will be written</param>
    /// <returns>The amount of bytes written into <paramref name="output"/></returns>
    public static unsafe int TryWriteBytesInto<T>(in T obj, Span<byte> output) where T : unmanaged
    {
        fixed (T* dptr = &obj)
        {
            Span<byte> dbytes = new(dptr, sizeof(T));
            int i = 0;
            for (; i < int.Min(output.Length, dbytes.Length); i++) output[i] = dbytes[i];
            return i;
        }
    }

    /// <summary>
    /// Attempts to read the bytes in <paramref name="input"/> into a new instance of <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">The <b><see langword="unmanaged"/></b> type of <paramref name="result"/></typeparam>
    /// <param name="input">The span from which the bytes will be read to be written into the new instance of <typeparamref name="T"/></param>
    /// <param name="result">The resulting new instance of <typeparamref name="T"/></param>
    /// <returns><see langword="true"/> if the operation is succesful, i.e. <paramref name="input"/> has enough bytes to be read into a new instance of <typeparamref name="T"/></returns>
    public static unsafe bool TryReadBytesFrom<T>(ReadOnlySpan<byte> input, [MaybeNullWhen(false)] out T result) where T : unmanaged
    {
        if (input.Length < sizeof(T))
        {
            result = default;
            return false;
        }

        fixed (T* dptr = &result)
        {
            Span<byte> dbytes = new(dptr, sizeof(T));
            for (int i = 0; i < sizeof(T); i++) dbytes[i] = input[i];
        }

        return true;
    }

    /// <summary>
    /// Attempts to read the bytes in <paramref name="input"/> into a new instance of <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">The <b><see langword="unmanaged"/></b> type of the expected result></typeparam>
    /// <param name="input">The stream from which the bytes will be read to be written into the new instance of <typeparamref name="T"/></param>
    /// <returns>The resulting new instance of <typeparamref name="T"/></returns>
    public static unsafe T ReadBytesFrom<T>(Stream input) where T : unmanaged
    {
        T result = default;
        Span<byte> dbytes = new(&result, sizeof(T));
        input.Read(dbytes);
        return result;
    }

    #endregion

    #region VarisizeGlyphAtlasTextRenderer.GlyphDefinition

    /// <summary>
    /// Reads the bytes that make up <paramref name="definition"/> and writes them into <paramref name="output"/>
    /// </summary>
    /// <remarks>
    /// Use <see cref="ReadBytesFrom{T}(Stream)"/> to read the information back into an object
    /// </remarks>
    /// <param name="definition">The object whose bytes will be read</param>
    /// <param name="output">The stream into which the bytes will be written</param>
    public static unsafe void WriteBytesInto(in this VarisizeGlyphAtlasTextRenderer.GlyphDefinition definition, Stream output)
        => WriteBytesInto<VarisizeGlyphAtlasTextRenderer.GlyphDefinition>(definition, output);

    /// <summary>
    /// Reads the bytes that make up <paramref name="definition"/> and writes them into <paramref name="output"/>
    /// </summary>
    /// <remarks>
    /// Use <see cref="TryReadBytesFrom{T}(ReadOnlySpan{byte}, out T)"/> to read the information back into an object
    /// </remarks>
    /// <param name="definition">The object whose bytes will be read</param>
    /// <param name="output">The span into which the output bytes will be written</param>
    /// <returns>The amount of bytes written into <paramref name="output"/></returns>
    public static unsafe int TryWriteBytesInto(in this VarisizeGlyphAtlasTextRenderer.GlyphDefinition definition, Span<byte> output)
        => TryWriteBytesInto<VarisizeGlyphAtlasTextRenderer.GlyphDefinition>(definition, output);

    #endregion
}
