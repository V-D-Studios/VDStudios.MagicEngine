using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VDStudios.MagicEngine.Demo.GUI.Elements;
using VDStudios.MagicEngine.Demo.Nodes;
using VDStudios.MagicEngine.Templates;

namespace VDStudios.MagicEngine.Demo.Scenes;
public sealed class DemoScene : Scene
{
    public DemoScene()
    {
        
    }

    protected override async ValueTask ConfigureScene()
    {
        await Attach(new ColorBackgroundNode());

        int top = Random.Shared.Next(1, 6);
        for (int t = 0; t < top; t++)
        {
            var tel = TemplatedNode.New<TestNode>(configuratorNode);

            int mid = Random.Shared.Next(0, 6);
            for (int m = 0; m < mid; m++)
            {
                tel.AddChild<TestNode>(out var mel);

                int bot = Random.Shared.Next(0, 6);
                for (int b = 0; b < bot; b++)
                    mel.AddChild<TestNode>();
            }

            await Attach(await tel.Instance());
        }

        //await Attach(new PlayerNode());

        // The following code can (and should) go somewhere else
        top = Random.Shared.Next(1, 6);

        Game.MainGraphicsManager.AddElement(new ImGUIDemo());

        Game.MainGraphicsManager.AddElement(new FPSWatch());
        Game.MainGraphicsManager.AddElement(new UPSWatch());

        for (int t = 0; t < top; t++)
        {
            var tel = TemplatedGUIElement.New<TestElement>(configuratorElement);

            int mid = Random.Shared.Next(0, 6);
            for (int m = 0; m < mid; m++)
            {
                tel.AddSubElement<TestElement>(out var mel);

                int bot = Random.Shared.Next(0, 6);
                for (int b = 0; b < bot; b++)
                    mel.AddSubElement<TestElement>();
            }

            tel.Instance(Game.MainGraphicsManager);
        }
    }

    public static void configuratorNode(Node e) { }
    public static object? configuratorElement(GUIElement e) => null;
}
