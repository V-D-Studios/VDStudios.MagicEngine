using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Numerics;
using SDL2.NET;
using SDL2.NET.SDLImage;
using VDStudios.MagicEngine.DemoResources;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Graphics.SDL;
using VDStudios.MagicEngine.Internal;

namespace VDStudios.MagicEngine.SDL.Demo;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var game = new SDLGame();
        await game.StartGame(g => new TestScene(g));
    }
}

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

public class PlayerRenderer : DrawOperation<SDLGraphicsContext>
{
    public PlayerRenderer(Game game) : base(game) { }

    private Texture? RobinTexture;
    private readonly Vector2 SpriteSize = new(32, 32);

    protected override ValueTask CreateResources() => ValueTask.CompletedTask;

    protected override void CreateGPUResources(SDLGraphicsContext context)
    {
        Log.Debug("Creating PlayerRenderer resources");
        using var stream = new MemoryStream(Animations.Robin);
        RobinTexture = Image.LoadTexture(context.Renderer, stream);
        Log.Debug("Succesfully created PlayerRenderer resources");
    }

    protected override void Draw(TimeSpan delta, SDLGraphicsContext context, RenderTarget<SDLGraphicsContext> target)
    {
        Debug.Assert(RobinTexture is not null);
        var dest = this.CreateDestinationRectangle(RobinTexture.Size, target.Transformation).ToRectangle();
        RobinTexture.Render(new Rectangle(32, 32, 0, 0), dest);
    }

    protected override void UpdateGPUState(SDLGraphicsContext context)
    {
    }
}