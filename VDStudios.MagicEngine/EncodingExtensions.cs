using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine;

/// <summary>
/// Provides extensions and static methods to encode binary data in various string formats
/// </summary>
public static class EncodingExtensions
{
    static EncodingExtensions()
    {
        Ascii85DecodeMap = new Dictionary<char, byte>();

        for (byte i = 0; i < Ascii85EncodeMap.Length; i++)
            Ascii85DecodeMap.Add(Ascii85EncodeMap[i], i);

        //for (byte i = 0; i < Ascii64EncodeMap.Length; i++)
        //    Ascii64DecodeMap.Add(Ascii64EncodeMap[i], i);
    }

#if X

    #region Ascii 64

    private const string Ascii64EncodeMap =
        "0123456789" +
        "abcdefghijklmnñopqrstuvwxyz" +
        "ABCDEFGHIJKLMNÑOPQRSTUVWXYZ";

    private static readonly Dictionary<char, byte> Ascii64DecodeMap = new();

    /// <summary>
    /// Decodes an Ascii-64 encoded Guid.
    /// </summary>
    /// <param name="ascii64Encoding">The Guid encoded using Ascii-64.</param>
    /// <returns>A Guid decoded from the parameter.</returns>
    public static unsafe Guid DecodeAscii64(ReadOnlySpan<char> ascii64Encoding)
    {
        // Ascii-64 can encode 4 bytes of binary data into 5 bytes of Ascii.
        // Since a Guid is 16 bytes long, the Ascii-64 encoding should be 20
        // characters long.
        if (ascii64Encoding.Length != 20)
            throw new ArgumentException("An encoded Guid should be 20 characters long.", nameof(ascii64Encoding));

        // We only support upper case characters.

        Span<char> enc = stackalloc char[ascii64Encoding.Length];
        ascii64Encoding.ToUpper(enc, null);

        // Split the string in half and decode each substring separately.
        var higher = AsciiDecode(enc.Slice(0, 10), Ascii64EncodeMap, Ascii64DecodeMap);
        var lower = AsciiDecode(enc.Slice(10, 10), Ascii64EncodeMap, Ascii64DecodeMap);

        Span<byte> byteArray = stackalloc byte[]
        {
            (byte)((higher & 0xFF00000000000000) >> 56),
            (byte)((higher & 0x00FF000000000000) >> 48),
            (byte)((higher & 0x0000FF0000000000) >> 40),
            (byte)((higher & 0x000000FF00000000) >> 32),
            (byte)((higher & 0x00000000FF000000) >> 24),
            (byte)((higher & 0x0000000000FF0000) >> 16),
            (byte)((higher & 0x000000000000FF00) >> 8),
            (byte)((higher & 0x00000000000000FF)),
            (byte)((lower & 0xFF00000000000000) >> 56),
            (byte)((lower & 0x00FF000000000000) >> 48),
            (byte)((lower & 0x0000FF0000000000) >> 40),
            (byte)((lower & 0x000000FF00000000) >> 32),
            (byte)((lower & 0x00000000FF000000) >> 24),
            (byte)((lower & 0x0000000000FF0000) >> 16),
            (byte)((lower & 0x000000000000FF00) >> 8),
            (byte)((lower & 0x00000000000000FF)),
        };

        return new Guid(byteArray);
    }

    /// <summary>
    /// Encodes binary data into a plaintext Ascii-64 format string
    /// </summary>
    /// <param name="bytes">The bytes to encode</param>
    /// <param name="output">The output span in which to write the characters</param>
    /// <returns>Ascii-64 encoded string</returns>
    public static unsafe void EncodeAscii64(ReadOnlySpan<byte> bytes, Span<char> output)
    {
        if (output.Length < Ascii64GetSpanSize(bytes.Length))
            throw new ArgumentException($"output span cannot be shorter than {Ascii64GetSpanSize(bytes.Length)} for an input of length {bytes.Length}", nameof(output));

        var (partCount, q) = int.DivRem(bytes.Length, sizeof(ulong));
        ulong* pptr = stackalloc ulong[q > 0 ? partCount++ : partCount];
        Span<ulong> parts = new(pptr, partCount);
        parts.Clear();
        bytes.CopyTo(new Span<byte>(pptr, partCount * sizeof(ulong)));

        for (int i = 0; i < partCount; i++)
            AsciiEncode(output.Slice(i * 10, 10), parts[i], Ascii64EncodeMap);
    }

    /// <summary>
    /// Gets the appropriate buffer size for the output of an <see cref="EncodeAscii64(ReadOnlySpan{byte}, Span{char})"/> operation
    /// </summary>
    /// <param name="sizeInBytes">The size, in bytes, of the input</param>
    /// <returns>The appropriate size for the output span</returns>
    public static int Ascii64GetSpanSize(int sizeInBytes)
        => DataStructuring.FitToSize(sizeInBytes, 20);

    /// <summary>
    /// Encodes a guid into a plaintext Ascii-64 format string
    /// </summary>
    /// <param name="guid">The guid to encode</param>
    /// <returns>Ascii-64 encoded string</returns>
    public static unsafe string EncodeAscii64(this Guid guid)
    {
        Span<char> output = stackalloc char[Ascii64GetSpanSize(sizeof(Guid))];
        guid.EncodeAscii64(output);
        return new string(output);
    }

    /// <summary>
    /// Encodes a guid into a plaintext Ascii-64 format string
    /// </summary>
    /// <param name="guid">The guid to encode</param>
    /// <param name="output">The formatted characters of an Ascii-64 string</param>
    public static unsafe void EncodeAscii64(this Guid guid, Span<char> output)
        => EncodeAscii85(new ReadOnlySpan<byte>(&guid, sizeof(Guid)), output);

    #endregion

#endif

    #region Ascii85

    private const string Ascii85EncodeMap =
        "0123456789" +
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
        "|}~{!\"#$%&'()`*+-./[\\]^:" +
        ";<=>?@_¼½¾ßÇÐ€«»¿•Ø£†‡§¥";

    private static readonly Dictionary<char, byte> Ascii85DecodeMap;

    /// <summary>
    /// Decodes an Ascii-85 encoded Guid.
    /// </summary>
    /// <param name="ascii85Encoding">The Guid encoded using Ascii-85.</param>
    /// <returns>A Guid decoded from the parameter.</returns>
    public static unsafe Guid DecodeAscii85(ReadOnlySpan<char> ascii85Encoding)
    {
        // Ascii-85 can encode 4 bytes of binary data into 5 bytes of Ascii.
        // Since a Guid is 16 bytes long, the Ascii-85 encoding should be 20
        // characters long.
        if (ascii85Encoding.Length != 20)
            throw new ArgumentException("An encoded Guid should be 20 characters long.", nameof(ascii85Encoding));

        // We only support upper case characters.

        Span<char> enc = stackalloc char[ascii85Encoding.Length];
        ascii85Encoding.ToUpper(enc, null);

        // Split the string in half and decode each substring separately.
        var higher = AsciiDecode(enc.Slice(0, 10), Ascii85EncodeMap, Ascii85DecodeMap);
        var lower = AsciiDecode(enc.Slice(10, 10), Ascii85EncodeMap, Ascii85DecodeMap);

        Span<byte> byteArray = stackalloc byte[]
        {
            (byte)((higher & 0xFF00000000000000) >> 56),
            (byte)((higher & 0x00FF000000000000) >> 48),
            (byte)((higher & 0x0000FF0000000000) >> 40),
            (byte)((higher & 0x000000FF00000000) >> 32),
            (byte)((higher & 0x00000000FF000000) >> 24),
            (byte)((higher & 0x0000000000FF0000) >> 16),
            (byte)((higher & 0x000000000000FF00) >> 8),
            (byte)((higher & 0x00000000000000FF)),
            (byte)((lower  & 0xFF00000000000000) >> 56),
            (byte)((lower  & 0x00FF000000000000) >> 48),
            (byte)((lower  & 0x0000FF0000000000) >> 40),
            (byte)((lower  & 0x000000FF00000000) >> 32),
            (byte)((lower  & 0x00000000FF000000) >> 24),
            (byte)((lower  & 0x0000000000FF0000) >> 16),
            (byte)((lower  & 0x000000000000FF00) >> 8),
            (byte)((lower  & 0x00000000000000FF)),
        };

        return new Guid(byteArray);
    }

    /// <summary>
    /// Encodes binary data into a plaintext Ascii-85 format string
    /// </summary>
    /// <param name="bytes">The bytes to encode</param>
    /// <param name="output">The output span in which to write the characters</param>
    /// <returns>Ascii-85 encoded string</returns>
    public static unsafe void EncodeAscii85(ReadOnlySpan<byte> bytes, Span<char> output)
    {
        if (output.Length < Ascii85GetSpanSize(bytes.Length)) 
            throw new ArgumentException($"output span cannot be shorter than {Ascii85GetSpanSize(bytes.Length)} for an input of length {bytes.Length}", nameof(output));

        var (partCount, q) = int.DivRem(bytes.Length, sizeof(ulong));
        ulong* pptr = stackalloc ulong[q > 0 ? partCount++ : partCount];
        Span<ulong> parts = new(pptr, partCount);
        parts.Clear();
        bytes.CopyTo(new Span<byte>(pptr, partCount * sizeof(ulong)));

        for (int i = 0; i < partCount; i++)
            AsciiEncode(output.Slice(i * 10, 10), parts[i], Ascii85EncodeMap);
    }

    /// <summary>
    /// Gets the appropriate buffer size for the output of an <see cref="EncodeAscii85(ReadOnlySpan{byte}, Span{char})"/> operation
    /// </summary>
    /// <param name="sizeInBytes">The size, in bytes, of the input</param>
    /// <returns>The appropriate size for the output span</returns>
    public static int Ascii85GetSpanSize(int sizeInBytes)
        => DataStructuring.FitToSize(sizeInBytes, 20);

    /// <summary>
    /// Encodes a guid into a plaintext Ascii-85 format string
    /// </summary>
    /// <param name="guid">The guid to encode</param>
    /// <returns>Ascii-85 encoded string</returns>
    public static unsafe string EncodeAscii85(this Guid guid)
    {
        Span<char> output = stackalloc char[Ascii85GetSpanSize(sizeof(Guid))];
        guid.EncodeAscii85(output);
        return new string(output);
    }

    /// <summary>
    /// Encodes a guid into a plaintext Ascii-85 format string
    /// </summary>
    /// <param name="guid">The guid to encode</param>
    /// <param name="output">The formatted characters of an Ascii-85 string</param>
    public static unsafe void EncodeAscii85(this Guid guid, Span<char> output)
        => EncodeAscii85(new ReadOnlySpan<byte>(&guid, sizeof(Guid)), output);

    #endregion

    private static void AsciiEncode(Span<char> strBuffer, ulong part, string encodeMap)
    {
        if (strBuffer.Length < 10)
            throw new ArgumentException("The string buffer cannot have a size less than 10", nameof(strBuffer));

        // Nb, the most significant digits in our encoded character will 
        // be the right-most characters.
        var charCount = (uint)encodeMap.Length;

        // Ascii-85 can encode 4 bytes of binary data into 5 bytes of Ascii.
        // Since a UInt64 is 8 bytes long, the Ascii-85 encoding should be 
        // 10 characters long.
        for (var i = 0; i < 10; i++)
        {
            // Get the remainder when dividing by the base.
            var remainder = part % charCount;

            // Divide by the base.
            part /= charCount;

            // Add the appropriate character for the current value (0-84).
            strBuffer[i] = encodeMap[(int)remainder];
        }
    }

    private static ulong AsciiDecode(ReadOnlySpan<char> str, string encodeMap, Dictionary<char, byte> decodeMap)
    {
        if (str.Length != 10)
            throw new ArgumentException("An Ascii-85 encoded Uint64 should be 10 characters long.", nameof(str));

        // Nb, the most significant digits in our encoded character 
        // will be the right-most characters.
        var charCount = (uint)encodeMap.Length;
        ulong result = 0;

        // Starting with the right-most (most-significant) character, 
        // iterate through the encoded string and decode.
        for (var i = str.Length - 1; i >= 0; i--)
        {
            // Multiply the current decoded value by the base.
            result *= charCount;

            // Add the integer value for that encoded character.
            result += decodeMap[str[i]];
        }

        return result;
    }
}
