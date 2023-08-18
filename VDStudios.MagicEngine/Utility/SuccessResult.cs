using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace VDStudios.MagicEngine.Utility;

/// <summary>
/// Provides an abstraction over a result that may or may not fail without necessarily raising an exception, and other, more idiomatic alternatives (such as <c><see cref="bool"/> Method([<see cref="System.Diagnostics.CodeAnalysis.NotNullWhenAttribute"/>(<see langword="true"/>)] <see langword="out"/> <see cref="object"/> result)</c> are unavailable
/// </summary>
/// <remarks>
/// Most useful for asynchronous methods that would benefit of the above pattern
/// </remarks>
/// <typeparam name="T">The type of the result</typeparam>
public readonly struct SuccessResult<T>
{
    /// <summary>
    /// Constructs a <see cref="SuccessResult{T}"/>
    /// </summary>
    /// <remarks>
    /// If the result is supposed to be a failure, use <see cref="Failure"/> instead
    /// </remarks>
    public SuccessResult(T result, bool success)
    {
        Result = result;
        IsSuccess = success;
        if (success && Result is null)
            throw new ArgumentException("Cannot create a Succesful SuccessResult with a null result", nameof(success));
    }

    /// <summary>
    /// A <see cref="SuccessResult{T}"/> that holds no result and simply represents a failure
    /// </summary>
    public static SuccessResult<T> Failure => default;

    /// <summary>
    /// The result of the operation
    /// </summary>
    /// <remarks>
    /// Not null if <see cref="IsSuccess"/> is true
    /// </remarks>
    public T Result { get; }

    /// <summary>
    /// Whether or not the operation that created this object was succesful
    /// </summary>
    [MemberNotNullWhen(true, nameof(Result))]
    public bool IsSuccess { get; }

    /// <summary>
    /// Attempts to get the result of the operation
    /// </summary>
    public bool TryGetResult([NotNullWhen(true)][MaybeNullWhen(false)] out T result)
    {
        if (IsSuccess)
        {
            Debug.Assert(Result is not null, "Result is null despite IsSuccess being true");
            result = Result;
            return true;
        }

        result = default;
        return false;
    }
}
