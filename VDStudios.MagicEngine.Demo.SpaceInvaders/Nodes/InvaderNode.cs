using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using VDStudios.MagicEngine.Animation;
using VDStudios.MagicEngine.Demo.SpaceInvaders.Resources;
using VDStudios.MagicEngine.DrawLibrary;
using VDStudios.MagicEngine.DrawLibrary.Geometry;
using VDStudios.MagicEngine.Geometry;
using Veldrid;

namespace VDStudios.MagicEngine.Demo.SpaceInvaders;

/// <summary>
/// Represents a given invader in the level
/// </summary>
public class InvaderNode : CharacterNode
{
    /// <summary>
    /// Constructs a new instance of an Invader
    /// </summary>
    /// <param name="column"></param>
    /// <param name="row"></param>
    /// <param name="scoreReturn"></param>
    /// <param name="type"></param>
    public InvaderNode(int column, int row, int scoreReturn, InvaderType type)
    {
        Column = column;
        Row = row;
        ScoreReturn = scoreReturn;
        Type = type;

        Matrix4x4 view0;
        Matrix4x4 view1;
        ShapeDefinition2D shape;

        var texture = ResourceCache.EntitySpritesheet.GetResource(Game.MainGraphicsManager);
        switch (type)
        {
            case InvaderType.Squid:
                view0 = Matrix4x4.Identity;//GeometryMath.Create2DView(texture, new(8, 8, 20, 8));
                view1 = Matrix4x4.Identity;//GeometryMath.Create2DView(texture, new(8, 8, 12, 8));
                shape = PolygonDefinition.Rectangle(default, new(8, 8));
                break;
            case InvaderType.Crab:
                view0 = Matrix4x4.Identity;//GeometryMath.Create2DView(texture, new(11, 8, 28, 8));
                view1 = Matrix4x4.Identity;//GeometryMath.Create2DView(texture, new(11, 8, 28, 0));
                shape = PolygonDefinition.Rectangle(default, new(11, 8));
                break;
            case InvaderType.Octopus:
                view0 = Matrix4x4.Identity;//GeometryMath.Create2DView(texture, new(12, 8, 0, 8));
                view1 = Matrix4x4.Identity;//GeometryMath.Create2DView(texture, new(12, 8, 0, 0));
                shape = PolygonDefinition.Rectangle(default, new(12, 8));
                break;
            default:
                throw new ArgumentException($"Invalid InvaderType {type}", nameof(type));
        }

        CharacterSprite = new TexturedShapeRenderer(
            (gd, rf) => texture,
            new ShapeDefinition2D[] { shape },
            new(
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
                    null
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
            ),
            TextureVertexGeneratorFill.Instance
        );

        DrawOperationManager.AddDrawOperation(CharacterSprite);

        ViewAnim = new(
            AnimTimer = new ManualTimeUpdateKeeper(TimeSpan.FromSeconds(.3)),
            TimedSequenceMode.Loop, 
            view0, view1
        );
    }

    public MothershipNode Mothership => mothership ?? throw new InvalidOperationException("This InvaderNode is not attached to a MothershipNode");
    private MothershipNode? mothership;

    public int Column { get; }

    public int Row { get; }

    public int ScoreReturn { get; }

    public InvaderType Type { get; }

    protected readonly TimedSequence<Matrix4x4, TimeSpan> ViewAnim;
    private readonly ManualTimeUpdateKeeper AnimTimer;

#if DEBUG
    internal TimeSpan TimePerFrame;
#endif

    protected override ValueTask<bool> Updating(TimeSpan delta)
    {
#if DEBUG
        AnimTimer.TimePerFrame = TimePerFrame;
#endif
        AnimTimer.Add(delta);
        ViewAnim.Update();
        ((TexturedShapeRenderer)CharacterSprite).TextureView = ViewAnim.Current;
        return base.Updating(delta);
    }

    protected override bool FilterParentNode(Node parent, [NotNullWhen(false)] [MaybeNullWhen(true)] out string? reasonForDenial)
    {
        if (parent is MothershipNode m)
        {
            mothership = m;
            reasonForDenial = null;
            return true;
        }

        reasonForDenial = "InvaderNodes can only be attached to a MothershipNode";
        return false;
    }
}
