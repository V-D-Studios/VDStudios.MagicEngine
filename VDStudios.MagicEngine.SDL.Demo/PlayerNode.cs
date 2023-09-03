using System.Diagnostics;
using System.Numerics;
using SDL2.NET;
using SDL2.NET.Input;
using SDL2.NET.SDLFont;
using SDL2.NET.SDLImage;
using SDL2.NET.Utilities;
using VDStudios.MagicEngine.DemoResources;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Graphics.SDL;
using VDStudios.MagicEngine.Graphics.SDL.DrawOperations;
using VDStudios.MagicEngine.Input;
using VDStudios.MagicEngine.World2D;
using Scancode = SDL2.NET.Scancode;

namespace VDStudios.MagicEngine.SDL.Demo;

public class PlayerNode : Node, IWorldMobile2D
{
    private readonly CharacterAnimationContainer AnimationContainer;
    private TextureOperation? RobinSprite;
    private TextOperation? RobinPositionReport;
    private TextOperation? CameraPositionReport;
    private DelegateOperation? MidPointViewer;
    private GraphicsManagerFrameTimer GMTimer;

    public float Speed { get; } = .3f;
    public Vector2 Direction { get; private set; } = Vector2.Zero;
    public Vector2 Position { get; private set; } = default;
    public Vector2 Size { get; private set; } = new(32, 32);

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

        GMTimer = new(Game.MainGraphicsManager, 30);
    }

    protected override async ValueTask<bool> Updating(TimeSpan delta)
    {
        Debug.Assert(RobinSprite is not null, "PlayerNode.SpriteOperation is unexpectedly null at the time of updating");
        Debug.Assert(RobinPositionReport is not null, "PlayerNode.TextOperation is unexpectedly null at the time of updating");
        Debug.Assert(CameraPositionReport is not null, "PlayerNode.TextOperation is unexpectedly null at the time of updating");
        Debug.Assert(ParentScene is not null, "PlayerNode.ParentScene is unexpectedly null at the time of updating");

        if (Keyboard.KeyStates[Scancode.W].IsPressed)
            Direction += Directions.Up;
        if (Keyboard.KeyStates[Scancode.A].IsPressed)
            Direction += Directions.Left;
        if (Keyboard.KeyStates[Scancode.S].IsPressed)
            Direction += Directions.Down;
        if (Keyboard.KeyStates[Scancode.D].IsPressed)
            Direction += Directions.Right;

        if (Keyboard.KeyStates[Scancode.F12].IsPressed)
        {
            var scdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MagicEngine Screenshots");
            Directory.CreateDirectory(scdir);
            using var stream = File.Open(Path.Combine(scdir, $"{Guid.NewGuid()}.png"), FileMode.Create);
            await RobinSprite.Manager!.TakeScreenshot(stream, Utility.ScreenshotImageFormat.PNG);
        }

        Position += Direction * Speed;

        if (AnimationContainer.CurrentAnimation.Update()
            || AnimationContainer.SwitchTo(Helper.TryGetFromDirection(Direction, out var dir) ? dir : CharacterAnimationKind.Idle))
            RobinSprite.View = AnimationContainer.CurrentAnimation.CurrentElement;

        RobinSprite.TransformationState.Transform(translation: new Vector3(Position, 0));

        Direction = default;

        if (GMTimer.HasClocked)
        {
            var goal = ((DemoScene)ParentScene).Camera.Goal;
            var scale = goal.Scale;
            var position = goal.Translation;

            RobinPositionReport.SetTextBlended($"Robin: {Position: 0000.00;-0000.00}", RgbaVector.Black.ToRGBAColor(), 16);
            CameraPositionReport.SetTextBlended($"Camera, Position: {position: 0000.00;-0000.00}, Scale: {scale: 0000.00;-0000.00}", RgbaVector.Red.ToRGBAColor(), 16);
            GMTimer.Restart();
        }

        return await base.Updating(delta);
    }

    protected override async ValueTask Attaching(Scene scene)
    {
        if (scene.GetDrawOperationManager<SDLGraphicsContext>(out var dopm))
        {
            RobinSprite = new TextureOperation(Game, c =>
            {
                using var stream = new MemoryStream(Animations.Robin);
                return Image.LoadTexture(c.Renderer, stream);
            }, AnimationContainer.CurrentAnimation.CurrentElement);

            RobinPositionReport = new TextOperation(new TTFont(RWops.CreateFromMemory(new PinnedArray<byte>(Fonts.CascadiaCode), true, true), 16), Game);
            CameraPositionReport = new TextOperation(new TTFont(RWops.CreateFromMemory(new PinnedArray<byte>(Fonts.CascadiaCode), true, true), 16), Game);
            MidPointViewer = new DelegateOperation(Game, (dop, ts, c, r) =>
            {
                var ws = c.Window.Size;
                var x = ws.Width / 2 - 16;
                var y = ws.Height / 2 - 16;

                c.Renderer.DrawRectangle(new Rectangle(32, 32, x, y), RgbaVector.Yellow.ToRGBAColor());
            });

            await dopm.AddDrawOperation(RobinSprite);
            await dopm.AddDrawOperation(RobinPositionReport, 1);
            await dopm.AddDrawOperation(CameraPositionReport, 1);
            await dopm.AddDrawOperation(MidPointViewer, 1);

            RobinSprite.TextureOutlineColor = RgbaVector.Blue.ToRGBAColor();

            CameraPositionReport.TransformationState.Transform(translation: new Vector3(0, 20, 0));
            //RobinSprite.TransformationState.Transform(scale: new Vector3(4, 4, 1));
        }
        else
            Debug.Fail("The attached scene did not have a DrawOperationManager for SDLGraphicsContext");

        await base.Attaching(scene);
    }
}
