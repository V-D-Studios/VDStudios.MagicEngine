using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents the Update Batch that a <see cref="IUpdateableNode"/> or <see cref="IAsyncUpdateableNode"/> can be assigned into
/// </summary>
public enum UpdateBatch
{
    /// <summary>
    /// The first update batch, will be updated first
    /// </summary>
    Batch1,

    /// <summary>
    /// The second update batch, will be updated after <see cref="Batch1"/>
    /// </summary>
    Batch2,

    /// <summary>
    /// The third update batch, will be updated after <see cref="Batch2"/>
    /// </summary>
    Batch3,

    /// <summary>
    /// The fourth update batch, will be updated after <see cref="Batch3"/>
    /// </summary>
    Batch4,

    /// <summary>
    /// The fifth update batch, will be updated after <see cref="Batch4"/>
    /// </summary>
    Batch5,

    /// <summary>
    /// The sixth update batch, will be updated after <see cref="Batch5"/>
    /// </summary>
    Batch6
}
