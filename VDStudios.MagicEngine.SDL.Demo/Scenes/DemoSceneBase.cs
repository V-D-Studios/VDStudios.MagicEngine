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
        registrar.RegisterService(new InputManagerService(Game, out inputReactor));
    }

    protected override ValueTask Beginning()
    {
        var manager = (SDLGraphicsManager)Game.MainGraphicsManager;
        manager.GetOrCreateRenderTargetList(0).Add(_cam = new SDLCamera2D(manager));
        manager.GetOrCreateRenderTargetList(1).Add(new PassthroughRenderTarget(manager));
        Debug.Assert(inputReactor is not null, "inputReactor is unexpectedly null");
        return Attach(inputReactor);
    }

    //List<Task> task = new();
    //SDLFrameHook? hook;
    //protected override async ValueTask<bool> Updating(TimeSpan delta)
    //{
    //    if()

    //    return true;
    //}

    /// <inheritdoc/>
    protected DemoSceneBase(Game game) : base(game)
    {
    }
}
