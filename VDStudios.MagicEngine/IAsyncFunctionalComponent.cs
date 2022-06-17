namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a <see cref="FunctionalComponent{TNode}"/> that can be updated asynchronously
/// </summary>
public interface IAsyncFunctionalComponent
{
    /// <summary>
    /// Updates the current <see cref="FunctionalComponent{TNode}"/>.
    /// </summary>
    /// <param name="gameDelta">The amount of time that has passed since the last Update frame in the Game</param>
    public ValueTask UpdateAsync(TimeSpan gameDelta);
}