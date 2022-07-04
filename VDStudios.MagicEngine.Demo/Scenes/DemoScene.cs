using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDStudios.MagicEngine.Demo.GUI.Elements;
using VDStudios.MagicEngine.Demo.Nodes;

namespace VDStudios.MagicEngine.Demo.Scenes;
public sealed class DemoScene : Scene
{
    public DemoScene()
    {
        
    }

    protected override async ValueTask ConfigureScene()
    {
        await Attach(new ColorBackgroundNode());
        //await Attach(new PlayerNode());

        // The following code can (and should) go somewhere else
        int top = Random.Shared.Next(0, 6);
        int mid = Random.Shared.Next(0, 6);
        int bot = Random.Shared.Next(0, 6);

        for (int t = 0; t < top; t++)
        {
            var tel = new TestElement();
            Game.MainGraphicsManager.AddElement(tel);
            
            for (int m = 0; m < mid; m++)
            {
                var mel = new TestElement();
                tel.AddElement(mel);

                for (int b = 0; b < bot; b++)
                {
                    var bel = new TestElement();
                    mel.AddElement(bel);
                }
            }
        }
    }
}
