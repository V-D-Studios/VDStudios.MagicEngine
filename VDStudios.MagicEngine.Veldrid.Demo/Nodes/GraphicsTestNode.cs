﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using SDL2.NET;
using VDStudios.MagicEngine.Demo.Common.Services;
using VDStudios.MagicEngine.Extensions.ImGuiExtension;
using VDStudios.MagicEngine.Extensions.ImGuiExtension.Elements;
using VDStudios.MagicEngine.Geometry;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Graphics.Veldrid;
using VDStudios.MagicEngine.Graphics.Veldrid.DrawOperations;
using VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes;
using VDStudios.MagicEngine.Timing;
using VDStudios.MagicEngine.Veldrid.Demo.ImGuiElements;
using VDStudios.MagicEngine.Veldrid.Demo.Services;
using Veldrid;
using static System.Formats.Asn1.AsnWriter;

namespace VDStudios.MagicEngine.Veldrid.Demo.Nodes;

public class GraphicsTestNode : Node
{
    public readonly ShapeDefinition2D CircleShape;
    public readonly ShapeDefinition2D SquareShape;
    public readonly ShapeDefinition2D ElipseShape;
    public readonly ShapeDefinition2D VerticalElipseShape;
    public readonly ShapeDefinition2D PartialElipseShape;
    public readonly ShapeDefinition2D PolygonShape;

    public readonly Shape2DRenderer PartialElipse;
    public readonly Shape2DRenderer Circle;
    public readonly Shape2DRenderer Square;
    public readonly Shape2DRenderer Elipse;
    public readonly Shape2DRenderer VerticalElipse;
    public readonly Shape2DRenderer Polygon;

    public readonly TexturedShape2DRenderer TexturedCircle;
    public readonly TexturedShape2DRenderer TexturedSquare;
    public readonly TexturedShape2DRenderer TexturedElipse;
    public readonly TexturedShape2DRenderer TexturedPolygon;

    public GraphicsTestNode(Game game) : base(game)
    {
        PartialElipse = new Shape2DRenderer(PartialElipseShape = new ElipseDefinition(new Vector2(0, 0), .6f, .3f, 100, -float.Tau / 3f), game);
        Circle = new Shape2DRenderer(CircleShape = new CircleDefinition(new Vector2(0, 0), .6f, 100), game);
        Square = new Shape2DRenderer(SquareShape = new RectangleDefinition(new Vector2(0, 0), new Vector2(.7f, .8f)), game);
        Elipse = new Shape2DRenderer(ElipseShape = new ElipseDefinition(new Vector2(0, 0), .6f, .3f, 100), game);
        VerticalElipse = new Shape2DRenderer(VerticalElipseShape = new ElipseDefinition(new Vector2(0, 0), .3f, .6f, 100), game);
        Polygon = new Shape2DRenderer(PolygonShape = new PolygonDefinition(stackalloc Vector2[]
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

        TexturedCircle = new TexturedShape2DRenderer(CircleShape, game,
            textureFactory: textureCache.GetResource("robin").OwnerDelegate,
            samplerFactory: samplerCache.GetResource("default").ResourceDelegate,
            viewFactory: textureCache.GetResource("robin").GetResource("default").ResourceDelegate
        );
        TexturedSquare = new TexturedShape2DRenderer(SquareShape, game,
            textureFactory: textureCache.GetResource("robin").OwnerDelegate,
            samplerFactory: samplerCache.GetResource("default").ResourceDelegate,
            viewFactory: textureCache.GetResource("robin").GetResource("default").ResourceDelegate
        );
        TexturedElipse = new TexturedShape2DRenderer(ElipseShape, game,
            textureFactory: textureCache.GetResource("robin").OwnerDelegate,
            samplerFactory: samplerCache.GetResource("default").ResourceDelegate,
            viewFactory: textureCache.GetResource("robin").GetResource("default").ResourceDelegate
        );
        TexturedPolygon = new TexturedShape2DRenderer(PolygonShape, game,
            textureFactory: textureCache.GetResource("baum").OwnerDelegate,
            samplerFactory: samplerCache.GetResource("default").ResourceDelegate,
            viewFactory: textureCache.GetResource("baum").GetResource("default").ResourceDelegate
        );
    }

    protected override ValueTask<bool> Updating(TimeSpan delta)
    {
        //TexturedSquare.TransformationState.Transform(new Vector3(.4f, -.4f, 1));
        Elipse.TransformationState.Transform(new Vector3(-.3f, -.5f, 0));
        //Elipse.TransformationState.Transform(new Vector3(0, 0, 0));
        //TexturedElipse.TransformationState.Transform(new Vector3(0, 0, 0));
        Polygon.TransformationState.Transform(new Vector3(-.7f, -.1f, 0));
        //TexturedCircle.TransformationState.Transform(new Vector3(0, 0, 0));

        TexturedSquare.TransformationState.Transform(new(.8f, .6f, 0.1f));
        TexturedPolygon.TransformationState.Transform(new Vector3(-.7f, 0f, 1f));
        PartialElipse.TransformationState.Transform(new Vector3(-.7f, .4f, 0));

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

        await drawOperationManager.AddDrawOperation(VerticalElipse);
        await drawOperationManager.AddDrawOperation(PartialElipse);

        var vgc = (VeldridGraphicsManager)Game.MainGraphicsManager;
        var res = vgc.Resources;

        var shape2Dshaders = Shape2DRenderer.GetDefaultShaders(res);
        var texturedShape2Dshaders = TexturedShape2DRenderer.GetDefaultShaders(res);

        await Circle.WaitUntilReady();
        await Square.WaitUntilReady();
        await Elipse.WaitUntilReady();
        await Polygon.WaitUntilReady();
        await VerticalElipse.WaitUntilReady();
        await PartialElipse.WaitUntilReady();

        await TexturedCircle.WaitUntilReady();
        await TexturedSquare.WaitUntilReady();
        await TexturedElipse.WaitUntilReady();
        await TexturedPolygon.WaitUntilReady();

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
                    shape2Dshaders
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

        if (res.ContainsPipeline<TexturedShape2DRenderer<Vertex2D, TextureCoordinate2D>>(1) is false)
            res.RegisterPipeline<TexturedShape2DRenderer<Vertex2D, TextureCoordinate2D>>(res.ResourceFactory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
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
                        TextureCoordinate2D.GetDescription(),
                    },
                    texturedShape2Dshaders
                ),
                resourceLayouts: new ResourceLayout[]
                {
                    res.FrameReportLayout,
                    res.GetResourceLayout<VeldridRenderTarget>(),
                    res.GetResourceLayout<VeldridDrawOperation>(),
                    res.GetResourceLayout(typeof(TexturedShape2DRenderer<,>))
                },
                outputs: res.GraphicsDevice.SwapchainFramebuffer.OutputDescription,
                resourceBindingModel: ResourceBindingModel.Improved
            )), out _, 1);

        TexturedCircle.TransformationState.Transform(new Vector3(-.8f, -.8f, 0));
        TexturedSquare.TransformationState.Transform(new Vector3(0, 0, 0));
        TexturedElipse.TransformationState.Transform(new Vector3(0, 0, 0));
        TexturedPolygon.TransformationState.Transform(new Vector3(0, 0, 0));

        Circle.ColorTransformation = ColorTransformation.CreateTint(RgbaVector.Pink);

        Square.ColorTransformation = ColorTransformation.CreateTint(RgbaVector.Blue);

        Elipse.ColorTransformation = ColorTransformation.CreateTint(RgbaVector.Yellow);

        Polygon.ColorTransformation = ColorTransformation.CreateTint(RgbaVector.Red);

        VerticalElipse.ColorTransformation = ColorTransformation.CreateTint(RgbaVector.Green);

        PartialElipse.ColorTransformation = ColorTransformation.CreateTint(RgbaVector.Cyan);

        var vgs = scene.Services.GetService<VeldridGameState>();

        vgs.Shapes.Add(VerticalElipseShape);
        vgs.Shapes.Add(CircleShape);
        vgs.Shapes.Add(ElipseShape);
        vgs.Shapes.Add(SquareShape);
        vgs.Shapes.Add(PartialElipseShape);

        vgs.DrawOperations.Add(Circle);
        vgs.DrawOperations.Add(Square);
        vgs.DrawOperations.Add(Elipse);
        vgs.DrawOperations.Add(VerticalElipse);
        vgs.DrawOperations.Add(PartialElipse);

        vgs.DrawOperations.Add(TexturedCircle);
        vgs.DrawOperations.Add(TexturedSquare);
        vgs.DrawOperations.Add(TexturedElipse);
    }

    protected override ValueTask Detaching()
    {
        Debug.Assert(ParentScene is not null, "ParentScene was unexpectedly null when detaching");

        var vgs = ParentScene.Services.GetService<VeldridGameState>();

        vgs.Shapes.Remove(VerticalElipseShape);
        vgs.Shapes.Remove(CircleShape);
        vgs.Shapes.Remove(ElipseShape);
        vgs.Shapes.Remove(SquareShape);
        vgs.Shapes.Remove(PolygonShape);
        vgs.Shapes.Remove(PartialElipseShape);

        vgs.DrawOperations.Remove(Circle);
        vgs.DrawOperations.Remove(Square);
        vgs.DrawOperations.Remove(Elipse);
        vgs.DrawOperations.Remove(Polygon);
        vgs.DrawOperations.Remove(VerticalElipse);
        vgs.DrawOperations.Remove(PartialElipse);

        vgs.DrawOperations.Remove(TexturedCircle);
        vgs.DrawOperations.Remove(TexturedSquare);
        vgs.DrawOperations.Remove(TexturedElipse);
        vgs.DrawOperations.Remove(TexturedPolygon);

        return base.Detaching();
    }
}
