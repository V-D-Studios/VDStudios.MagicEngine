using VDStudios.MagicEngine.Graphics.SDL;
using VDStudios.MagicEngine.Graphics.SDL.RenderTargets;

namespace VDStudios.MagicEngine.SDL.Demo;

public class TestScene : Scene
{
    public TestScene(Game game) : base(game)
    {
    }

    protected override async ValueTask Beginning()
    {
        var manager = ((SDLGraphicsManager)Game.MainGraphicsManager);
        manager.RenderTargets.Add(new SDLCamera2D(manager));
        await Attach(new PlayerNode(Game));
    }
}