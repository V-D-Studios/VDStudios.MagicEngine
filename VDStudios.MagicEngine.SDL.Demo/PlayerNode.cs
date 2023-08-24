using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Graphics.SDL;

namespace VDStudios.MagicEngine.SDL.Demo;

public class PlayerNode : Node, IDrawableNode<SDLGraphicsContext>
{
    private PlayerRenderer? Renderer;

    public PlayerNode(Game game) : base(game)
    {
        DrawOperationManager = new DrawOperationManager<SDLGraphicsContext>(this);
    }

    protected override async ValueTask Attaching(Scene scene)
    {
        Renderer = await DrawOperationManager.AddDrawOperation(new PlayerRenderer(Game));
        await base.Attaching(scene);
    }

    public DrawOperationManager<SDLGraphicsContext> DrawOperationManager { get; }
}
