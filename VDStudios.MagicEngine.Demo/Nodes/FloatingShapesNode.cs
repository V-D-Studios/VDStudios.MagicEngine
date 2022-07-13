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
        float toffset = 1;
        float roffset = -1;

        Span<Vector2> triangle = stackalloc Vector2[] { new(-.5f + toffset, -.5f + toffset), new(.5f + toffset, -.5f + toffset), new(toffset, .5f + toffset) };
        Span<Vector2> rectangle = stackalloc Vector2[] { new(-1f + roffset, -.5f + roffset), new(-1f + roffset, .5f + roffset), new(1f + roffset, .5f + roffset), new(1f + roffset, -.5f + roffset) };

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
