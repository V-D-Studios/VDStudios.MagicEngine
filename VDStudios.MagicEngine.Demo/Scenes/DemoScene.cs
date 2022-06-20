using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDStudios.MagicEngine.Demo.Nodes;

namespace VDStudios.MagicEngine.Demo.Scenes;
public sealed class DemoScene : Scene
{
    public DemoScene()
    {
        AttachNode(new PlayerNode());
    }
}
