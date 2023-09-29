using System.Diagnostics;
using System.Numerics;
using SDL2.NET;
using SDL2.NET.Input;
using SDL2.NET.SDLImage;
using SDL2.NET.Utilities;
using VDStudios.MagicEngine.Demo.Common.Services;
using VDStudios.MagicEngine.DemoResources;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Graphics.Veldrid;
using VDStudios.MagicEngine.Graphics.Veldrid.DrawOperations;
using VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes;
using VDStudios.MagicEngine.Veldrid.Demo.Nodes;
using Veldrid;
using Veldrid.ImageSharp;
using static System.Formats.Asn1.AsnWriter;

namespace VDStudios.MagicEngine.Veldrid.Demo.Scenes;

public class TestScene : DemoSceneBase
{
    public TestScene(Game game) : base(game)
    {
    }

    protected override async ValueTask Beginning()
    {
        await base.Beginning();
        RegisterDrawOperationManager(new DrawOperationManager<VeldridGraphicsContext>(this));

        var vgc = (VeldridGraphicsManager)Game.MainGraphicsManager;
        var resources = vgc.Resources;
        var textureCache = resources.TextureCache;
        var samplerCache = resources.SamplerCache;

        textureCache.RegisterResource(
            "baum",
            c => new ImageSharpTexture(new MemoryStream(Animations.Baum)).CreateDeviceTexture(c.GraphicsDevice, c.ResourceFactory), 
            out var textureEntry
        );
        textureEntry.RegisterResource("default", static (c, t) => c.ResourceFactory.CreateTextureView(t));

        textureCache.RegisterResource(
            "robin", 
            c => new ImageSharpTexture(new MemoryStream(Animations.Robin)).CreateDeviceTexture(c.GraphicsDevice, c.ResourceFactory), 
            out textureEntry
        );
        textureEntry.RegisterResource("default", static (c, t) => c.ResourceFactory.CreateTextureView(t));

        textureCache.RegisterResource(
            "grass1", 
            c => new ImageSharpTexture(new MemoryStream(Animations.Grass1)).CreateDeviceTexture(c.GraphicsDevice, c.ResourceFactory), 
            out textureEntry
        );
        textureEntry.RegisterResource("default", static (c, t) => c.ResourceFactory.CreateTextureView(t));

        samplerCache.RegisterResource("default", static c => c.ResourceFactory.CreateSampler(new SamplerDescription(
            SamplerAddressMode.Wrap,
            SamplerAddressMode.Wrap,
            SamplerAddressMode.Wrap,
            SamplerFilter.MinLinear_MagLinear_MipLinear,
            null,
            4,
            0,
            0,
            0,
            SamplerBorderColor.TransparentBlack
        )), out _);

        //var txc = Services.GetService<ResourceCache<VeldridGraphicsContext, Texture>>();

        //var pnode = new MobileSingleSpriteEntityNode(
        //    new(Game, txc.GetResource("robin").Factory),
        //    CreateRobinAnimationContainer()
        //);
        //await Attach(pnode);

        //Services.GetService<GameState>().PlayerNode = pnode;

        //var terrainNode = new TerrainNode(new ResourceOperation(Game, txc.GetResource("grass1").Factory));
        //await Attach(terrainNode);

        //var hnode = new HUDNode(Game);
        //await Attach(hnode);

        //int trees = Random.Next(500, 1000);
        //var baumf = txc.GetResource("baum").Factory;
        //for (int i = 0; i < trees; i++)
        //{
        //    var tnode = new SingleSpriteEntityNode(new ResourceOperation(Game, baumf));
        //    await Attach(tnode);
        //    tnode.Position = new Vector2(spread(), spread());
        //    //tnode.EnableDebugOutlinesDefaultColor();

        //    int spread()
        //        => 32 * Random.Next(0, 96) * (Random.Next(0, 100) > 50 ? -1 : 1);
        //}

        //pnode.EnableDebugOutlinesDefaultColor();

        //Camera.Move(scale: new(2, 2, 1));

        //Camera.Target = pnode;
        vgc.InputReady += Vgc_InputReady;
        var tnode = new GraphicsTestNode(Game);
        await Attach(tnode);
    }

    private void Vgc_InputReady(GraphicsManager graphicsManager, Input.InputSnapshot inputSnapshot, TimeSpan timestamp)
    {
        if ((inputSnapshot.ActiveModifiers & Input.KeyModifier.Ctrl) > 0 &&
            inputSnapshot.KeyEventDictionary.TryGetValue(Input.Scancode.G, out var g) && inputSnapshot.KeyEventDictionary.TryGetValue(Input.Scancode.R, out var r))
        {
            if (g.Repeat && r.Repeat) return;
            var vgc = (VeldridGraphicsManager)Game.MainGraphicsManager;
            var resources = vgc.Resources;
            resources.RemovePipeline<TexturedShape2DRenderer<Vertex2D, TextureCoordinate2D>>(out _);
            resources.ShaderCache.RemoveResource<TexturedShape2DRenderer<Vertex2D, TextureCoordinate2D>>(out _);
            Log.Information("Cleared Default Pipeline and ShaderSet for TexturedShape2DRenderer<Vertex2D, TextureCoordinate2D>");
        }
    }

    //private static CharacterAnimationContainer CreateRobinAnimationContainer()
    //{
    //    var animContainer = new CharacterAnimationContainer(8,
    //        Helper.GetRectangles(AnimationDefinitions.Robin.Player.Idle.Frames), true,
    //        Helper.GetRectangles(AnimationDefinitions.Robin.Player.Active.Frames), true,
    //        Helper.GetRectangles(AnimationDefinitions.Robin.Player.Up.Frames), true,
    //        Helper.GetRectangles(AnimationDefinitions.Robin.Player.Down.Frames), true,
    //        Helper.GetRectangles(AnimationDefinitions.Robin.Player.Left.Frames), true,
    //        Helper.GetRectangles(AnimationDefinitions.Robin.Player.Right.Frames), true,
    //        Helper.GetRectangles(AnimationDefinitions.Robin.Player.UpRight.Frames), true,
    //        Helper.GetRectangles(AnimationDefinitions.Robin.Player.DownRight.Frames), true,
    //        Helper.GetRectangles(AnimationDefinitions.Robin.Player.UpLeft.Frames), true,
    //        Helper.GetRectangles(AnimationDefinitions.Robin.Player.DownLeft.Frames), true
    //    );

    //    foreach (var (_, a) in animContainer)
    //        a.EndHang = TimeSpan.FromSeconds(1d / 6);

    //    animContainer.Idle.StartHang
    //        = animContainer.Idle.EndHang
    //        = animContainer.Active.StartHang
    //        = animContainer.Active.EndHang
    //        = TimeSpan.FromSeconds(1d / 2);

    //    return animContainer;
    //}
}