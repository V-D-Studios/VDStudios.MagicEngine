using SDL2.NET;

namespace VDStudios.MagicEngine.Exceptions;

/// <summary>
/// An exception that is thrown when a <see cref="GraphicsManager"/> rejects the changing of its <see cref="GraphicsManager.Window"/>
/// </summary>
[Serializable]
public class WindowChangeRejectedException : Exception
{
    /// <summary>
    /// Instances and describes a new <see cref="WindowChangeRejectedException"/>
    /// </summary>
    /// <param name="reason">The reason the <see cref="Window"/> change was rejected</param>
    public WindowChangeRejectedException(string? reason)
        : base($"This GraphicsManager rejected its Window being changed{(reason is null ? "" : $": {reason}")}")
    { }

    /// <inheritdoc/>
    protected WindowChangeRejectedException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
