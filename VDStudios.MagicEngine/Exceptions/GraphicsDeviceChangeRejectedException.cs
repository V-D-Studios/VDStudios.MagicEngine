namespace VDStudios.MagicEngine.Exceptions;

/// <summary>
/// An exception that is thrown when a <see cref="Node"/> or <see cref="Scene"/> rejects the attaching of a child <see cref="Node"/>
/// </summary>
[Serializable]
public class GraphicsDeviceChangeRejectedException : Exception
{
    /// <summary>
    /// Instances and describes a new <see cref="GraphicsDeviceChangeRejectedException"/>
    /// </summary>
    /// <param name="reason">The reason the <see cref="GraphicsDevice"/> change was rejected</param>
    public GraphicsDeviceChangeRejectedException(string? reason)
        : base($"This GraphicsManager rejected its GraphicsDevice (Renderer) being changed{(reason is null ? "" : $": {reason}")}")
    { }

    /// <inheritdoc/>
    protected GraphicsDeviceChangeRejectedException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}