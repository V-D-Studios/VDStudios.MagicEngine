namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a <see cref="Node"/> that is to be updated asynchronously. Takes precedence over <see cref="IUpdateableNode"/> if both are implemented
/// </summary>
public interface IAsyncUpdateableNode
{
    /// <summary>
    /// Updates the <see cref="Node"/> asynchronously
    /// </summary>
    /// <param name="delta">The amount of time that has passed since the last update batch call</param>
    public Task UpdateAsync(TimeSpan delta);

    /// <summary>
    /// The batch this <see cref="IUpdateableNode"/> should be assigned to
    /// </summary>
    public UpdateBatch UpdateBatch { get; }
}