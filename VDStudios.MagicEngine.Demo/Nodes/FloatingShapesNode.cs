using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using VDStudios.MagicEngine.DrawLibrary.Primitives;
using Veldrid;

namespace VDStudios.MagicEngine.Demo.Nodes;
public class FloatingShapesNode : Node, IDrawableNode
{
    public FloatingShapesNode()
    {
        Span<Vector2> triangle = stackalloc Vector2[] { new(-.15f + .5f, -.15f + .5f), new(.15f + .5f, -.15f + .5f), new(.5f, .15f + .5f) };
        Span<Vector2> hexagon = stackalloc Vector2[]
        {
            new(330.71074380165294f / 500f - 0.5f, 494.82644628099155f / 500f - 0.5f),
            new(539.801652892562f / 500f - 0.5f, 439.4545454545454f / 500f - 0.5f),
            new(626.876207061902f / 500f - 0.5f, 241.4568745545897f / 500f - 0.5f),
            new(526.365491022952f / 500f - 0.5f, 49.92971818522767f / 500f - 0.5f),
            new(313.956123998003f / 500f - 0.5f, 9.09693153458295f / 500f - 0.5f),
            new(149.59669171830413f / 500f - 0.5f, 149.7064357876441f / 500f - 0.5f),
            new(157.05319901188642f / 500f - 0.5f, 365.87640633068054f / 500f - 0.5f)
        };
        
        DrawOperationManager = new DrawOperationManagerDrawQueueDelegate(this, (q, o) =>
        {
            q.Enqueue(o, -1);
        });
        DrawOperationManager.AddDrawOperation(new PolygonList(new PolygonDefinition[]
        {
            new(triangle),
            new(hexagon)
        }, new() { RenderMode = PolygonRenderMode.TriangulatedFill }));
    }

    public DrawOperationManager DrawOperationManager { get; }

    public bool SkipDrawPropagation { get; }
}
