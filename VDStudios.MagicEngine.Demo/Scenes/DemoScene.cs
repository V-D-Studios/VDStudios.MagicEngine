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

        Log.Information("Adding FPS metrics to MainGraphicsManager GUI");
        Game.MainGraphicsManager.AddElement(new FPSWatch());
        Game.MainGraphicsManager.AddElement(new UPSWatch());
    }

    public static void configuratorNode(Node e) { }
    public static object? configuratorElement(GUIElement e) => null;
}
