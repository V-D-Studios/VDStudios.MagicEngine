﻿namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a <see cref="Node"/> or <see cref="FunctionalComponent{TNode}"/> that is to be updated asynchronously. Takes precedence over <see cref="IUpdateable"/> if both are implemented
/// </summary>
public interface IUpdateableAsync
{
    /// <summary>
    /// Updates the <see cref="Node"/> or <see cref="FunctionalComponent{TNode}"/> asynchronously
    /// </summary>
    /// <param name="delta">The amount of time that has passed since the last update batch call</param>
    public ValueTask UpdateAsync(TimeSpan delta);

    /// <summary>
    /// The batch this <see cref="IUpdateable"/> should be assigned to
    /// </summary>
    public UpdateBatch UpdateBatch { get; }
}