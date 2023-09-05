using System.ComponentModel.DataAnnotations;
using VDStudios.MagicEngine.Graphics.SDL;
using VDStudios.MagicEngine.Internal;
using VDStudios.MagicEngine.SDL.Demo.Scenes;
using VDStudios.MagicEngine.Services;

namespace VDStudios.MagicEngine.SDL.Demo;

public class DemoGame : SDLGame
{
    protected override void RegisteringServices(IServiceRegistrar registrar)
    {
        base.RegisteringServices(registrar);
        registrar.RegisterService((t, c) => new GameState(c), ServiceLifetime.Singleton);
    }

    private static async Task Main(string[] args)
    {
        var game = new DemoGame();
        await game.StartGame(g => new TestScene(g));
    }
}

