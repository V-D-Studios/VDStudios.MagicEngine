using System.Diagnostics;
using SDL2.NET.SDLImage;
using VDStudios.MagicEngine.DemoResources;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Graphics.SDL;
using VDStudios.MagicEngine.Graphics.SDL.DrawOperations;

namespace VDStudios.MagicEngine.SDL.Demo;

public class PlayerNode : Node
{
    private readonly CharacterAnimationContainer AnimationContainer;
    private TextureOperation? SpriteOperation;

    public PlayerNode(Game game) : base(game)
    {
        AnimationContainer = new(8,
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

        foreach (var (_, a) in AnimationContainer)
            a.StartHang = a.EndHang = TimeSpan.FromSeconds(1d / 2);
    }

    protected override ValueTask<bool> Updating(TimeSpan delta)
    {
        Debug.Assert(SpriteOperation is not null, "PlayerNode.SpriteOperation is unexpectedly null at the time of updating");

        if (AnimationContainer.CurrentAnimation.Update())
            SpriteOperation.View = AnimationContainer.CurrentAnimation.CurrentElement;

        return base.Updating(delta);
    }

    protected override async ValueTask Attaching(Scene scene)
    {
        Game.MainGraphicsManager.InputReady += MainGraphicsManager_InputReady;

        if (scene.GetDrawOperationManager<SDLGraphicsContext>(out var dopm))
        {
            SpriteOperation = new TextureOperation(Game, c =>
            {
                using var stream = new MemoryStream(Animations.Robin);
                return Image.LoadTexture(c.Renderer, stream);
            }, AnimationContainer.CurrentAnimation.CurrentElement);

            await dopm.AddDrawOperation(SpriteOperation);
        }
        else
            Debug.Fail("The attached scene did not have a DrawOperationManager for SDLGraphicsContext");

        await base.Attaching(scene);
    }

    private void MainGraphicsManager_InputReady(GraphicsManager graphicsManager, Input.InputSnapshot inputSnapshot, DateTime timestamp)
    {
        throw new NotImplementedException();
    }
}
