using System.Diagnostics.CodeAnalysis;
using SDL2.NET;
using VDStudios.MagicEngine.SDL.Demo.Nodes;
using VDStudios.MagicEngine.SDL.Demo.Services;
using VDStudios.MagicEngine.Services;
using VDStudios.MagicEngine.World2D;

namespace VDStudios.MagicEngine.SDL.Demo;

public class GameState : GameObject
{
    public GameState(ServiceCollection services) : base(services.Game, "Global", "State") 
    {
        var inman = services.GetService<InputManagerService>();

        inman.AddKeyBinding(Scancode.W, s =>
        {
            if (PlayerNode is not null)
                PlayerNode.Direction += Directions.Up;
            return ValueTask.CompletedTask;
        });

        inman.AddKeyBinding(Scancode.A, s =>
        {
            if (PlayerNode is not null)
                PlayerNode.Direction += Directions.Left;
            return ValueTask.CompletedTask;
        });

        inman.AddKeyBinding(Scancode.S, s =>
        {
            if (PlayerNode is not null)
                PlayerNode.Direction += Directions.Down;
            return ValueTask.CompletedTask;
        });

        inman.AddKeyBinding(Scancode.D, s =>
        {
            if (PlayerNode is not null)
                PlayerNode.Direction += Directions.Right;
            return ValueTask.CompletedTask;
        });

        inman.AddKeyBinding(Scancode.F9, s =>
        {
            IsRecording = !IsRecording;
            return ValueTask.CompletedTask;
        });
    }
    
    public bool IsRecording { get; private set; }

    public IWorldMobile2D? PlayerNode { get; set; }
}
