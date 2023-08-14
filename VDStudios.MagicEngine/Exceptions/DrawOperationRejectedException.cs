using VDStudios.MagicEngine.Graphics;

namespace VDStudios.MagicEngine.Exceptions;

/// <summary>
/// An exception that is thrown when a <see cref="DrawOperation"/> rejects being registered on a <see cref="GraphicsManager"/>
/// </summary>
[Serializable]
public class DrawOperationRejectedException : Exception
{
    /// <summary>
    /// Instances and describes a new <see cref="DrawOperationRejectedException"/>
    /// </summary>
    /// <param name="reason">The reason the operation was rejected</param>
    /// <param name="manager">The manager that rejected the operation</param>
    /// <param name="rejectedDrawOperation">The operation that was rejected by the manager</param>
    public DrawOperationRejectedException(string? reason, GraphicsManager manager, DrawOperation rejectedDrawOperation)
        : base($"DrawOperation of type {rejectedDrawOperation.GetType().Name} was rejected by GraphicsManager of type {manager.GetType().Name} and could not be registered{(reason is null ? "" : $": {reason}")}")
    { }

    /// <inheritdoc/>
    protected DrawOperationRejectedException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}