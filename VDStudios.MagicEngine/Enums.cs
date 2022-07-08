using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents the likelihood or tendency that a given element has to be asynchronous
/// </summary>
public enum AsynchronousTendency
{
    /// <summary>
    /// Represents an element that is never asynchronous
    /// </summary>
    Synchronous,

    /// <summary>
    /// Represents an element that most often performs synchronously, but can sometimes perform asynchronously
    /// </summary>
    RarelyAsynchronous,

    /// <summary>
    /// Represents an element that performs synchronously or asynchronously at varying or similar intervals
    /// </summary>
    SometimesAsynchronous,

    /// <summary>
    /// Represents an element that most often performs asynchronously, but can sometimes synchronously
    /// </summary>
    /// <remarks>
    /// Can also represent an element that initially performs synchronously, but can sometimes perform asynchronously afterwards. In an async method, for example, the first part of the method may be synchronous, but depending on certain criteria may execute the rest of itself asynchronously
    /// </remarks>
    OftenAsynchronous,

    /// <summary>
    /// Represents an element that almost always performs asynchronously
    /// </summary>
    /// <remarks>
    /// Can also represent an element that initially performs synchronously, but always performs asynchronously afterwards. In an async method, for example, the first part of the method may be synchronous, but the rest of it executes asynchronously
    /// </remarks>
    MostlyAsynchronous,

    /// <summary>
    /// Represents an element that always performs asynchronously
    /// </summary>
    /// <remarks>
    /// Should not be used if partially synchronous. In methods, for example, consider calling and awaiting <see cref="Task.Yield"/> to ensure the method executes asynchronously from the start
    /// </remarks>
    AlwaysAsynchronous
}

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
