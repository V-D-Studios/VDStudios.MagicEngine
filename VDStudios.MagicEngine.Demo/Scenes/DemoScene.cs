using System.Collections;
using System.Collections.Concurrent;
using System.Numerics;
using VDStudios.MagicEngine.Demo.GUI.Elements;
using VDStudios.MagicEngine.Demo.Nodes;
using VDStudios.MagicEngine.GUILibrary.ImGUI;
using VDStudios.MagicEngine.RenderTargets;
using System.Linq;

namespace VDStudios.MagicEngine.Demo.Scenes;

public sealed class DemoScene : Scene
{
    public DemoScene()
    {
    }

    private PassthroughCamera2D Camera;

    protected override async ValueTask ConfigureScene()
    {
        Log.Information("Configuring DemoScene");

        Log.Debug("Configuring Camera");

        Log.Verbose("Creating camera");
        Camera = new PassthroughCamera2D(Game.MainGraphicsManager, LinearInterpolator.Interpolator);
        Camera.CameraSpeedMultiplier = 2;

        Log.Verbose("Clearing cameras from MainGraphicsManager");
        Game.MainGraphicsManager.RenderTargets.Clear();

        Log.Verbose("Attaching camera to MainGraphicsManager");
        Game.MainGraphicsManager.RenderTargets.Add(Camera);

        Log.Debug("Attaching ColorBackgroundNode");

        await Attach(new ColorBackgroundNode());
        await Attach(new FloatingShapesNode());

        var lagger = new LaggerNode();
        await Attach(lagger);

        Log.Information("Adding FPS metrics to MainGraphicsManager GUI");
        Game.MainGraphicsManager.AddElement(new FPSWatch());
        Game.MainGraphicsManager.AddElement(new UPSWatch());
        Game.MainGraphicsManager.AddElement(new Watch("Lagger", null, new() 
        {
            ("Lag 7ms once", () =>
            {
                lagger.lags.Enqueue(TimeSpan.FromMilliseconds(7));
                return true;
            }),
            ("Lag 7ms 16 times", () =>
            {
                int i = 16;
                while (i-- > 0)
                    lagger.lags.Enqueue(TimeSpan.FromMilliseconds(7));
                return true;
            }),
            ("Lag 7ms 20 times", () =>
            {
                int i = 20;
                while (i-- > 0)
                    lagger.lags.Enqueue(TimeSpan.FromMilliseconds(7));
                return true;
            }),
            ("Lag 7ms 300 times", () =>
            {
                int i = 300;
                while (i-- > 0)
                    lagger.lags.Enqueue(TimeSpan.FromMilliseconds(7));
                return true;
            }),
        }));
    }

    private bool next = true;
    private int ind = -1;
    private readonly Vector2[] CamPos = new Vector2[]
    {
        new(0,0),
        new(-.5f,.5f),new(-1,1),new(0,0),
        new(.5f,.5f),new(1,1),new(0,0),
        new(.5f,-.5f),new(1,-1),new(0,0),
        new(-.5f,-.5f),new(-1,-1)
    };

    private readonly float[] CamRots = new float[]
    {
        0, .1f, .2f, .5f, .7f, 1f,
        -.1f, -.2f, -.5f, -.7f, -1f,
    };

    protected override ValueTask<bool> Updating(TimeSpan delta)
    {
        if (next is true)
        {
            if (Environment.GetCommandLineArgs().Contains("no-move-cam") is false)
                Camera.Position = CamPos[ind = (ind + 1) % CamPos.Length];

            if (Environment.GetCommandLineArgs().Contains("no-rot-cam") is false)
                Camera.Rotation = CamRots[ind = (ind + 1) % CamRots.Length];

            next = false;
            GameDeferredCallSchedule.ScheduleDeferredCall((ex, delta) => next = true, TimeSpan.FromSeconds(1));
        }
        return new ValueTask<bool>(true);
    }

    private sealed class LaggerNode : Node
    {
        public readonly ConcurrentQueue<TimeSpan> lags = new();

        protected override ValueTask<bool> Updating(TimeSpan delta)
        {
            if (lags.TryDequeue(out var delay))
                SDL2.Bindings.SDL.SDL_Delay(delay > TimeSpan.Zero ? (uint)delay.TotalMilliseconds : 0);
            return base.Updating(delta);
        }
    }

    public static void configuratorNode(Node e) { }
    public static object? configuratorElement(GUIElement e) => null;
}
