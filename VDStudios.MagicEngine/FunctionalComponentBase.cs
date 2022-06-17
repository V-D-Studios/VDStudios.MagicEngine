using System.Runtime.CompilerServices;

namespace VDStudios.MagicEngine;

/// <summary>
/// Represents the base class for <see cref="FunctionalComponent{TNode}"/>
/// </summary>
/// <remarks>
/// Remember to override the Update methods in this class. It's safe to throw an exception in the method you won't use, see: <see cref="ThrowNotImplemented(string?)"/>
/// </remarks>
public abstract class FunctionalComponentBase : GameObject, IAsyncFunctionalComponent, IFunctionalComponent
{
    private TimeSpan sdl_lastUpdate;

    /// <summary>
    /// Represents the internal Component Index in the currently attached node
    /// </summary>
    public int Index { get; internal set; }

    internal abstract void InternalDispose();

    /// <summary>
    /// Throws a <see cref="NotImplementedException"/> exception if the given method is not implemented
    /// </summary>
    /// <remarks>
    /// This method is useful to fill whichever update method you're not using
    /// </remarks>
    /// <param name="caller">Ignore this argument</param>
    protected void ThrowNotImplemented([CallerMemberName] string? caller = null)
        => throw new NotImplementedException($"Method {caller} is not implemented for this FunctionalComponent");

    /// <summary>
    /// Whether or not this <see cref="FunctionalComponent{TNode}"/> is asynchronous and <see cref="UpdateAsync(TimeSpan)"/> should be called
    /// </summary>
    /// <remarks>
    /// This property is <c>init</c> only and will not change after its constructed
    /// </remarks>
    public bool IsAsync { get; protected init; }

    /// <summary>
    /// Updates the current <see cref="FunctionalComponent{TNode}"/>. Not all components are synchronous: see <see cref="IsAsync"/> to decide which method to call. 
    /// </summary>
    /// <remarks>
    /// Only <see cref="UpdateComponentAsync(TimeSpan, TimeSpan)"/> or <see cref="UpdateComponent(TimeSpan, TimeSpan)"/> will be called, NOT both. If your code has asynchronous parts, override <see cref="UpdateComponentAsync(TimeSpan, TimeSpan)"/>, if not override <see cref="UpdateComponent(TimeSpan, TimeSpan)"/>
    /// </remarks>
    /// <param name="gameDelta">The amount of time that has passed since the last Update frame in the Game</param>
    public void Update(TimeSpan gameDelta)
    {
        if (sdl_lastUpdate == default)
            UpdateComponent(gameDelta, TimeSpan.Zero);

        var ot = sdl_lastUpdate;
        sdl_lastUpdate = Game.TotalTime;
        UpdateComponent(gameDelta, ot - sdl_lastUpdate);
    }

    /// <summary>
    /// Updates the current <see cref="FunctionalComponent{TNode}"/>. Not all components are asynchronous, see <see cref="IsAsync"/> to decide which method to call. 
    /// </summary>
    /// <remarks>
    /// Only <see cref="UpdateComponentAsync(TimeSpan, TimeSpan)"/> or <see cref="UpdateComponent(TimeSpan, TimeSpan)"/> will be called, NOT both. If your code has asynchronous parts, override <see cref="UpdateComponentAsync(TimeSpan, TimeSpan)"/>, if not override <see cref="UpdateComponent(TimeSpan, TimeSpan)"/>
    /// </remarks>
    /// <param name="gameDelta">The amount of time that has passed since the last Update frame in the Game</param>
    public ValueTask UpdateAsync(TimeSpan gameDelta)
    {
        if (sdl_lastUpdate == default)
            return UpdateComponentAsync(gameDelta, TimeSpan.Zero);

        var ot = sdl_lastUpdate;
        sdl_lastUpdate = Game.TotalTime;
        return UpdateComponentAsync(gameDelta, ot - sdl_lastUpdate);
    }

    /// <summary>
    /// This method is called after <see cref="UpdateAsync(TimeSpan)"/> and is where your code goes
    /// </summary>
    /// <remarks>
    /// Only <see cref="UpdateComponentAsync(TimeSpan, TimeSpan)"/> or <see cref="UpdateComponent(TimeSpan, TimeSpan)"/> will be called, NOT both. If your code has asynchronous parts, override <see cref="UpdateComponentAsync(TimeSpan, TimeSpan)"/>, if not override <see cref="UpdateComponent(TimeSpan, TimeSpan)"/>
    /// </remarks>
    /// <param name="gameDelta">The amount of time that has passed since the last Update frame in the Game</param>
    /// <param name="componentDelta">The amount of time that has passed since the last time this component was updated. Will be 0 the first time it's updated</param>
    protected abstract ValueTask UpdateComponentAsync(TimeSpan gameDelta, TimeSpan componentDelta);

    /// <summary>
    /// This method is called after <see cref="Update(TimeSpan)"/> and is where your code goes
    /// </summary>
    /// <remarks>
    /// Only <see cref="UpdateComponentAsync(TimeSpan, TimeSpan)"/> or <see cref="UpdateComponent(TimeSpan, TimeSpan)"/> will be called, NOT both. If your code has asynchronous parts, override <see cref="UpdateComponentAsync(TimeSpan, TimeSpan)"/>, if not override <see cref="UpdateComponent(TimeSpan, TimeSpan)"/>
    /// </remarks>
    /// <param name="gameDelta">The amount of time that has passed since the last Update frame in the Game</param>
    /// <param name="componentDelta">The amount of time that has passed since the last time this component was updated. Will be 0 the first time it's updated</param>
    protected abstract void UpdateComponent(TimeSpan gameDelta, TimeSpan componentDelta);

    /// <inheritdoc/>
    public void Dispose() => InternalDispose();

    /// <summary>
    /// Uninstalls the current <see cref="FunctionalComponent{TNode}"/> from its <see cref="Node"/>
    /// </summary>
    public abstract void UninstallFromNode();
}
