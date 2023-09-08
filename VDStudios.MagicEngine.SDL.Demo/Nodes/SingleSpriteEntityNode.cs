using System.Numerics;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Graphics.SDL;
using VDStudios.MagicEngine.Graphics.SDL.DrawOperations;
using VDStudios.MagicEngine.SDL.Demo.Scenes;
using VDStudios.MagicEngine.SDL.Demo.Utilities;
using VDStudios.MagicEngine.World2D;

namespace VDStudios.MagicEngine.SDL.Demo.Nodes;

public class SingleSpriteEntityNode : EntityNode, IWorldObject2D
{
    protected CharacterAnimationContainer? AnimationContainer { get; }
    public TextureOperation Sprite { get; }

    public SingleSpriteEntityNode(TextureOperation sprite, CharacterAnimationContainer? animationContainer = null)
        : base(sprite?.Game ?? throw new ArgumentNullException(nameof(sprite)))
    {
        Sprite = sprite;
        AnimationContainer = animationContainer;
        EntityDrawOperations.Add(Sprite);
    }

    protected override ValueTask EntityUpdating(TimeSpan delta)
    {
        Sprite.TransformationState.Transform(translation: new Vector3(Position, 0));

        if (AnimationContainer is not null && (AnimationContainer.CurrentAnimation.Update()
            || AnimationContainer.SwitchTo(Helper.TryGetFromDirection(Direction, out var dir) ? dir : CharacterAnimationKind.Idle)))
            Sprite.View = AnimationContainer.CurrentAnimation.CurrentElement;

        return ValueTask.CompletedTask;
    }

    protected override async ValueTask Attaching(Scene scene)
    {
        await base.Attaching(scene);
        if (scene.GetDrawOperationManager<SDLGraphicsContext>(out var dopm))
            await dopm.AddDrawOperation(Sprite, RenderTargetList.Objects);
    }

    public void EnableDebugOutlinesDefaultColor()
    {
        Sprite.TextureEdgeOutline = new(RgbaVector.Blue.ToRGBAColor(), 2, true);
        Sprite.TextureCenterOutline = new(RgbaVector.Orange.ToRGBAColor(), 2, true);
        Sprite.TextureTopLeftOutline = new(RgbaVector.DarkRed.ToRGBAColor(), 2, true);
        Sprite.TextureTopRightOutline = new(RgbaVector.Grey.ToRGBAColor(), 2, true);
        Sprite.TextureBottomRightOutline = new(RgbaVector.Pink.ToRGBAColor(), 2, true);
        Sprite.TextureBottomLeftOutline = new(RgbaVector.Green.ToRGBAColor(), 2, true);
    }

    public void DisableDebugOutlinesDefaultColor()
    {
        Sprite.TextureEdgeOutline = null;
        Sprite.TextureCenterOutline = null;
        Sprite.TextureTopLeftOutline = null;
        Sprite.TextureTopRightOutline = null;
        Sprite.TextureBottomRightOutline = null;
        Sprite.TextureBottomLeftOutline = null;
    }
}
