namespace VDStudios.MagicEngine.Exceptions;

/// <summary>
/// An exception that is thrown when a <see cref="Node"/> rejects being attached to a given parent <see cref="Node"/> or <see cref="Scene"/>
/// </summary>
[Serializable]
public class ParentNodeRejectedException : Exception
{
    /// <summary>
    /// Instances and describes a new <see cref="ParentNodeRejectedException"/>
    /// </summary>
    /// <param name="reason">The reason the parent was rejected</param>
    /// <param name="node">The node that rejected the parent</param>
    /// <param name="parent">The parent that was rejected</param>
    public ParentNodeRejectedException(string? reason, Node node, Node parent)
        : base($"Node of type {node.GetType().Name} rejected Node of type {parent.GetType().Name} as a parent, and would not attach to it{(reason is null ? "" : $": {reason}")}")
    { }

    /// <summary>
    /// Instances and describes a new <see cref="ParentNodeRejectedException"/>
    /// </summary>
    /// <param name="reason">The reason the parent was rejected</param>
    /// <param name="node">The node that rejected the parent</param>
    /// <param name="parent">The parent that was rejected</param>
    public ParentNodeRejectedException(string? reason, Node node, Scene parent)
        : base($"Node of type {node.GetType().Name} rejected Scene of type {parent.GetType().Name} as a parent, and would not attach to it{(reason is null ? "" : $": {reason}")}")
    { }

    /// <inheritdoc/>
    protected ParentNodeRejectedException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
