using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using SDL2.NET;
using VDStudios.MagicEngine.Geometry;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Graphics.Veldrid;
using VDStudios.MagicEngine.Graphics.Veldrid.DrawOperations;
using VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes;
using Veldrid;

namespace VDStudios.MagicEngine.Veldrid.Demo.Nodes;

public class GraphicsTestNode : Node
{
    public readonly Shape2DRenderer Circle;
    public readonly Shape2DRenderer Square;
    public readonly Shape2DRenderer Elipse;
    public readonly Shape2DRenderer Polygon;

    public readonly TexturedShape2DRenderer TexturedCircle;
    public readonly TexturedShape2DRenderer TexturedSquare;
    public readonly TexturedShape2DRenderer TexturedElipse;
    public readonly TexturedShape2DRenderer TexturedPolygon;

    public GraphicsManagerFrameTimer PipelineTimer;

    public GraphicsTestNode(Game game) : base(game)
    {
        Circle = new Shape2DRenderer(new CircleDefinition(new Vector2(0, 0), .6f, 100), game);
        Square = new Shape2DRenderer(new RectangleDefinition(new Vector2(0, 0), new Vector2(.7f, .8f)), game);
        Elipse = new Shape2DRenderer(new ElipseDefinition(new Vector2(0, 0), .6f, .3f, 100), game);
        Polygon = new Shape2DRenderer(new PolygonDefinition(stackalloc Vector2[]
        {
            new(330.71074380165294f / 500f, 494.82644628099155f / 500f),
            new(539.801652892562f / 500f, 439.4545454545454f / 500f),
            new(626.876207061902f / 500f, 241.4568745545897f / 500f),
            new(526.365491022952f / 500f, 49.92971818522767f / 500f),
            new(313.956123998003f / 500f, 9.09693153458295f / 500f),
            new(149.59669171830413f / 500f, 149.7064357876441f / 500f),
            new(157.05319901188642f / 500f, 365.87640633068054f / 500f)
        }, true), game);

        var vgc = (VeldridGraphicsManager)Game.MainGraphicsManager;
        var res = vgc.Resources;
        var textureCache = res.TextureCache;
        var samplerCache = res.SamplerCache;

        TexturedCircle = new TexturedShape2DRenderer(new CircleDefinition(new Vector2(0, 0), .6f, 100), game,
            textureFactory: textureCache.GetResource("baum").OwnerDelegate,
            samplerFactory: samplerCache.GetResource("default").ResourceDelegate,
            viewFactory: textureCache.GetResource("baum").GetResource("default").ResourceDelegate
        );
        TexturedSquare = new TexturedShape2DRenderer(new RectangleDefinition(new Vector2(0, 0), new Vector2(.7f, .8f)), game,
            textureFactory: textureCache.GetResource("baum").OwnerDelegate,
            samplerFactory: samplerCache.GetResource("default").ResourceDelegate,
            viewFactory: textureCache.GetResource("baum").GetResource("default").ResourceDelegate
        );
        TexturedElipse = new TexturedShape2DRenderer(new ElipseDefinition(new Vector2(0, 0), .6f, .3f, 100), game,
            textureFactory: textureCache.GetResource("baum").OwnerDelegate,
            samplerFactory: samplerCache.GetResource("default").ResourceDelegate,
            viewFactory: textureCache.GetResource("baum").GetResource("default").ResourceDelegate
        );
        TexturedPolygon = new TexturedShape2DRenderer(new PolygonDefinition(stackalloc Vector2[]
        {
            new(330.71074380165294f / 500f, 494.82644628099155f / 500f),
            new(539.801652892562f / 500f, 439.4545454545454f / 500f),
            new(626.876207061902f / 500f, 241.4568745545897f / 500f),
            new(526.365491022952f / 500f, 49.92971818522767f / 500f),
            new(313.956123998003f / 500f, 9.09693153458295f / 500f),
            new(149.59669171830413f / 500f, 149.7064357876441f / 500f),
            new(157.05319901188642f / 500f, 365.87640633068054f / 500f)
        }, true), game,
        textureFactory: textureCache.GetResource("baum").OwnerDelegate,
        samplerFactory: samplerCache.GetResource("default").ResourceDelegate,
        viewFactory: textureCache.GetResource("baum").GetResource("default").ResourceDelegate
    );

        PipelineTimer = new GraphicsManagerFrameTimer(Game.MainGraphicsManager, 60);
    }

    protected override ValueTask<bool> Updating(TimeSpan delta)
    {
        if (PipelineTimer.HasClocked)
        {
            Circle.PipelineIndex = Circle.PipelineIndex > 0u ? 0u : 1u;
            Square.PipelineIndex = Square.PipelineIndex > 0u ? 0u : 1u;
            Elipse.PipelineIndex = Elipse.PipelineIndex > 0u ? 0u : 1u;
            Polygon.PipelineIndex = Polygon.PipelineIndex > 0u ? 0u : 1u;

            PipelineTimer.Restart();
        }

        return base.Updating(delta);
    }

    protected override async ValueTask Attaching(Scene scene)
    {
        await base.Attaching(scene);

        if (scene.GetDrawOperationManager<VeldridGraphicsContext>(out var drawOperationManager) is false)
            Debug.Fail("Could not find a DrawOperationManager for VeldridGraphicsContext");

        await drawOperationManager.AddDrawOperation(Circle);
        await drawOperationManager.AddDrawOperation(Square);
        await drawOperationManager.AddDrawOperation(Elipse);
        await drawOperationManager.AddDrawOperation(Polygon);

        await drawOperationManager.AddDrawOperation(TexturedCircle);
        await drawOperationManager.AddDrawOperation(TexturedSquare);
        await drawOperationManager.AddDrawOperation(TexturedElipse);
        await drawOperationManager.AddDrawOperation(TexturedPolygon);

        var vgc = (VeldridGraphicsManager)Game.MainGraphicsManager;
        var res = vgc.Resources;

        var shaders = Shape2DRenderer.GetDefaultShaders(res);

        if (res.ContainsPipeline<Shape2DRenderer<VertexColor2D>>(1) is false)
            res.RegisterPipeline<Shape2DRenderer<VertexColor2D>>(res.ResourceFactory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                blendState: BlendStateDescription.SingleAlphaBlend,
                depthStencilStateDescription: new DepthStencilStateDescription(
                    depthTestEnabled: true,
                    depthWriteEnabled: true,
                    comparisonKind: ComparisonKind.LessEqual
                ),
                rasterizerState: new RasterizerStateDescription
                (
                    cullMode: FaceCullMode.Back,
                    fillMode: PolygonFillMode.Wireframe,
                    frontFace: FrontFace.Clockwise,
                    depthClipEnabled: true,
                    scissorTestEnabled: false
                ),
                primitiveTopology: PrimitiveTopology.TriangleList,
                shaderSet: new ShaderSetDescription(
                    new VertexLayoutDescription[]
                    {
                        VertexColor2D.GetDescription(),
                    },
                    shaders
                ),
                resourceLayouts: new ResourceLayout[]
                {
                    res.FrameReportLayout,
                    res.GetResourceLayout<VeldridRenderTarget>(),
                    res.GetResourceLayout<VeldridDrawOperation>()
                },
                outputs: res.GraphicsDevice.SwapchainFramebuffer.OutputDescription,
                resourceBindingModel: ResourceBindingModel.Improved
            )), out _, 1);

        await Circle.WaitUntilReady();
        await Square.WaitUntilReady();
        await Elipse.WaitUntilReady();
        await Polygon.WaitUntilReady();

        await TexturedCircle.WaitUntilReady();
        await TexturedSquare.WaitUntilReady();
        await TexturedElipse.WaitUntilReady();
        await TexturedPolygon.WaitUntilReady();

        TexturedCircle.TransformationState.Transform(new Vector3(-.8f, -.8f, 0));
        TexturedSquare.TransformationState.Transform(new Vector3(0, 0, 0));
        TexturedElipse.TransformationState.Transform(new Vector3(0, 0, 0));
        TexturedPolygon.TransformationState.Transform(new Vector3(0, 0, 0));

        Circle.ColorTransformation = ColorTransformation.CreateTint(RgbaVector.Pink);

        Square.TransformationState.Transform(new Vector3(.4f, .5f, 0));
        Square.ColorTransformation = ColorTransformation.CreateTint(RgbaVector.Blue);

        Elipse.TransformationState.Transform(new Vector3(-.3f, -.5f, 0));
        Elipse.ColorTransformation = ColorTransformation.CreateTint(RgbaVector.Yellow);

        Polygon.TransformationState.Transform(new Vector3(-.7f, -.1f, 0));
        Polygon.ColorTransformation = ColorTransformation.CreateTint(RgbaVector.Red);
    }
}
