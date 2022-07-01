namespace VDStudios.MagicEngine.Exceptions;

/// <summary>
/// An exception that is thrown when a <see cref="GraphicsManager"/> rejects the changing of its <see cref="GraphicsManager.CurrentScene"/>
/// </summary>
[Serializable]
public class SceneChangeRejectedException : Exception
{
    /// <summary>
    /// Instances and describes a new <see cref="SceneChangeRejectedException"/>
    /// </summary>
    /// <param name="reason">The reason the <see cref="Scene"/> change was rejected</param>
    public SceneChangeRejectedException(string? reason)
        : base($"This GraphicsManager rejected its Scene being changed{(reason is null ? "" : $": {reason}")}")
    { }

    /// <inheritdoc/>
    protected SceneChangeRejectedException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
