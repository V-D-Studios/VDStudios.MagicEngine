using VDStudios.MagicEngine.Demo.SpaceInvaders.Scenes;
using VDStudios.MagicEngine.Demo.SpaceInvaders.Services;

namespace VDStudios.MagicEngine.Demo.SpaceInvaders;

public static class Program
{
    public static MenuScene MainMenuScene { get; } = new();
    public static CoreGameScene GameScene { get; } = new();
    public static HighScoreScreenScene HighScoreScene { get; } = new();

    private static async Task Main(string[] args)
    {
        var game = Game.NewSDLGame();

        game.GameServices.RegisterService(new HighScoreList(), true);
        
        await game.StartGame(MainMenuScene);
    }
}
