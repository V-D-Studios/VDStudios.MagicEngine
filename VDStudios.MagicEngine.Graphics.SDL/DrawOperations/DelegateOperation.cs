namespace VDStudios.MagicEngine.Graphics.SDL.DrawOperations;

/// <summary>
/// A <see cref="DrawOperation{TGraphicsContext}"/> that performs the actions defined by the delegates it was created with
/// </summary>
public class DelegateOperation : DrawOperation<SDLGraphicsContext>
{
    private readonly Func<DelegateOperation, ValueTask>? createResourcesDelegate;
    private readonly Action<DelegateOperation, SDLGraphicsContext>? createGPUResources;
    private readonly Action<DelegateOperation, SDLGraphicsContext>? updateGpuState;
    private readonly Action<DelegateOperation, TimeSpan, SDLGraphicsContext, RenderTarget<SDLGraphicsContext>> draw;

    /// <summary>
    /// Creates a new instance of <see cref="DelegateOperation"/>
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public DelegateOperation
        (Game game, Action<DelegateOperation, TimeSpan, SDLGraphicsContext, RenderTarget<SDLGraphicsContext>> draw, Func<DelegateOperation, ValueTask>? createResourcesDelegate = null, Action<DelegateOperation, SDLGraphicsContext>? createGPUResources = null, Action<DelegateOperation, SDLGraphicsContext>? updateGpuState = null)
        : base(game)
    {
        this.createResourcesDelegate = createResourcesDelegate;
        this.createGPUResources = createGPUResources;
        this.updateGpuState = updateGpuState;
        this.draw = draw ?? throw new ArgumentNullException(nameof(draw));
    }

    /// <inheritdoc/>
    protected override ValueTask CreateResourcesAsync()
        => createResourcesDelegate?.Invoke(this) ?? ValueTask.CompletedTask;

    /// <inheritdoc/>
    protected override void CreateGPUResources(SDLGraphicsContext context)
        => createGPUResources?.Invoke(this, context);

    /// <inheritdoc/>
    protected override void Draw(TimeSpan delta, SDLGraphicsContext context, RenderTarget<SDLGraphicsContext> target)
        => draw(this, delta, context, target);

    /// <inheritdoc/>
    protected override void UpdateGPUState(SDLGraphicsContext context)
        => updateGpuState?.Invoke(this, context);
}
