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
    /// <param name="component">The component that was rejected</param>
    public FunctionalComponentRejectedException(string reason, Node node, FunctionalComponent component) 
        : base($"FunctionalComponent of type {component.GetType().Name} was rejected by Node of type {node.GetType().Name} and could not be installed: {reason}") 
    { }

    /// <inheritdoc/>
    protected FunctionalComponentRejectedException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
