using SDL2.NET;
using VDStudios.MagicEngine;
using VDStudios.MagicEngine.Demo.Scenes;

await Game.Instance.StartGame<DemoScene>();

#if !DEBUG
Console.WriteLine("\nPress any key to continue...");
Console.ReadKey();
#endif