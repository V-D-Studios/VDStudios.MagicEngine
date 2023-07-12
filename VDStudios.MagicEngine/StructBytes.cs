using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SharpGen.Runtime;
using VDStudios.MagicEngine.DrawLibrary;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
    /// Reads the bytes that make up every element of <paramref name="array"/> and writes them into <paramref name="output"/>
    /// </summary>
    /// <typeparam name="T">The <b><see langword="unmanaged"/></b> type of the elements in <paramref name="array"/></typeparam>
    /// <param name="array">The array containing the objects whose bytes will be read</param>
    /// <param name="output">The stream into which the bytes will be written</param>
    public static unsafe void WriteBytesInto<T>(T[] array, Stream output) where T : unmanaged
    {
        var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
        try
        {
            T* dptr = (T*)Unsafe.AsPointer(ref array[0]);
            Span<byte> dbytes = new(dptr, sizeof(T) * array.Length);
            output.Write(dbytes);
        }
        finally
        {
            handle.Free();
        }
    }

    /// <summary>
    /// Reads the bytes that make up every element of <paramref name="array"/> into <paramref name="output"/>
    /// </summary>
    /// <typeparam name="T">The <b><see langword="unmanaged"/></b> type of the elements in <paramref name="array"/></typeparam>
    /// <param name="array">The array containing the objects whose bytes will be read</param>
    /// <param name="output">The span into which the output bytes will be written</param>
    /// <returns>The amount of bytes written into <paramref name="output"/></returns>
    public static unsafe int TryWriteBytesInto<T>(T[] array, Span<byte> output) where T : unmanaged
    {
        var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
        try
        {
            T* dptr = (T*)Unsafe.AsPointer(ref array[0]);
            int i = 0;
            if (sizeof(T) % sizeof(nint) == 0 && output.Length % sizeof(nint) == 0)
            {
                Span<nint> dbytes = new(dptr, (sizeof(T) / sizeof(nint)) * array.Length);
                Span<nint> outp = new(Unsafe.AsPointer(ref output[0]), output.Length / sizeof(nint));
                for (; i < int.Min(output.Length, dbytes.Length); i++)
                    outp[i] = dbytes[i];
                i *= sizeof(nint);
            }
            else
            {
                Span<byte> dbytes = new(dptr, sizeof(T) * array.Length);
                for (; i < int.Min(output.Length, dbytes.Length); i++)
                    output[i] = dbytes[i];
            }
            return i;
        }
        finally
        {
            handle.Free();
        }
    }

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
            int i = 0;
            if (sizeof(T) % sizeof(nint) == 0 && output.Length % sizeof(nint) == 0)
            {
                Span<nint> dbytes = new(dptr, sizeof(T) / sizeof(nint));
                Span<nint> outp = new(Unsafe.AsPointer(ref output[0]), output.Length / sizeof(nint));
                for (; i < int.Min(output.Length, dbytes.Length); i++)
                    outp[i] = dbytes[i];
                i *= sizeof(nint);
            }
            else
            {
                Span<byte> dbytes = new(dptr, sizeof(T));
                for (; i < int.Min(output.Length, dbytes.Length); i++)
                    output[i] = dbytes[i];
            }
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
            if (sizeof(T) % sizeof(nint) == 0 && input.Length % sizeof(nint) == 0)
            {
                Span<nint> dbytes = new(dptr, sizeof(T) / sizeof(nint));
                Span<nint> inp = new(Unsafe.AsPointer(ref Unsafe.AsRef(input[0])), input.Length / sizeof(nint));
                for (int i = 0; i < dbytes.Length; i++)
                    dbytes[i] = inp[i];
            }
            else
            {
                Span<byte> dbytes = new(dptr, sizeof(T));
                for (int i = 0; i < int.Min(input.Length, dbytes.Length); i++)
                    dbytes[i] = input[i];
            }
        }

        return true;
    }

    /// <summary>
    /// Attempts to read the bytes in <paramref name="input"/> into new instances of <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">The <b><see langword="unmanaged"/></b> type of the elements in<paramref name="output"/></typeparam>
    /// <param name="input">The span from which the bytes will be read to be written into the new instance of <typeparamref name="T"/></param>
    /// <param name="output">The array containing the resulting elements</param>
    /// <returns><see langword="true"/> if the operation is succesful, i.e. <paramref name="input"/> has exactly enough bytes to construct one or more objects of type <typeparamref name="T"/></returns>
    public static unsafe bool TryReadBytesFrom<T>(ReadOnlySpan<byte> input, T[] output) where T : unmanaged
        => TryReadBytesFrom(input, output.AsSpan());

    /// <summary>
    /// Attempts to read the bytes in <paramref name="input"/> into new instances of <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">The <b><see langword="unmanaged"/></b> type of the elements in<paramref name="output"/></typeparam>
    /// <param name="input">The span from which the bytes will be read to be written into the new instance of <typeparamref name="T"/></param>
    /// <param name="output">The span containing the resulting elements</param>
    /// <returns><see langword="true"/> if the operation is succesful, i.e. <paramref name="input"/> has exactly enough bytes to construct one or more objects of type <typeparamref name="T"/></returns>
    public static unsafe bool TryReadBytesFrom<T>(ReadOnlySpan<byte> input, Span<T> output) where T : unmanaged
    {
        if (input.Length % sizeof(T) != 0)
            return false;

        fixed (T* dptr = &output[0])
        {
            if (sizeof(T) % sizeof(nint) == 0 && input.Length % sizeof(nint) == 0)
            {
                Span<nint> dbytes = new(dptr, (sizeof(T) / sizeof(nint)) * output.Length);
                Span<nint> inp = new(Unsafe.AsPointer(ref Unsafe.AsRef(input[0])), input.Length / sizeof(nint));
                for (int i = 0; i < dbytes.Length; i++)
                    dbytes[i] = inp[i];
            }
            else
            {
                Span<byte> dbytes = new(dptr, sizeof(T) * output.Length);
                for (int i = 0; i < int.Min(input.Length, dbytes.Length); i++)
                    dbytes[i] = input[i];
            }
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
}
