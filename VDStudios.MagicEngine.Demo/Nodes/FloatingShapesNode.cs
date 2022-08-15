using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using VDStudios.MagicEngine.DrawLibrary.Geometry;
using VDStudios.MagicEngine.Geometry;
using VDStudios.MagicEngine.GUILibrary.ImGUI;
using Veldrid;

namespace VDStudios.MagicEngine.Demo.Nodes;
public class FloatingShapesNode : Node, IDrawableNode
{
    private struct ColorVertex
    {
        public Vector2 Position;
        public RgbaFloat Color;
    }

    private class ColorVertexGenerator : IShapeRendererVertexGenerator<ColorVertex>
    {
        private static readonly RgbaFloat[] Colors = new RgbaFloat[]
        {
            new(1f, .2f, .2f, 1f),
            new(.2f, 1f, .2f, 1f),
            new(.2f, .2f, 1f, 1f),
        };

        public ColorVertex Generate(int index, Vector2 shapeVertex, ShapeDefinition shape)
        {
            if (shape.Count is 3)
                goto Preset;
            if (shape.Count is 4)
            {
                if (index is >= 2)
                    index--;
                goto Preset;
            }

            var x = shape.Count / 3d;
            return new() { Position = shapeVertex, Color = Colors[index < x ? 0 : index > x * 2 ? 2 : 1] };

        Preset:
            return new() { Position = shapeVertex, Color = Colors[index] };
        }
    }

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
        DrawOperationManager.AddDrawOperation(new ShapeRenderer<ColorVertex>(
            new ShapeDefinition[]
            {
                new PolygonDefinition(triangle, true) { Name = "Triangle" },
                hexagon,
                new PolygonDefinition(rectangle, true) { Name = "Rectangle" },
                circ,
                circle
            }, 
            new(
                BlendStateDescription.SingleAlphaBlend,
                DepthStencilStateDescription.DepthOnlyLessEqual,
                FaceCullMode.Front,
                FrontFace.Clockwise,
                true,
                false,
                PolygonRenderMode.TriangulatedFill,
                new VertexLayoutDescription(
                    new VertexElementDescription("Position", VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate),
                    new VertexElementDescription("Color", VertexElementFormat.Float4, VertexElementSemantic.TextureCoordinate)
                ),
                new(ShaderStages.Fragment, FSNFragment.GetUTF8Bytes(), "main"),
                new(ShaderStages.Vertex, FSNVertex.GetUTF8Bytes(), "main"),
                static (GraphicsManager m, GraphicsDevice d, ResourceFactory f, out ResourceLayout[] l, out ResourceSet[] s) =>
                {
                    l = new ResourceLayout[] { m.WindowAspectTransformLayout };
                    s = new ResourceSet[] { m.WindowAspectTransformSet };
                }
            ),
            new ColorVertexGenerator())
        );
    }

    private static readonly string FSNFragment = @"
#version 450

layout(location = 0) out vec4 fsout_Color;
layout(location = 0) in vec4 fsin_Color;

void main() {
    fsout_Color = fsin_Color;
}
";

    private static readonly string FSNVertex = @"
#version 450

layout(location = 0) in vec2 Position;
layout(location = 1) in vec4 Color;
layout(location = 0) out vec4 fsin_Color;
layout(binding = 0) uniform WindowAspectTransform {
    layout(offset = 0) mat4 WindowScale;
};

void main() {
    fsin_Color = Color;
    gl_Position = WindowScale * vec4(Position, 0.0, 1.0);
}
";

    TimeSpan tb;
    static readonly TimeSpan tb_ceil = TimeSpan.FromSeconds(1.5);
    int x = 0;
    readonly int[] SubDivSeq = Enumerable.Range(3, 60).ToArray();
    protected override ValueTask<bool> Updating(TimeSpan delta)
    {
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
