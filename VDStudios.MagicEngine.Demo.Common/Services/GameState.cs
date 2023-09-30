using System.Diagnostics.CodeAnalysis;
using SDL2.NET;
using VDStudios.MagicEngine.Geometry;
using VDStudios.MagicEngine.Services;
using VDStudios.MagicEngine.World2D;

namespace VDStudios.MagicEngine.Demo.Common.Services;

public class GameState : GameObject
{
    public GameState(ServiceCollection services) : base(services.Game, "Global", "State")
    {
        var inman = services.GetService<InputManagerService>();

        inman.AddKeyBinding(Scancode.W, (s, r) =>
        {
            if (PlayerNode is not null)
                PlayerNode.Direction += Directions.Up;
            return ValueTask.CompletedTask;
        });

        inman.AddKeyBinding(Scancode.A, (s, r) =>
        {
            if (PlayerNode is not null)
                PlayerNode.Direction += Directions.Left;
            return ValueTask.CompletedTask;
        });

        inman.AddKeyBinding(Scancode.S, (s, r) =>
        {
            if (PlayerNode is not null)
                PlayerNode.Direction += Directions.Down;
            return ValueTask.CompletedTask;
        });

        inman.AddKeyBinding(Scancode.D, (s, r) =>
        {
            if (PlayerNode is not null)
                PlayerNode.Direction += Directions.Right;
            return ValueTask.CompletedTask;
        });

        inman.AddKeyBinding(Scancode.F12, async (s, r) =>
        {
            if (r > 0) return;

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

        //inman.AddKeyBinding(Scancode.F9, (s, r) =>
        //{
        //    if (r > 0)
        //        return ValueTask.CompletedTask;

        //    lock (Sync)
        //    {
        //        if (recorder is null)
        //        {
        //            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MagicEngine Recordings");
        //            Directory.CreateDirectory(dir);

        //            recorder = new SDLRecorder(
        //                    (SDLGraphicsManager)Game.MainGraphicsManager,
        //                    File.OpenWrite(Path.Combine(dir, $"{DateTime.Now:yyyy-MM-dd hh_mm_ss_ffff}.avi")),
        //                    true
        //                );
        //            recorder.Start();
        //        }
        //        else
        //        {
        //            recorder.Dispose();
        //            recorder = null;
        //        }
        //    }
        //    return ValueTask.CompletedTask;
        //});
    }

    public IWorldMobile2D? PlayerNode { get; set; }
}
