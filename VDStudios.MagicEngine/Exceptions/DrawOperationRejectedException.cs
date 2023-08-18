using VDStudios.MagicEngine.Graphics;

namespace VDStudios.MagicEngine.Exceptions;

/// <summary>
/// An exception that is thrown when a <see cref="DrawOperation{TGraphicsContext}"/> rejects being registered on a <see cref="GraphicsManager{TGraphicsContext}"/>
/// </summary>
[Serializable]
public class DrawOperationRejectedException<TGraphicsContext> : Exception
    where TGraphicsContext : GraphicsContext<TGraphicsContext>
{
    /// <summary>
    /// Instances and describes a new <see cref="DrawOperationRejectedException{TGraphicsContext}"/>
    /// </summary>
    /// <param name="reason">The reason the operation was rejected</param>
    /// <param name="manager">The manager that rejected the operation</param>
    /// <param name="rejectedDrawOperation">The operation that was rejected by the manager</param>
    public DrawOperationRejectedException(string? reason, GraphicsManager<TGraphicsContext> manager, DrawOperation<TGraphicsContext> rejectedDrawOperation)
        : base($"DrawOperation of type {rejectedDrawOperation.GetType().Name} was rejected by GraphicsManager of type {manager.GetType().Name} and could not be registered{(reason is null ? "" : $": {reason}")}")
    { }

    /// <inheritdoc/>
    protected DrawOperationRejectedException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}