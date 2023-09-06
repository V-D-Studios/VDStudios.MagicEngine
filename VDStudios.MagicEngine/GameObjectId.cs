using System.Buffers.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents an unique Id given to every <see cref="GameObject"/> for varied purposes, primarily debugging and logging
/// </summary>
[StructLayout(LayoutKind.Explicit)]
[DebuggerDisplay("Id {ToString()} - Raw {Raw}")]
public readonly struct GameObjectId : IEquatable<GameObjectId>, ISpanParsable<GameObjectId>
{
    const string IdCharpool = "0A1B2C3D4E5H6G7U8J9KMPQRSTVWXYZF";
    const int StringLength = 16;

    /// <summary>
    /// Creates a new <see cref="GameObjectId"/>
    /// </summary>
    /// <returns></returns>
    public static unsafe GameObjectId NewId() 
        => new(DateTime.Now);

    internal GameObjectId(ulong raw)
        => Raw = raw;

    private unsafe GameObjectId(DateTime dt)
    {
        TimeStamp = (uint)(dt - DateTime.UnixEpoch).TotalMilliseconds;
        RandomNumberGenerator.Fill(MemoryMarshal.Cast<uint, byte>(new Span<uint>(ref salt)));
    }

    [FieldOffset(0)]
    internal readonly ulong Raw;

    /// <summary>
    /// A Random Salt value for this <see cref="GameObject"/>
    /// </summary>
    public uint Salt => salt;
    [FieldOffset(0)] private readonly uint salt;

    /// <summary>
    /// The Timestamp of this Id. <c>(<see cref="DateTime.Now"/> - <see cref="DateTime.UnixEpoch"/>)</c> -> <see cref="TimeSpan.TotalSeconds"/>
    /// </summary>
    [field: FieldOffset(4)]
    public uint TimeStamp { get; }

    /// <inheritdoc/>
    public override string ToString()
    {
        if (BitConverter.IsLittleEndian is false )
            throw new NotSupportedException("BigEndian is not supported. Figure out how it looks against LittleEndian and just flip the bytes in this method or smth. Library error, contact author.");

        Span<char> chars = stackalloc char[StringLength];
        chars[0] = 'x';
        chars[1] = ':';

        int i = 2;
        var v = Raw;
        while (v > 0)
        {
            chars[i++] = IdCharpool[(int)(v % (ulong)IdCharpool.Length)];
            v /= (ulong)IdCharpool.Length;
        }

        var i2 = StringLength - i + 2;
        if (i2 == 2) return new string(chars);

        chars[2..^i].CopyTo(chars[i2..]);
        for (; i2 < i2 - i; i2++) chars[i2] = '0';

        return new string(chars);
    }

    /// <inheritdoc/>
    public readonly bool Equals(GameObjectId other)
        => Raw == other.Raw;

    /// <inheritdoc/>
    public override bool Equals(object? obj) 
        => obj is GameObjectId id && Equals(id);

    /// <inheritdoc/>
    public static bool operator ==(GameObjectId left, GameObjectId right) 
        => left.Equals(right);

    /// <inheritdoc/>
    public static bool operator !=(GameObjectId left, GameObjectId right) 
        => !(left == right);

    /// <inheritdoc/>
    public override int GetHashCode()
        => Raw.GetHashCode();

    private static bool CheckFormat(ref ReadOnlySpan<char> s)
    {
        if (s.StartsWith("x:", StringComparison.OrdinalIgnoreCase))
            s = s[2..];

        if (s.Length is not StringLength - 2)
            return false;

        for (int i = 0; i < StringLength - 2; i++)
            if (IdCharpool.Contains(s[i]) is false)
                return false;

        return true;
    }

    private static GameObjectId ParseId(ReadOnlySpan<char> s)
    {
        throw new NotImplementedException();
        ulong x = 0;
        for (int i = s.Length - 1; i >= 0; i--)
        {
            var indx = (ulong)IdCharpool.IndexOf(s[i]);
            x = (x + indx) * (ulong)IdCharpool.Length;
        }
        return new GameObjectId(x);
    }

    /// <inheritdoc/>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out GameObjectId result)
    {
        if (CheckFormat(ref s) is false)
        {
            result = default;
            return false;
        }

        result = ParseId(s);
        return true;
    }

    /// <inheritdoc/>
    public static GameObjectId Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null) 
        => CheckFormat(ref s) is false ? throw new FormatException("Argument s was in an incorrect format") : ParseId(s);

    /// <summary>
    /// Tries to parse a span of characters into a value.
    /// </summary>
    /// <param name="s">The span of characters to parse.</param>
    /// <param name="result">When this method returns, contains the result of successfully parsing <paramref name="s"/>, or an undefined value on failure.</param>
    /// <returns><see langword="true"/> if <paramref name="s"/> was successfully parsed; otherwise, <see langword="false"/>.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, [MaybeNullWhen(false)] out GameObjectId result)
        => TryParse(s, null, out result);

    /// <inheritdoc/>
    public static GameObjectId Parse(string s, IFormatProvider? provider = null)
        => Parse(s.AsSpan(), provider);

    /// <inheritdoc/>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out GameObjectId result)
        => TryParse(s.AsSpan(), provider, out result);

    /// <summary>
    /// Tries to parse a span of characters into a value.
    /// </summary>
    /// <param name="s">The span of characters to parse.</param>
    /// <param name="result">When this method returns, contains the result of successfully parsing <paramref name="s"/>, or an undefined value on failure.</param>
    /// <returns><see langword="true"/> if <paramref name="s"/> was successfully parsed; otherwise, <see langword="false"/>.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, [MaybeNullWhen(false)] out GameObjectId result)
        => TryParse(s, null, out result);
}
