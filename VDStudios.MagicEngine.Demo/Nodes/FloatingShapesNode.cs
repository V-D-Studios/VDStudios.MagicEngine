using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using VDStudios.MagicEngine.DrawLibrary.Primitives;
using VDStudios.MagicEngine.Geometry;
using VDStudios.MagicEngine.GUILibrary.ImGUI;
using Veldrid;

namespace VDStudios.MagicEngine.Demo.Nodes;
public class FloatingShapesNode : Node, IDrawableNode
{
    CircumferenceDefinition circumference;

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
        
        // Apparently, oddly numbered polygons have their last vertex skipped?

        Span<Vector2> rectangle = stackalloc Vector2[]
        {
            new(-.15f - .5f, -.15f - .5f),
            new(-.15f - .5f, .15f - .5f),
            new(.15f - .5f, .15f - .5f),
            new(.15f - .5f, -.15f - .5f)
        };

        var circ = PolygonDefinition.Circle(new(-.2f, .15f), .3f, 5);
        circ.Name = "Circle";

        circumference = new CircumferenceDefinition(new(-.7f, .6f), .65f);

        var watch = new Watch("Circle division watch", new()
        {
            ([NotNullWhen(true)] out string? x) =>
            {
                x = circumference.Subdivisions.ToString();
                return true;
            }
        });
        Game.MainGraphicsManager.AddElement(watch);

        DrawOperationManager = new DrawOperationManagerDrawQueueDelegate(this, (q, o) =>
        {
            q.Enqueue(o, -1);
        });
        DrawOperationManager.AddDrawOperation(new ShapeBuffer(new ShapeDefinition[]
        {
            new PolygonDefinition(triangle, true) { Name = "Triangle" },
            new PolygonDefinition(hexagon, true) { Name = "Hexagon" },
            new PolygonDefinition(rectangle, true) { Name = "Rectangle" },
            circ,
            circumference
        }, new() { RenderMode = PolygonRenderMode.TriangulatedFill }));
    }

    TimeSpan tb;
    static readonly TimeSpan tb_ceil = TimeSpan.FromSeconds(3);
    int x = 0;
    protected override ValueTask<bool> Updating(TimeSpan delta)
    {
        Span<int> sequence = stackalloc int[] { 3, 6, 9, 12, 15, 18, 21, 18, 15, 12, 9, 6 };
        tb += delta;
        if (tb > tb_ceil)
        {
            tb = default;
            if (x >= sequence.Length)
                x = 0;
            circumference.Subdivisions = sequence[x++];
        }
        return ValueTask.FromResult(true);
    }

    public DrawOperationManager DrawOperationManager { get; }

    public bool SkipDrawPropagation { get; }
}
