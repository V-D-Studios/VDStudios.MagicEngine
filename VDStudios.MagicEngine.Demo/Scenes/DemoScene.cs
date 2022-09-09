using System.Collections.Concurrent;
using VDStudios.MagicEngine.Demo.Nodes;
using VDStudios.MagicEngine.GUILibrary.ImGUI;

namespace VDStudios.MagicEngine.Demo.Scenes;

public sealed class DemoScene : Scene
{
    public DemoScene()
    {
    }

    protected override async ValueTask ConfigureScene()
    {
        Log.Information("Configuring DemoScene");

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
