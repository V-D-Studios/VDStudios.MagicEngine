using VDStudios.MagicEngine;
using VDStudios.MagicEngine.Demo.Scenes;

await Game.NewGame<SDLGame>().StartGame<DemoScene>();

#if !DEBUG
Console.WriteLine("\nPress any key to continue...");
Console.ReadKey();
#endif