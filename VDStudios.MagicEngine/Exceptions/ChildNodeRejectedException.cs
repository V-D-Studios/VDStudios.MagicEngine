namespace VDStudios.MagicEngine.Exceptions;

/// <summary>
/// An exception that is thrown when a <see cref="Node"/> or <see cref="Scene"/> rejects the attaching of a child <see cref="Node"/>
/// </summary>
[Serializable]
public class ChildNodeRejectedException : Exception
{
    /// <summary>
    /// Instances and describes a new <see cref="ChildNodeRejectedException"/>
    /// </summary>
    /// <param name="reason">The reason the child was rejected</param>
    /// <param name="node">The node that rejected the child</param>
    /// <param name="child">The child that was rejected</param>
    public ChildNodeRejectedException(string? reason, Node node, Node child)
        : base($"Node of type {child.GetType().Name} was rejected as a child by Node of type {node.GetType().Name} and could not be attached{(reason is null ? "" : $": {reason}")}")
    { }

    /// <summary>
    /// Instances and describes a new <see cref="ChildNodeRejectedException"/>
    /// </summary>
    /// <param name="reason">The reason the child was rejected</param>
    /// <param name="scene">The scene that rejected the child</param>
    /// <param name="child">The child that was rejected</param>
    public ChildNodeRejectedException(string? reason, Scene scene, Node child)
        : base($"Node of type {child.GetType().Name} was rejected as a child by Scene of type {scene.GetType().Name} and could not be attached{(reason is null ? "" : $": {reason}")}")
    { }

    /// <inheritdoc/>
    protected ChildNodeRejectedException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
