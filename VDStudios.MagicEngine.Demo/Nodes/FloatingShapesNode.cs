﻿using System.Numerics;
using VDStudios.MagicEngine.Demo.Properties;
using VDStudios.MagicEngine.DrawLibrary;
using VDStudios.MagicEngine.DrawLibrary.Geometry;
using VDStudios.MagicEngine.Geometry;
using VDStudios.MagicEngine.GUILibrary.ImGUI;
using Veldrid;
using Veldrid.ImageSharp;

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

        private static ColorVertex Generate(int index, Vector2 shapeVertex, ShapeDefinition shape)
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

        /// <inheritdoc/>
        public void Start(ShapeRenderer<ColorVertex> renderer, IEnumerable<ShapeDefinition> allShapes, int regenCount, ref object? context) { }

        /// <inheritdoc/>
        public void Generate(ShapeDefinition shape, IEnumerable<ShapeDefinition> allShapes, Span<ColorVertex> vertices, CommandList commandList, DeviceBuffer vertexBuffer, int index, out bool useDeviceBuffer, ref object? context)
        {
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = Generate(i, shape[i], shape);
            useDeviceBuffer = false;
        }

        /// <inheritdoc/>
        public void Stop(ShapeRenderer<ColorVertex> renderer, ref object? context) { }
    }

    private readonly CircleDefinition circle;
    private readonly PolygonDefinition hexagon;
    private readonly SegmentDefinition segment;

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

        segment = new(new(.2f, .3f), new(-.4f, -.1f), 10f);

        var watch = new Watch(viewLoggers: new() { () => { segment.ForceUpdate(); return true; } });
        Game.MainGraphicsManager.AddElement(watch);

        // Apparently, oddly numbered polygons have their last vertex skipped?

        Span<Vector2> rectangle = stackalloc Vector2[]
        {
            new(-.15f - .5f, -.15f - .5f),
            new(-.15f - .5f, .15f - .5f),
            new(.15f - .5f, .15f - .5f),
            new(.15f - .5f, -.15f - .5f)
        };
        DrawOperationManager = new DrawOperationManager(this);

        var circ = PolygonDefinition.Circle(new(-.2f, .15f), .3f, 5);
        circ.Name = "Circle";

        circle = new CircleDefinition(Vector2.Zero, .65f);
        var texturedRect = PolygonDefinition.Circle(new(.25f, .25f), .25f, 21844);

        var robstrm = new MemoryStream(Assets.boundary_test);
        var img = new ImageSharpTexture(robstrm);
        var tsr = DrawOperationManager.AddDrawOperation(new TexturedShapeRenderer<Vector2>(
            img,
            new ShapeDefinition[]
            {
                texturedRect,
                segment
            },
            new(
                new(
                    BlendStateDescription.SingleAlphaBlend,
                    DepthStencilStateDescription.DepthOnlyLessEqual,
                    FaceCullMode.Front,
                    FrontFace.Clockwise,
                    true,
                    false,
                    PolygonRenderMode.TriangulatedFill,
                    new VertexLayoutDescription(
                        new VertexElementDescription("TexturePosition", VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate),
                        new VertexElementDescription("Position", VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate)
                    ),
                    null,
                    null,
                    GraphicsManager.AddWindowAspectTransform
                ),
                new(
                    SamplerAddressMode.Clamp,
                    SamplerAddressMode.Clamp,
                    SamplerAddressMode.Clamp,
                    SamplerFilter.MinPoint_MagPoint_MipPoint,
                    null,
                    0,
                    0,
                    0,
                    0,
                    SamplerBorderColor.TransparentBlack
                )
            ),
            new TextureVertexGeneratorFill()) { PreferredPriority = -2 }
        );

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
                new(ShaderStages.Vertex, FSNVertex.GetUTF8Bytes(), "main"),
                new(ShaderStages.Fragment, FSNFragment.GetUTF8Bytes(), "main"),
                GraphicsManager.AddWindowAspectTransform
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
    private TimeSpan tb;
    private static readonly TimeSpan tb_ceil = TimeSpan.FromSeconds(1.5);
    private int x = 0;
    private readonly int[] SubDivSeq = Enumerable.Range(3, 60).ToArray();
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
