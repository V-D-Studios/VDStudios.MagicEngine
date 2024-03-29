﻿using System.Runtime.CompilerServices;

namespace VDStudios.MagicEngine.Utility;

/// <summary>
/// Represents a relative or absolute amount of elements to skip in an indexed collection
/// </summary>
public readonly struct ElementSkip : IEquatable<ElementSkip>
{
    private enum ElementSkipMode : byte
    {
        Default = 0,
        PercentageToSkip,
        PercentageToMaintain,
        AmountToMaintain
    }

    private ElementSkip(float percentageToSkip, int amount, ElementSkipMode mode)
    {
        Percentage = percentageToSkip;
        Amount = amount;
        Mode = mode;
    }

    private readonly float Percentage;
    private readonly int Amount;
    private readonly ElementSkipMode Mode;

    /// <summary>
    /// Creates an <see cref="ElementSkip"/> that will skip <paramref name="percentage"/>% of the elements in a given collection, spread throughout it
    /// </summary>
    /// <param name="percentage">A value between 0.0 (0%) to 1.0 (100%)</param>
    /// <returns></returns>
    public static ElementSkip PercentToSkip(float percentage)
        => new(CheckAndThrowIfOutOfRange(percentage), 0, ElementSkipMode.PercentageToSkip);

    /// <summary>
    /// Creates an <see cref="ElementSkip"/> that will skip all but <paramref name="percentage"/>% of the elements in a given collection, spread throughout it
    /// </summary>
    /// <param name="percentage">A value between 0.0 (0%) to 1.0 (100%)</param>
    /// <returns></returns>
    public static ElementSkip PercentToMaintain(float percentage)
        => new(CheckAndThrowIfOutOfRange(percentage), 0, ElementSkipMode.PercentageToMaintain);

    /// <summary>
    /// Creates an <see cref="ElementSkip"/> that will skip all but <paramref name="maintain"/> elements throughout a given collection
    /// </summary>
    /// <param name="maintain">The amount of elements to maintain in a given collection</param>
    public static ElementSkip ElementsToMaintain(int maintain)
        => new(0, CheckAndThrowIfOutOfRange(maintain), ElementSkipMode.AmountToMaintain);

    /// <summary>
    /// Computes the value to multiply with an index to only access the amount of elements represented by this object throughout the collection
    /// </summary>
    /// <param name="length">The length of the collection</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetSkipFactor(int length)
        => int.Max(1, Mode switch
        {
            ElementSkipMode.Default => 1,
            ElementSkipMode.AmountToMaintain => length / Amount,
            ElementSkipMode.PercentageToSkip => length / (int)(length * (1f - Percentage)),
            ElementSkipMode.PercentageToMaintain => length / (int)(length * Percentage),
            _ => ThrowForUnknownMode()
        });

    /// <summary>
    /// Computes the amount of elements that will be read from the collection if <see cref="GetSkipFactor(int)"/>'s value is used
    /// </summary>
    /// <param name="length">The length of the collection</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetElementCount(int length)
        => int.Min(length, Mode switch
        {
            ElementSkipMode.Default => length,
            ElementSkipMode.AmountToMaintain => Amount,
            ElementSkipMode.PercentageToSkip => (int)(length * (1f - Percentage)),
            ElementSkipMode.PercentageToMaintain => (int)(length * Percentage),
            _ => ThrowForUnknownMode()
        });

    private int ThrowForUnknownMode() => throw new InvalidOperationException($"Unknown ElementSkipMode {Mode}; likely a library bug");
    private static float CheckAndThrowIfOutOfRange(float value)
        => value is < float.Epsilon or > (1f - float.Epsilon) ? throw new ArgumentOutOfRangeException(nameof(value), "value must be between 0.0 and 1.0") : value;
    private static int CheckAndThrowIfOutOfRange(int value)
        => value is < 0 ? throw new ArgumentOutOfRangeException(nameof(value), "value must be larger than 0") : value;

    /// <inheritdoc/>
    public bool Equals(ElementSkip other)
        => MathUtils.NearlyEqual(Percentage, other.Percentage)
            && Amount == other.Amount
            && Mode == other.Mode;

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is ElementSkip skip && Equals(skip);

    /// <inheritdoc/>
    public static bool operator ==(ElementSkip left, ElementSkip right)
        => left.Equals(right);

    /// <inheritdoc/>
    public static bool operator !=(ElementSkip left, ElementSkip right)
        => !(left == right);

    /// <inheritdoc/>
    public override int GetHashCode()
        => HashCode.Combine(Percentage, Amount, Mode);
}
