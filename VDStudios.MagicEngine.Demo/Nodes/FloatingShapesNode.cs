using System.Numerics;
using System.Runtime.InteropServices;
using VDStudios.MagicEngine.Demo.Properties;
using VDStudios.MagicEngine.DrawLibrary;
using VDStudios.MagicEngine.DrawLibrary.Geometry;
using VDStudios.MagicEngine.Geometry;
using VDStudios.MagicEngine.GUILibrary.ImGUI;
using VDStudios.MagicEngine.Utility;
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

    private class ColorVertexGenerator : IShape2DRendererVertexGenerator<ColorVertex>
    {
        private static readonly RgbaFloat[] Colors = new RgbaFloat[]
        {
            new(1f, .2f, .2f, 1f),
            new(.2f, 1f, .2f, 1f),
            new(.2f, .2f, 1f, 1f),
        };

        private static ColorVertex Generate(int index, Vector2 shapeVertex, ShapeDefinition2D shape)
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
        public void Start(ShapeRenderer<ColorVertex> renderer, IEnumerable<ShapeDefinition2D> allShapes, int regenCount, ref object? context) { }

        /// <inheritdoc/>
        public void Generate(ShapeDefinition2D shape, IEnumerable<ShapeDefinition2D> allShapes, Span<ColorVertex> vertices, CommandList commandList, DeviceBuffer vertexBuffer, int index, uint vertexStart, uint vertexSize, out bool useDeviceBuffer, ref object? context)
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
    private readonly TexturedShapeRenderer<Vector2> TexturedRenderer;
    private readonly ShapeRenderer<ColorVertex> Renderer;

    public FloatingShapesNode()
    {
        DrawOperationManager = new DrawOperationManager(this);

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

        var elipseTall = new ElipseDefinition(new(0f, 0f), .2f, .65f, 30) { Name = "Tall Elipse" };
        var elipseWide = new ElipseDefinition(new(0f, 0f), .65f, .2f, 30) { Name = "Wide Elipse" };
        var donut = new DonutDefinition(new(.2f, .2f), .2f, .1f, 30, 3);

        var texturedRect = PolygonDefinition.Circle(new(.25f, .25f), .25f, 21844);

        var robstrm = new MemoryStream(Assets.boundary_test);
        var img = new ImageSharpTexture(robstrm);
        robstrm.Dispose();

        TexturedShapeRenderDescription shapeRendererDesc = new(
                new(
                    BlendStateDescription.SingleAlphaBlend,
                    DepthStencilStateDescription.DepthOnlyLessEqual,
                    FaceCullMode.Front,
                    FrontFace.Clockwise,
                    true,
                    false,
                    PolygonRenderMode.TriangulatedWireframe,
                    null,
                    null,
                    null,
                    GraphicsManager.AddWindowAspectTransform
                ),
                new SamplerDescription(
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
                ),
                default(TextureViewDescription)
            );

        TexturedRenderer = DrawOperationManager.AddDrawOperation(new TexturedShapeRenderer<Vector2>(
            img,
            new ShapeDefinition2D[]
            {
                //texturedRect,
                donut
            },
            shapeRendererDesc,
            new TextureVertexGeneratorFill()) { PreferredPriority = -2 }
        );

        TexturedRenderer.WaitUntilReady();
        var wireframePipeline = TexturedRenderer.Pipeline;
        TexturedShapeRenderer<Vector2>.ConfigureDescription(TexturedRenderer.Manager, TexturedRenderer.Manager.Device.ResourceFactory, ref shapeRendererDesc.ShapeRendererDescription);
        shapeRendererDesc.ShapeRendererDescription.RenderMode = PolygonRenderMode.TriangulatedFill;
        var fillPipeline = TexturedShapeRenderer<Vector2>.CreatePipeline(
            TexturedRenderer.Manager, 
            TexturedRenderer.Manager.Device, 
            TexturedRenderer.Manager.Device.ResourceFactory,
            wireframePipeline.ResourceLayouts.ToArray(),
            shapeRendererDesc.ShapeRendererDescription
        );

        bool isWireframe = true;
        var watch = new Watch(viewLoggers: new() 
        {
            ("Force Donut update", () => { donut.ForceRegenerate(); return true; }),
            ("Toggle Donut Pipeline", () => 
            {
                if (isWireframe)
                {
                    TexturedRenderer.Pipeline = fillPipeline;
                    isWireframe = false;
                }
                else
                {
                    TexturedRenderer.Pipeline = wireframePipeline;
                    isWireframe = true;
                }
                return true;
            })
        });

        Game.MainGraphicsManager.AddElement(watch);

        // Apparently, oddly numbered polygons have their last vertex skipped?

        Span<Vector2> rectangle = stackalloc Vector2[]
        {
            new(-.15f - .5f, -.15f - .5f),
            new(-.15f - .5f, .15f - .5f),
            new(.15f - .5f, .15f - .5f),
            new(.15f - .5f, -.15f - .5f)
        };
        
        var circ1 = new CircleDefinition(new(-.2f, .15f), .3f, 7) { Name = "Circle 1" };
        var circ2 = new CircleDefinition(new(.2f, -.15f), .3f, 8) { Name = "Circle 2" };
        circle = new CircleDefinition(Vector2.Zero, .65f);

        Renderer = DrawOperationManager.AddDrawOperation(new ShapeRenderer<ColorVertex>(
            new ShapeDefinition2D[]
            {
                hexagon,
                new PolygonDefinition(rectangle, true) { Name = "Rectangle" },
                new PolygonDefinition(triangle, true) { Name = "Triangle" },
                circle,
                circ1,
                circ2,
                elipseTall,
                elipseWide,
                //donut
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
                null,
                null,
                GraphicsManager.AddWindowAspectTransform
            ),
            new ColorVertexGenerator())
            { VertexSkip = ElementSkip.ElementsToMaintain(100) }
        );

        GameDeferredCallsSchedule.Schedule(DeferredTest, 12);
    }

    private void DeferredTest()
    {
        Log.Information("I was deferred for 100 frames");
        GameDeferredCallsSchedule.Schedule(DeferredTest, 100);
    }

    private TimeSpan tb;
    private float rot;
    private float sca;
    private float rotspeed = 1f / 1000;
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
            TexturedRenderer.Transform(translation: new(-.2f, -.2f, 1), scale: new(2, 2, 1));
            //wTexturedRenderer.ColorTransformation = ColorTransformation.CreateOverlay(RgbaFloat.Black);
            //TexturedRenderer.ColorTransformation = Random.Next(0, 100) switch
            //{
            //    < 25 => ColorTransformation.CreateTint(GenNewColor()).WithOpacity(.87f),
            //    < 50 => ColorTransformation.CreateOverlay(GenNewColor()).WithOpacity(.87f),
            //    < 75 => ColorTransformation.CreateTintAndOverlay(GenNewColor(), GenNewColor()).WithOpacity(.87f),
            //    _    => ColorTransformation.CreateOpacity(.87f),
            //};

            //GameDeferredCallsSchedule.Schedule(() => Log.Information("I was deferred for 1 second"), TimeSpan.FromSeconds(1));
        }
        var rotation = new Vector4(-.1f, -.1f, 0f, rot += rotspeed * (float)delta.TotalMilliseconds);
        sca = (((rotspeed * (float)(delta.TotalMilliseconds))) + sca) % 1.5f;
        //TexturedRenderer.Transform(rotZ: rotation);
        //Renderer.Transform(scale: new(sca, sca, 1));

        return ValueTask.FromResult(true);
    }

    private unsafe RgbaFloat GenNewColor()
        => new(
            r: Random.NextSingle(),
            g: Random.NextSingle(),
            b: Random.NextSingle(),
            a: 1
        );

    public DrawOperationManager DrawOperationManager { get; }

    public bool SkipDrawPropagation { get; }
}
