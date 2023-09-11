using System.Numerics;
using SDL2.NET;
using SDL2.NET.SDLImage;
using SDL2.NET.Utilities;
using VDStudios.MagicEngine.DemoResources;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Graphics.SDL;
using VDStudios.MagicEngine.Graphics.SDL.DrawOperations;
using VDStudios.MagicEngine.SDL.Demo.Nodes;
using VDStudios.MagicEngine.Demo.Common.Services;
using VDStudios.MagicEngine.Services;
using static System.Formats.Asn1.AsnWriter;
using VDStudios.MagicEngine.Demo.Common.Utilities;

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
        
        var txc = Services.GetService<ResourceCache<SDLGraphicsContext, Texture>>();

        var pnode = new MobileSingleSpriteEntityNode(
            new(Game, txc.GetResource("robin").Factory),
            CreateRobinAnimationContainer()
        );
        await Attach(pnode);

        Services.GetService<GameState>().PlayerNode = pnode;

        var terrainNode = new TerrainNode(new TextureOperation(Game, txc.GetResource("grass1").Factory));
        await Attach(terrainNode);

        var hnode = new HUDNode(Game);
        await Attach(hnode);

        int trees = Random.Next(500, 1000);
        var baumf = txc.GetResource("baum").Factory;
        for (int i = 0; i < trees; i++)
        {
            var tnode = new SingleSpriteEntityNode(new TextureOperation(Game, baumf));
            await Attach(tnode);
            tnode.Position = new Vector2(spread(), spread());
            //tnode.EnableDebugOutlinesDefaultColor();

            int spread()
                => 32 * Random.Next(0, 96) * (Random.Next(0, 100) > 50 ? -1 : 1);
        }

        //pnode.EnableDebugOutlinesDefaultColor();

        Camera.Move(scale: new(2, 2, 1));

        Camera.Target = pnode;
    }

    private static CharacterAnimationContainer CreateRobinAnimationContainer()
    {
        var animContainer = new CharacterAnimationContainer(8,
            Helper.GetRectangles(AnimationDefinitions.Robin.Player.Idle.Frames), true,
            Helper.GetRectangles(AnimationDefinitions.Robin.Player.Active.Frames), true,
            Helper.GetRectangles(AnimationDefinitions.Robin.Player.Up.Frames), true,
            Helper.GetRectangles(AnimationDefinitions.Robin.Player.Down.Frames), true,
            Helper.GetRectangles(AnimationDefinitions.Robin.Player.Left.Frames), true,
            Helper.GetRectangles(AnimationDefinitions.Robin.Player.Right.Frames), true,
            Helper.GetRectangles(AnimationDefinitions.Robin.Player.UpRight.Frames), true,
            Helper.GetRectangles(AnimationDefinitions.Robin.Player.DownRight.Frames), true,
            Helper.GetRectangles(AnimationDefinitions.Robin.Player.UpLeft.Frames), true,
            Helper.GetRectangles(AnimationDefinitions.Robin.Player.DownLeft.Frames), true
        );

        foreach (var (_, a) in animContainer)
            a.EndHang = TimeSpan.FromSeconds(1d / 6);

        animContainer.Idle.StartHang
            = animContainer.Idle.EndHang
            = animContainer.Active.StartHang
            = animContainer.Active.EndHang
            = TimeSpan.FromSeconds(1d / 2);

        return animContainer;
    }

}