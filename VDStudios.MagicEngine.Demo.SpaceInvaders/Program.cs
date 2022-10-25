namespace VDStudios.MagicEngine.Demo.SpaceInvaders;

public static class Program
{
    private static async Task Main(string[] args)
    {
        await Game.NewGame<SDLGame>().StartGame<MenuScene>();
    }
}

public class MenuScene : Scene
{
}