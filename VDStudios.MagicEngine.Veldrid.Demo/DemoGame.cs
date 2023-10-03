using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using SDL2.NET;
using SDL2.NET.SDLImage;
using SDL2.NET.Utilities;
using Serilog;
using VDStudios.MagicEngine.Demo.Common.Services;
using VDStudios.MagicEngine.DemoResources;
using VDStudios.MagicEngine.Extensions.ImGuiExtension.Elements;
using VDStudios.MagicEngine.Graphics.Veldrid;
using VDStudios.MagicEngine.SDL.Base;
using VDStudios.MagicEngine.Services;
using VDStudios.MagicEngine.Veldrid.Demo.ImGuiElements;
using VDStudios.MagicEngine.Veldrid.Demo.Scenes;
using VDStudios.MagicEngine.Veldrid.Demo.Services;

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

    protected override LoggerConfiguration ConfigureInternalLogger(LoggerConfiguration config)
    {
        return base.ConfigureInternalLogger(config).MinimumLevel.Verbose();
    }

    protected override LoggerConfiguration ConfigureLogger(LoggerConfiguration config)
    {
        return base.ConfigureInternalLogger(config).MinimumLevel.Verbose();
    }

    protected override void RegisteringServices(IServiceRegistrar registrar)
    {
        base.RegisteringServices(registrar);
        registrar.RegisterService((g, s) => new VeldridGameState(s), ServiceLifetime.Singleton);
    }

    protected override void Start(Scene firstScene)
    {
        MainGraphicsManager.InputReady += InputActions.Check;
        var vgc = (VeldridGraphicsManager)MainGraphicsManager;
        vgc.ImGUIElements.Add(new FPSWatch(this));
        vgc.ImGUIElements.Add(new DebugActionsButtonPane(this));
        vgc.ImGUIElements.Add(new PipelineSwitchPane(this));
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
        var x = game.StartGame(g => new TestScene(g));
        Serilog.Log.Logger = game.GetLogger("Program", "Gameless Log", typeof(DemoGame), "Global");
        return x;
    }
}
