using SDL2.NET;
using VDStudios.MagicEngine;
using VDStudios.MagicEngine.Demo.Scenes;

await SDLAppBuilder.CreateInstance<Game>().StartGame<DemoScene>();