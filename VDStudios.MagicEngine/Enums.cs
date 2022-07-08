using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents the Update Batch that a <see cref="Node"/> can be assigned into
/// </summary>
public enum UpdateBatch
{
    /// <summary>
    /// The first update batch, will be updated first
    /// </summary>
    First,

    /// <summary>
    /// The second update batch, will be updated after <see cref="First"/>
    /// </summary>
    Second,

    /// <summary>
    /// The third update batch, will be updated after <see cref="Second"/>
    /// </summary>
    Third,

    /// <summary>
    /// The fourth update batch, will be updated after <see cref="Third"/>
    /// </summary>
    Fourth,

    /// <summary>
    /// The fifth update batch, will be updated after <see cref="Fourth"/>
    /// </summary>
    Fifth,

    /// <summary>
    /// The sixth update batch, will be updated after <see cref="Fifth"/> and all others
    /// </summary>
    Last
}
