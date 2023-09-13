using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using SDL2.NET;
using SDL2.NET.SDLImage;
using SDL2.NET.Utilities;
using VDStudios.MagicEngine.DemoResources;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Graphics.Veldrid;
using VDStudios.MagicEngine.SDL.Base;
using VDStudios.MagicEngine.Services;
using VDStudios.MagicEngine.Veldrid.Demo.Scenes;

namespace VDStudios.MagicEngine.Veldrid.Demo;

public class DemoGame : SDLGame
{
    public DemoGame() 
        : base(
            game => new VeldridGraphicsManager(game), 
            game => new GameLifeTimeOnWindowCloses(((VeldridGraphicsManager)game.MainGraphicsManager).Window)
        )
    {
    }

    //protected override void RegisteringServices(IServiceRegistrar registrar)
    //{
    //    base.RegisteringServices(registrar);
    //    registrar.RegisterService((t, c) => new GameState(c), ServiceLifetime.Singleton);
    //    registrar.RegisterService(new GameSettings());
    //}

    private static Task Main(string[] args)
    {
        var game = new DemoGame();
        return game.StartGame(g => new TestScene(g));
    }
}
