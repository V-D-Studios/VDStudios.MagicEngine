using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using VDStudios.MagicEngine.DrawLibrary.Primitives;

namespace VDStudios.MagicEngine.Demo.Nodes;
public class FloatingShapesNode : Node, IDrawableNode
{
    public FloatingShapesNode()
    {
        float toffset = .25f;
        float roffset = -.55f;

        Span<Vector2> triangle = stackalloc Vector2[] { new(-.25f + toffset, -.25f + toffset), new(.25f + toffset, -.25f + toffset), new(toffset, .25f + toffset) };
        Span<Vector2> rectangle = stackalloc Vector2[] { new(-.25f + roffset, -.25f + roffset), new(-.5f + roffset, .25f + roffset), new(.5f + roffset, .25f + roffset), new(.5f + roffset, -.25f + roffset) };

        DrawOperationManager = new DrawOperationManagerDrawQueueDelegate(this, (q, o) =>
        {
            q.Enqueue(o, -1);
        });
        DrawOperationManager.AddDrawOperation(new PolygonList(new PolygonDefinition[]
        {
            new(triangle),
            new(rectangle)
        }));
    }

    public DrawOperationManager DrawOperationManager { get; }

    public bool SkipDrawPropagation { get; }
}
