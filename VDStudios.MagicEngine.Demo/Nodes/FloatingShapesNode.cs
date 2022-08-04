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
    CircleDefinition circle;
    PolygonDefinition hexagon;

    public FloatingShapesNode()
    {
        Span<Vector2> triangle = stackalloc Vector2[] { new(-.15f + .5f, -.15f + .5f), new(.15f + .5f, -.15f + .5f), new(.5f, .15f + .5f) };
        hexagon = new PolygonDefinition(stackalloc Vector2[]
        {
            new(330.71074380165294f / 500f, 494.82644628099155f / 500f),
            new(539.801652892562f / 500f, 439.4545454545454f / 500f),
            new(626.876207061902f / 500f, 241.4568745545897f / 500f),
            new(526.365491022952f / 500f, 49.92971818522767f / 500f),
            new(313.956123998003f / 500f, 9.09693153458295f / 500f),
            new(149.59669171830413f / 500f, 149.7064357876441f / 500f),
            new(157.05319901188642f / 500f, 365.87640633068054f / 500f)
        }, true) { Name = "Hexagon" };

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

        circle = new CircleDefinition(Vector2.Zero, .65f);

        var watch = new Watch("Circle division watch", new()
        {
            new Watch.DelegateViewer(([NotNullWhen(true)] out string? x) =>
            {
                x = circle.Subdivisions.ToString();
                return true;
            })
        },
        new()
        {
            () =>
            {
                Log.Debug("Flagged for circle subdivided in {parts} parts", circle.Subdivisions);
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
            hexagon,
            new PolygonDefinition(rectangle, true) { Name = "Rectangle" },
            circ,
            circumference
        }, new() { RenderMode = PolygonRenderMode.TriangulatedFill }));
    }

    TimeSpan tb;
    static readonly TimeSpan tb_ceil = TimeSpan.FromSeconds(1.5);
    int x = 0;
    readonly int[] SubDivSeq = Enumerable.Range(3, 60).ToArray();
    Vector2 offset;
    float xoff = .01f;
    protected override ValueTask<bool> Updating(TimeSpan delta)
    {
        if (offset.X <= -1)
            xoff = .01f;
        else if (offset.X >= 1) 
            xoff = -.01f;

        offset.X += xoff * (float)delta.TotalSeconds;

        hexagon.Transform(Matrix3x2.CreateTranslation(offset));

        tb += delta;
        if (tb > tb_ceil)
        {
            tb = default;
            if (x >= SubDivSeq.Length)
                x = 0;
            circle.Subdivisions = SubDivSeq[x++];
        }

        return ValueTask.FromResult(true);
    }

    public DrawOperationManager DrawOperationManager { get; }

    public bool SkipDrawPropagation { get; }
}
