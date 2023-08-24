using System.Diagnostics;
using System.Numerics;
using SDL2.NET;
using SDL2.NET.SDLImage;
using VDStudios.MagicEngine.DemoResources;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Graphics.SDL;

namespace VDStudios.MagicEngine.SDL.Demo;

public class PlayerRenderer : DrawOperation<SDLGraphicsContext>
{
    public PlayerRenderer(Game game) : base(game) { }

    private Texture? RobinTexture;
    private readonly Vector2 SpriteSize = new(32, 32);

    public Rectangle View { get; set; }

    protected override ValueTask CreateResources() => ValueTask.CompletedTask;

    protected override void CreateGPUResources(SDLGraphicsContext context)
    {
        Log.Debug("Creating PlayerRenderer resources");
        using var stream = new MemoryStream(Animations.Robin);
        RobinTexture = Image.LoadTexture(context.Renderer, stream);
        Log.Debug("Succesfully created PlayerRenderer resources");
        ColorTransformationChanged += PlayerRenderer_ColorTransformationChanged;
    }

    private void PlayerRenderer_ColorTransformationChanged(DrawOperation<SDLGraphicsContext> drawOperation, TimeSpan timestamp)
    {
        if (RobinTexture is not null)
            this.ApplyColor(RobinTexture);
    }

    protected override void Draw(TimeSpan delta, SDLGraphicsContext context, RenderTarget<SDLGraphicsContext> target)
    {
        Debug.Assert(RobinTexture is not null);

        Transform(scale: new Vector3(4, 4, 1));

        var dest = this.CreateDestinationRectangle(SpriteSize, target.Transformation).ToRectangle();
        RobinTexture.Render(View, dest);
    }

    protected override void UpdateGPUState(SDLGraphicsContext context)
    {
    }
}