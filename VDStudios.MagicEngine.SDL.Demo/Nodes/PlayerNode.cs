using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using SDL2.NET.Input;
using SDL2.NET.SDLImage;
using VDStudios.MagicEngine.DemoResources;
using VDStudios.MagicEngine.Graphics.SDL;
using VDStudios.MagicEngine.Graphics.SDL.DrawOperations;
using VDStudios.MagicEngine.Input;
using VDStudios.MagicEngine.SDL.Demo.Scenes;
using VDStudios.MagicEngine.SDL.Demo.Services;
using VDStudios.MagicEngine.SDL.Demo.Utilities;
using VDStudios.MagicEngine.World2D;
using Scancode = SDL2.NET.Scancode;

namespace VDStudios.MagicEngine.SDL.Demo.Nodes;

public class PlayerNode : SingleSpriteEntityNode, IWorldMobile2D
{
    public float Speed { get; } = .3f;

    private static TextureOperation CreateRobinSprite(Game game)
        => new(game, c =>
        {
            using var stream = new MemoryStream(Animations.Robin);
            return Image.LoadTexture(c.Renderer, stream);
        });

    private static CharacterAnimationContainer CreateAnimationContainer()
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

    public PlayerNode(Game game) :
        base(CreateRobinSprite(game), CreateAnimationContainer())
    { }

    protected override async ValueTask<bool> Updating(TimeSpan delta)
    {
        Position += Direction * Speed;
        await base.Updating(delta);
        Direction = default;

        return true;
    }

    protected override async ValueTask Attaching(Scene scene)
    {
        await base.Attaching(scene);
        scene.Services.GetService<GameState>().PlayerNode = this;

        if (scene.GetDrawOperationManager<SDLGraphicsContext>(out var dopm))
        {
            var inman = scene.Services.GetService<InputManagerService>();

            inman.AddKeyBinding(Scancode.W, s =>
            {
                Direction += Directions.Up;
                return ValueTask.CompletedTask;
            });

            inman.AddKeyBinding(Scancode.A, s =>
            {
                Direction += Directions.Left;
                return ValueTask.CompletedTask;
            });

            inman.AddKeyBinding(Scancode.S, s =>
            {
                Direction += Directions.Down;
                return ValueTask.CompletedTask;
            });

            inman.AddKeyBinding(Scancode.D, s =>
            {
                Direction += Directions.Right;
                return ValueTask.CompletedTask;
            });
        }
        else
            Debug.Fail("The attached scene did not have a DrawOperationManager for SDLGraphicsContext");
    }
}
