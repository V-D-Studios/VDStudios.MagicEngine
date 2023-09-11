using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using SDL2.NET;
using SDL2.NET.SDLImage;
using SDL2.NET.Utilities;
using VDStudios.MagicEngine.DemoResources;
using VDStudios.MagicEngine.Graphics.SDL;
using VDStudios.MagicEngine.Internal;
using VDStudios.MagicEngine.SDL.Base;
using VDStudios.MagicEngine.SDL.Demo.Scenes;
using VDStudios.MagicEngine.SDL.Demo.Services;
using VDStudios.MagicEngine.Services;

namespace VDStudios.MagicEngine.SDL.Demo;

public class DemoGame : SDLGame
{
    public DemoGame() : base(
        game => new SDLGraphicsManager(game),
        game => new GameLifeTimeOnWindowCloses(((SDLGraphicsManager)game.MainGraphicsManager).Window)
    )
    { }

    protected override void RegisteringServices(IServiceRegistrar registrar)
    {
        base.RegisteringServices(registrar);
        registrar.RegisterService((t, c) => new GameState(c), ServiceLifetime.Singleton);
        registrar.RegisterService(new GameSettings());
        registrar.RegisterService((t, c) =>
        {
            var cache = new ResourceCache<SDLGraphicsContext, Texture>();

            cache.RegisterResource("baum", baum, out _);

            cache.RegisterResource("robin", robin, out _);

            cache.RegisterResource("grass1", grass1, out _);

            unsafe Texture robin(SDLGraphicsContext c)
            {
                fixed (byte* ptr = Animations.Robin)
                {
                    using var rwop = RWops.CreateFromPointer(ptr, Animations.Robin.Length);
                    return Image.LoadTexture(c.Renderer, rwop);
                }
            }

            unsafe Texture baum(SDLGraphicsContext c)
            {
                fixed (byte* ptr = Animations.Baum)
                {
                    using var rwop = RWops.CreateFromPointer(ptr, Animations.Baum.Length);
                    return Image.LoadTexture(c.Renderer, rwop);
                }
            }

            unsafe Texture grass1(SDLGraphicsContext c)
            {
                fixed (byte* ptr = Animations.Grass1)
                {
                    using var rwop = RWops.CreateFromPointer(ptr, Animations.Grass1.Length);
                    return Image.LoadTexture(c.Renderer, rwop);
                }
            }

            return cache;
        }, ServiceLifetime.Singleton);
    }

    private static Task Main(string[] args)
    {
        var game = new DemoGame();
        return game.StartGame(g => new TestScene(g));
    }
}
