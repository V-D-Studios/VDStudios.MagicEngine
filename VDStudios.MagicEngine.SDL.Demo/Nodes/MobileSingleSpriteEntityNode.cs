using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using SDL2.NET.Input;
using SDL2.NET.SDLImage;
using VDStudios.MagicEngine.DemoResources;
using VDStudios.MagicEngine.Graphics.SDL;
using VDStudios.MagicEngine.Graphics.SDL.DrawOperations;
using VDStudios.MagicEngine.Input;
using VDStudios.MagicEngine.SDL.Demo.Scenes;
using VDStudios.MagicEngine.Demo.Common.Services;
using VDStudios.MagicEngine.World2D;
using Scancode = SDL2.NET.Scancode;
using VDStudios.MagicEngine.Demo.Common.Utilities;
using SDL2.NET;

namespace VDStudios.MagicEngine.SDL.Demo.Nodes;

public class MobileSingleSpriteEntityNode : SingleSpriteEntityNode, IWorldMobile2D
{
    public float Speed { get; } = .3f;

    public MobileSingleSpriteEntityNode(TextureOperation textureOperation, CharacterAnimationContainer<Rectangle>? animationContainer) :
        base(textureOperation, animationContainer)
    { }

    protected override async ValueTask EntityUpdating(TimeSpan delta)
    {
        Position += Direction * Speed;
        await base.EntityUpdating(delta);
        Direction = default;
    }

    protected override async ValueTask Attaching(Scene scene)
    {
        await base.Attaching(scene);
    }
}
