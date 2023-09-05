using System.Data.SqlTypes;
using System.Diagnostics;
using SDL2.NET;
using VDStudios.MagicEngine.Graphics.SDL;
using VDStudios.MagicEngine.Graphics.SDL.RenderTargets;
using VDStudios.MagicEngine.SDL.Demo.Services;
using VDStudios.MagicEngine.Services;

namespace VDStudios.MagicEngine.SDL.Demo.Scenes;

public abstract class DemoSceneBase : Scene
{
    private SDLCamera2D? _cam;
    public SDLCamera2D Camera => _cam ?? throw new InvalidOperationException("Cannot access this Scene's Camera before the Scene is begun");
    private InputManagerService.InputReactorNode? inputReactor;

    /// <inheritdoc/>
    protected override void RegisteringServices(IServiceRegistrar registrar)
    {
        InputManagerService inman;
        registrar.RegisterService(inman = new InputManagerService(Game, out inputReactor));

        inman.AddKeyBinding(Scancode.F12, async s =>
        {
            var scdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MagicEngine Screenshots");
            Directory.CreateDirectory(scdir);
            if (Game.ActiveGraphicsManagers.Count > 1)
            {
                scdir = Path.Combine(scdir, DateTime.Now.ToString("yyyy-MM-dd hh_mm_ss_ffff"));
                Directory.CreateDirectory(scdir);
                int i = 0;
                foreach (var manager in Game.ActiveGraphicsManagers)
                {
                    using var stream = File.Open(Path.Combine(scdir, $"Screen_{i}.png"), FileMode.Create);
                    await manager.TakeScreenshot(stream, Utility.ScreenshotImageFormat.PNG);
                }
            }
            else
            {
                using var stream = File.Open(Path.Combine(scdir, $"{DateTime.Now:yyyy-MM-dd hh_mm_ss_ffff}.png"), FileMode.Create);
                await Game.MainGraphicsManager.TakeScreenshot(stream, Utility.ScreenshotImageFormat.PNG);
            }
        });
    }

    protected override ValueTask Beginning()
    {
        var manager = (SDLGraphicsManager)Game.MainGraphicsManager;
        manager.GetOrCreateRenderTargetList(0).Add(_cam = new SDLCamera2D(manager));
        manager.GetOrCreateRenderTargetList(1).Add(new PassthroughRenderTarget(manager));
        Debug.Assert(inputReactor is not null, "inputReactor is unexpectedly null");
        return Attach(inputReactor);
    }

    /// <inheritdoc/>
    protected DemoSceneBase(Game game) : base(game)
    {
    }
}
