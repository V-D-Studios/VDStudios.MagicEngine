using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using SDL2.NET.Input;
using SDL2.NET.SDLImage;
using VDStudios.MagicEngine.DemoResources;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Graphics.SDL;
using VDStudios.MagicEngine.Graphics.SDL.DrawOperations;
using VDStudios.MagicEngine.Input;
using VDStudios.MagicEngine.SDL.Demo.Scenes;
using VDStudios.MagicEngine.SDL.Demo.Services;
using VDStudios.MagicEngine.SDL.Demo.Utilities;
using VDStudios.MagicEngine.World2D;
using Scancode = SDL2.NET.Scancode;

namespace VDStudios.MagicEngine.SDL.Demo.Nodes;

public class PlayerNode : EntityNode, IWorldMobile2D
{
    private readonly CharacterAnimationContainer AnimationContainer;
    private TextureOperation? RobinSprite;

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
            a.EndHang = TimeSpan.FromSeconds(1d / 6);

        AnimationContainer.Idle.StartHang
            = AnimationContainer.Idle.EndHang
            = AnimationContainer.Active.StartHang
            = AnimationContainer.Active.EndHang
            = TimeSpan.FromSeconds(1d / 2);
    }

    protected override async ValueTask<bool> Updating(TimeSpan delta)
    {
        await base.Updating(delta);

        Debug.Assert(RobinSprite is not null, "PlayerNode.SpriteOperation is unexpectedly null at the time of updating");

        Position += Direction * Speed;

        if (AnimationContainer.CurrentAnimation.Update()
            || AnimationContainer.SwitchTo(Helper.TryGetFromDirection(Direction, out var dir) ? dir : CharacterAnimationKind.Idle))
            RobinSprite.View = AnimationContainer.CurrentAnimation.CurrentElement;

        RobinSprite.TransformationState.Transform(translation: new Vector3(Position, 0));
        ((DemoScene)ParentScene).Camera.Goal.Transform(scale: new(4, 4, 1));
        RobinSprite.TextureEdgeOutline = new(RgbaVector.Blue.ToRGBAColor(), 2, true);
        RobinSprite.TextureCenterOutline = new(RgbaVector.Orange.ToRGBAColor(), 2, true);
        RobinSprite.TextureTopLeftOutline = new(RgbaVector.DarkRed.ToRGBAColor(), 2, true);
        RobinSprite.TextureTopRightOutline = new(RgbaVector.Grey.ToRGBAColor(), 2, true);
        RobinSprite.TextureBottomRightOutline = new(RgbaVector.Pink.ToRGBAColor(), 2, true);
        RobinSprite.TextureBottomLeftOutline = new(RgbaVector.Green.ToRGBAColor(), 2, true);

        Direction = default;

        return true;
    }

    protected override async ValueTask Attaching(Scene scene)
    {
        scene.Services.GetService<GameState>().PlayerNode = this;

        if (scene.GetDrawOperationManager<SDLGraphicsContext>(out var dopm))
        {
            RobinSprite = new TextureOperation(Game, c =>
            {
                using var stream = new MemoryStream(Animations.Robin);
                return Image.LoadTexture(c.Renderer, stream);
            }, AnimationContainer.CurrentAnimation.CurrentElement);

            await dopm.AddDrawOperation(RobinSprite);

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

        await base.Attaching(scene);
    }
}
