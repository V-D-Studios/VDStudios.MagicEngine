using System.Diagnostics;
using System.Numerics;
using SDL2.NET;
using SDL2.NET.SDLFont;
using SDL2.NET.Utilities;
using VDStudios.MagicEngine.DemoResources;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Graphics.SDL;
using VDStudios.MagicEngine.Graphics.SDL.DrawOperations;
using VDStudios.MagicEngine.SDL.Demo.Scenes;

namespace VDStudios.MagicEngine.SDL.Demo.Nodes;

public class HUDNode : Node
{
    private TextOperation? RobinPositionReport;
    private TextOperation? CameraPositionReport;
    private DelegateOperation? MidPointViewer;
    private GraphicsManagerFrameTimer GMTimer;

    public HUDNode(Game game) : base(game)
    {
        GMTimer = new(Game.MainGraphicsManager, 30);
    }

    protected override async ValueTask<bool> Updating(TimeSpan delta)
    {
        await base.Updating(delta);

        Debug.Assert(RobinPositionReport is not null, "HUDNode.TextOperation is unexpectedly null at the time of updating");
        Debug.Assert(CameraPositionReport is not null, "HUDNode.TextOperation is unexpectedly null at the time of updating");
        
        var state = ParentScene.Services.GetService<GameState>();
        Debug.Assert(state.PlayerNode is not null, "GameState.PlayerNode is unexpectedly null at the time of HUDNode updating");

        if (GMTimer.HasClocked)
        {
            var goal = ((DemoSceneBase)ParentScene).Camera.Goal;
            var scale = goal.Scale;
            var position = goal.Translation;

            RobinPositionReport.SetTextBlended($"Robin: {state.PlayerNode.Position: 0000.00;-0000.00}", RgbaVector.Black.ToRGBAColor(), 16);
            CameraPositionReport.SetTextBlended($"Camera, Position: {position: 0000.00;-0000.00}, Scale: {scale: 0000.00;-0000.00}", RgbaVector.Red.ToRGBAColor(), 16);
            GMTimer.Restart();
        }

        return true;
    }

    protected override async ValueTask Attaching(Scene scene)
    {
        if (scene.GetDrawOperationManager<SDLGraphicsContext>(out var dopm))
        {
            RobinPositionReport = new TextOperation(new TTFont(RWops.CreateFromMemory(new PinnedArray<byte>(Fonts.CascadiaCode), true, true), 16), Game);
            CameraPositionReport = new TextOperation(new TTFont(RWops.CreateFromMemory(new PinnedArray<byte>(Fonts.CascadiaCode), true, true), 16), Game);
            MidPointViewer = new DelegateOperation(Game, (dop, ts, c, r) =>
            {
                var ws = c.Window.Size;
                var x = ws.Width / 2 - 16;
                var y = ws.Height / 2 - 16;

                c.Renderer.DrawRectangle(new Rectangle(32, 32, x, y), RgbaVector.Yellow.ToRGBAColor());
            });

            await dopm.AddDrawOperation(RobinPositionReport, 1);
            await dopm.AddDrawOperation(CameraPositionReport, 1);
            await dopm.AddDrawOperation(MidPointViewer, 1);

            CameraPositionReport.TransformationState.Transform(translation: new Vector3(0, 20, 0));
        }
        else
            Debug.Fail("The attached scene did not have a DrawOperationManager for SDLGraphicsContext");
    }
}
