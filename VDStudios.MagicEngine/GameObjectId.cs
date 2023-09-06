using System.Buffers.Text;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents an unique Id given to every <see cref="GameObject"/> for varied purposes, primarily debugging and logging
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public readonly struct GameObjectId : IEquatable<GameObjectId>
{
    /// <summary>
    /// Creates a new <see cref="GameObjectId"/>
    /// </summary>
    /// <returns></returns>
    public static unsafe GameObjectId NewId() 
        => new(DateTime.Now);

    private unsafe GameObjectId(DateTime dt)
    {
        TimeStamp = (uint)(dt - DateTime.UnixEpoch).TotalSeconds;
        RandomNumberGenerator.Fill(MemoryMarshal.Cast<uint, byte>(new Span<uint>(ref salt)));
    }

    [field: FieldOffset(0)]
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
        if (BitConverter.IsLittleEndian is false)
            throw new NotSupportedException("BigEndian is not supported. Figure out how it looks against LittleEndian and just flip the bytes in this method or smth. Library error, contact author.");

        const string charpool = "0123456789ABCDEFGHJKLMNPQRSTWXYZ";
        Span<char> chars = stackalloc char[15];
        chars[0] = 'x';
        chars[1] = ':';
        int i = 2;
        var v = Raw;
        while (v > 0)
        {
            chars[i++] = charpool[(int)(v % (ulong)charpool.Length)];
            v /= (ulong)charpool.Length;
        }

        for (; i < chars.Length; i++) chars[i] = 'ñ';

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
}
