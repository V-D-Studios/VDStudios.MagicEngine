﻿using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using SDL2.NET.Input;
using SDL2.NET.SDLImage;
using VDStudios.MagicEngine.DemoResources;
using VDStudios.MagicEngine.Graphics.SDL;
using VDStudios.MagicEngine.Graphics.SDL.DrawOperations;
using VDStudios.MagicEngine.Input;
using VDStudios.MagicEngine.SDL.Demo.Scenes;
using VDStudios.MagicEngine.SDL.Demo.Services;
using VDStudios.MagicEngine.SDL.Demo.Utilities;
using VDStudios.MagicEngine.World2D;
using Scancode = SDL2.NET.Scancode;

namespace VDStudios.MagicEngine.SDL.Demo.Nodes;

public class MobileSingleSpriteEntityNode : SingleSpriteEntityNode, IWorldMobile2D
{
    public float Speed { get; } = .3f;

    public MobileSingleSpriteEntityNode(TextureOperation textureOperation, CharacterAnimationContainer? animationContainer) :
        base(textureOperation, animationContainer)
    { }

    protected override async ValueTask<bool> Updating(TimeSpan delta)
    {
        Position += Direction * Speed;
        await base.Updating(delta);
        Direction = default;

        return true;
    }

    protected override async ValueTask Attaching(Scene scene)
    {
        await base.Attaching(scene);
    }
}