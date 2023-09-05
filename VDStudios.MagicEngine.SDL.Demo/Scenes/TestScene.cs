using SDL2.NET;
using SDL2.NET.SDLImage;
using SDL2.NET.Utilities;
using VDStudios.MagicEngine.DemoResources;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Graphics.SDL;
using VDStudios.MagicEngine.Graphics.SDL.DrawOperations;
using VDStudios.MagicEngine.SDL.Demo.Nodes;
using VDStudios.MagicEngine.SDL.Demo.Services;
using VDStudios.MagicEngine.Services;

namespace VDStudios.MagicEngine.SDL.Demo.Scenes;

public class TestScene : DemoSceneBase
{
    public TestScene(Game game) : base(game)
    {
    }

    protected override async ValueTask Beginning()
    {
        await base.Beginning();
        RegisterDrawOperationManager(new DrawOperationManager<SDLGraphicsContext>(this));
        
        var pnode = new PlayerNode(Game);
        await Attach(pnode);

        var hnode = new HUDNode(Game);
        await Attach(hnode);

        var tnode = new SingleSpriteEntityNode(new TextureOperation(
        Game, c =>
        {
            using var rwop = RWops.CreateFromMemory(new PinnedArray<byte>(Animations.Baum));
            return Image.LoadTexture(c.Renderer, rwop);
        }));
        await Attach(tnode);

        tnode.EnableDebugOutlinesDefaultColor();
        pnode.EnableDebugOutlinesDefaultColor();

        Camera.Move(scale: new(2, 2, 1));

        Camera.Target = pnode;
    }
}