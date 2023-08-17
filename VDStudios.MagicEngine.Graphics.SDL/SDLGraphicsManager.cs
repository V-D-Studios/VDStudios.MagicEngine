using SDL2.NET;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Input;

namespace VDStudios.MagicEngine.Graphics.SDL;

/// <summary>
/// A <see cref="GraphicsManager{TGraphicsContext}"/> for SDL
/// </summary>
public class SDLGraphicsManager : GraphicsManager<SDLGraphicsContext>
{
    /// <inheritdoc/>
    public override RgbaVector BackgroundColor { get; set; }

    /// <inheritdoc/>
    public override IntVector2 WindowSize { get; protected set; }

    private SDLGraphicsContext? context;

    /// <summary>
    /// The SDL Window managed by this <see cref="SDLGraphicsManager"/>
    /// </summary>
    public Window Window { get => window ?? throw new InvalidOperationException("Cannot get the Window of an SDLGraphicsManager that has not been initialized"); private set => window = value; }
    private Window? window;

    /// <summary>
    /// <see cref="Window"/>'s configuration
    /// </summary>
    public WindowConfig WindowConfig { get; }

    /// <inheritdoc/>
    public SDLGraphicsManager(Game game, WindowConfig? windowConfig = null) : base(game) 
    {
        WindowConfig = windowConfig ?? WindowConfig.Default;
    }

    /// <inheritdoc/>
    protected override ValueTask<SDLGraphicsContext> FetchGraphicsContext()
        => ValueTask.FromResult(context ??= new(this));

    /// <inheritdoc/>
    protected override void FramelockedDispose(bool disposing)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    protected override InputSnapshotBuffer CreateNewEmptyInputSnapshotBuffer()
        => new SDLInputSnapshotBuffer(this);

    /// <inheritdoc/>
    protected override void SetupGraphicsManager()
    {
        Window = new Window(Game.GameTitle, 800, 600, WindowConfig);
    }

    /// <inheritdoc/>
    protected override Task Run()
    {
        throw new NotImplementedException();
    }
}
