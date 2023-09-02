using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents an unique Id given to every <see cref="GameObject"/> for varied purposes, primarily debugging and logging
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public readonly struct GameObjectId : IFormattable, IEquatable<GameObjectId>
{
    /// <summary>
    /// Creates a new <see cref="GameObjectId"/>
    /// </summary>
    /// <returns></returns>
    public static unsafe GameObjectId NewId()
    {
        Unsafe.SkipInit(out byte s);
        Span<byte> bytes = new(ref s);
        RandomNumberGenerator.Fill(bytes);
        var dt = DateTime.Now;
        return new((byte)dt.Hour, (byte)dt.Minute, (byte)dt.Second, s);
    }

    private GameObjectId(byte h, byte m, byte s, byte salt)
    {
        HourCreated = h;
        MinuteCreated = m;
        SecondCreated = s;
        Salt = salt;
    }

    [field: FieldOffset(0)]
    internal readonly uint Raw;
    /// <summary>
    /// The Hour at which this <see cref="GameObject"/> was created
    /// </summary>
    [field: FieldOffset(0)]
    public byte HourCreated { get; }

    /// <summary>
    /// The Minute at which this <see cref="GameObject"/> was created
    /// </summary>
    [field: FieldOffset(1)]
    public byte MinuteCreated { get; }

    /// <summary>
    /// The Second at which this <see cref="GameObject"/> was created
    /// </summary>
    [field: FieldOffset(2)]
    public byte SecondCreated { get; }

    /// <summary>
    /// A Random Salt value for this <see cref="GameObject"/>
    /// </summary>
    [field: FieldOffset(3)]
    public byte Salt { get; }

    /// <inheritdoc/>
    public readonly string ToString(string? format, IFormatProvider? formatProvider)
        => $"x:{Raw.ToString(format, formatProvider)}";

    /// <inheritdoc/>
    public override string ToString()
        => ToString("X", null);

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
