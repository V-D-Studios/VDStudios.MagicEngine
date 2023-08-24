using System.Diagnostics;
using VDStudios.MagicEngine.DemoResources;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Graphics.SDL;

namespace VDStudios.MagicEngine.SDL.Demo;

public class PlayerNode : Node, IDrawableNode<SDLGraphicsContext>
{
    private readonly CharacterAnimationContainer Animations;
    private PlayerRenderer? Renderer;

    public PlayerNode(Game game) : base(game)
    {
        DrawOperationManager = new DrawOperationManager<SDLGraphicsContext>(this);
        Animations = new(8,
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

        foreach (var (_, a) in Animations)
            a.StartHang = a.EndHang = TimeSpan.FromSeconds(1d / 2);
    }

    protected override ValueTask<bool> Updating(TimeSpan delta)
    {
        Debug.Assert(Renderer is not null, "PlayerNode.Renderer is unexpectedly null at the time of rendering");

        if (Animations.CurrentAnimation.Update())
            Renderer.View = Animations.CurrentAnimation.CurrentElement;

        return base.Updating(delta);
    }

    protected override async ValueTask Attaching(Scene scene)
    {
        Renderer = await DrawOperationManager.AddDrawOperation(new PlayerRenderer(Game));
        await base.Attaching(scene);
    }

    public DrawOperationManager<SDLGraphicsContext> DrawOperationManager { get; }
}
