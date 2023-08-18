using VDStudios.MagicEngine.Graphics.SDL;
using VDStudios.MagicEngine.Graphics.SDL.RenderTargets;
using VDStudios.MagicEngine.Internal;

namespace VDStudios.MagicEngine.SDL.Demo;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var game = new SDLGame();
        await game.StartGame(g => new TestScene(g));
    }
}

public class TestScene : Scene
{
    public TestScene(Game game) : base(game)
    {
    }

    protected override ValueTask Beginning()
    {
        var manager = ((SDLGraphicsManager)Game.MainGraphicsManager);
        manager.RenderTargets.Add(new SDLCamera2D(manager));
        return ValueTask.CompletedTask;
    }
}