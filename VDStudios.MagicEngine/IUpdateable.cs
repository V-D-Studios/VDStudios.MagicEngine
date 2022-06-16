using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a <see cref="Node"/> or <see cref="FunctionalComponent{TNode}"/> that is to be updated. <see cref="IUpdateableAsync"/> takes precedence if both are implemented
/// </summary>
public interface IUpdateable
{
    /// <summary>
    /// Updates the <see cref="Node"/> or <see cref="FunctionalComponent{TNode}"/>
    /// </summary>
    /// <param name="delta">The amount of time that has passed since the last update batch call</param>
    public void Update(TimeSpan delta);

    /// <summary>
    /// The batch this <see cref="IUpdateable"/> should be assigned to
    /// </summary>
    public UpdateBatch UpdateBatch { get; }
}
