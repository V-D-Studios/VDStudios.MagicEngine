using System.Diagnostics;
using System.Numerics;
using SDL2.NET;
using SDL2.NET.SDLFont;
using SDL2.NET.Utilities;
using VDStudios.MagicEngine.DemoResources;
using VDStudios.MagicEngine.Graphics.SDL;
using VDStudios.MagicEngine.Graphics.SDL.DrawOperations;
using VDStudios.MagicEngine.SDL.Demo.Scenes;
using VDStudios.MagicEngine.Demo.Common.Services;
using VDStudios.MagicEngine.Graphics;

namespace VDStudios.MagicEngine.SDL.Demo.Nodes;

public class HUDNode : Node
{
    private readonly TextOperation RecordingNotif;
    private readonly TextOperation RobinPositionReport;
    private readonly TextOperation CameraPositionReport;
    private readonly DelegateOperation MidPointViewer;

    private GraphicsManagerFrameTimer GMTimer;

    public HUDNode(Game game) : base(game)
    {
        GMTimer = new(Game.MainGraphicsManager, 30);

        RecordingNotif = new TextOperation(new TTFont(RWops.CreateFromMemory(new PinnedArray<byte>(Fonts.CascadiaCode), true, true), 16), Game);
        RobinPositionReport = new TextOperation(new TTFont(RWops.CreateFromMemory(new PinnedArray<byte>(Fonts.CascadiaCode), true, true), 16), Game);
        CameraPositionReport = new TextOperation(new TTFont(RWops.CreateFromMemory(new PinnedArray<byte>(Fonts.CascadiaCode), true, true), 16), Game);
        MidPointViewer = new DelegateOperation(Game, (dop, ts, c, r) =>
        {
            var ws = c.Window.Size;
            var x = ws.Width / 2 - 16;
            var y = ws.Height / 2 - 16;

            c.Renderer.DrawRectangle(new Rectangle(32, 32, x, y), RgbaVector.Yellow.ToRGBAColor());
        });
    }

    protected override async ValueTask<bool> Updating(TimeSpan delta)
    {
        await base.Updating(delta);

        var state = ParentScene.Services.GetService<GameState>();
        Debug.Assert(state.PlayerNode is not null, "GameState.PlayerNode is unexpectedly null at the time of HUDNode updating");
        
        if (GMTimer.HasClocked)
        {
            var goal = ((DemoSceneBase)ParentScene).Camera.Goal;
            var scale = goal.Scale;
            var position = goal.Translation;

            //RecordingNotif.SetTextBlended(state.IsRecording ? "Recording..." : "", RgbaVector.DarkRed.ToRGBAColor(), 16);
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
            await dopm.AddDrawOperation(RobinPositionReport, RenderTargetList.GUI);
            await dopm.AddDrawOperation(CameraPositionReport, RenderTargetList.GUI);
            await dopm.AddDrawOperation(RecordingNotif, RenderTargetList.GUI);
            await dopm.AddDrawOperation(MidPointViewer, RenderTargetList.GUI);

            MidPointViewer.IsActive = false;

            CameraPositionReport.TransformationState.Transform(translation: new Vector3(0, 20, 0));
            RecordingNotif.TransformationState.Transform(translation: new Vector3(0, 40, 0));
        }
        else
            Debug.Fail("The attached scene did not have a DrawOperationManager for SDLGraphicsContext");
    }
}
