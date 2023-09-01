using VDStudios.MagicEngine.Graphics.SDL;
using VDStudios.MagicEngine.Graphics.SDL.RenderTargets;

namespace VDStudios.MagicEngine.SDL.Demo;

public abstract class DemoScene : Scene
{
    private SDLCamera2D? _cam;
    public SDLCamera2D Camera => _cam ?? throw new InvalidOperationException("Cannot access this Scene's Camera before the Scene is begun");

    protected override ValueTask Beginning()
    {
        var manager = ((SDLGraphicsManager)Game.MainGraphicsManager);
        manager.GetOrCreateRenderTargetList(0).Add(_cam = new SDLCamera2D(manager));
        manager.GetOrCreateRenderTargetList(1).Add(new PassthroughRenderTarget(manager));
        return ValueTask.CompletedTask;
    }

    protected DemoScene(Game game) : base(game)
    {
    }
}
