using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.Exceptions;

/// <summary>
/// An exception that is thrown when a <see cref="Node"/> rejects the installation of a given <see cref="FunctionalComponent"/>
/// </summary>
[Serializable]
public class FunctionalComponentRejectedException : Exception
{
    /// <summary>
    /// Instances and describes a new <see cref="FunctionalComponentRejectedException"/>
    /// </summary>
    /// <param name="reason">The reason the component was rejected</param>
    /// <param name="node">The node that rejected the component</param>
    /// <param name="rejectedComponent">The component that was rejected</param>
    public FunctionalComponentRejectedException(string? reason, Node node, FunctionalComponent rejectedComponent) 
        : base($"FunctionalComponent of type {rejectedComponent.GetType().Name} was rejected by Node of type {node.GetType().Name} and could not be installed{(reason is null ? "" : $": {reason}")}")
    { }

    /// <inheritdoc/>
    protected FunctionalComponentRejectedException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
