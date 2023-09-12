using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDStudios.MagicEngine.Geometry;
using VDStudios.MagicEngine.Graphics.Veldrid;
using VDStudios.MagicEngine.Graphics.Veldrid.DrawOperations;

namespace VDStudios.MagicEngine.Veldrid.Demo.Nodes;
public class GraphicsTestNode : Node
{
    public Shape2DRenderer ShapeRenderer;

    public GraphicsTestNode(Game game) : base(game)
    {
        ShapeRenderer = new Shape2DRenderer(new CircleDefinition(new Vector2(0, 0), 4), game);
    }

    protected override async ValueTask Attaching(Scene scene)
    {
        await base.Attaching(scene);

        if (scene.GetDrawOperationManager<VeldridGraphicsContext>(out var drawOperationManager) is false)
            Debug.Fail("Could not find a DrawOperationManager for VeldridGraphicsContext");

        await drawOperationManager.AddDrawOperation(ShapeRenderer);
    }
}
