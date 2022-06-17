namespace VDStudios.MagicEngine;

/// <summary>
/// Represents a <see cref="FunctionalComponent{TNode}"/> that can be updated synchronously
/// </summary>
public interface IFunctionalComponent
{
    /// <summary>
    /// Updates the current <see cref="FunctionalComponent{TNode}"/>
    /// </summary>
    /// <param name="gameDelta">The amount of time that has passed since the last Update frame in the Game</param>
    public void Update(TimeSpan gameDelta);
}