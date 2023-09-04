using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Graphics.SDL;

namespace VDStudios.MagicEngine.SDL.Demo;

public class TestScene : DemoScene
{
    public TestScene(Game game) : base(game)
    {
    }

    protected override async ValueTask Beginning()
    {
        await base.Beginning();
        RegisterDrawOperationManager(new DrawOperationManager<SDLGraphicsContext>(this));
        await Attach(new InputReactorNode(Game));
        var pnode = new PlayerNode(Game);
        await Attach(pnode);
        Camera.Target = pnode;
    }
}