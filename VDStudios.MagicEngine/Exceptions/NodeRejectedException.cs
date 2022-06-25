namespace VDStudios.MagicEngine.Exceptions;

/// <summary>
/// An exception that is thrown when a <see cref="FunctionalComponent"/> rejects being installed onto a <see cref="Node"/>
/// </summary>
[Serializable]
public class NodeRejectedException : Exception
{
    /// <summary>
    /// Instances and describes a new <see cref="NodeRejectedException"/>
    /// </summary>
    /// <param name="reason">The reason the node was rejected</param>
    /// <param name="component">The component that rejected the node</param>
    /// <param name="rejectedNode">The node that was rejected by the component</param>
    public NodeRejectedException(string? reason, FunctionalComponent component, Node rejectedNode)
        : base($"Node of type {rejectedNode.GetType().Name} was rejected by FunctionalComponent of type {component.GetType().Name} and could not be installed onto{(reason is null ? "" : $": {reason}")}")
    { }

    /// <inheritdoc/>
    protected NodeRejectedException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}