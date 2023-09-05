using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using SDL2.NET.Input;
using VDStudios.MagicEngine.SDL.Demo.Nodes;
using Scancode = SDL2.NET.Scancode;

namespace VDStudios.MagicEngine.SDL.Demo.Services;

public class InputManagerService
{
    public delegate ValueTask KeyBindingAction(Scancode scancode);

    private readonly ConcurrentDictionary<Scancode, KeyBindingAction> keyBindings = new();
    private readonly SemaphoreSlim keySemaphore = new(1, 1);

    public InputManagerService(Game game, out InputReactorNode reactor) 
    {
        reactor = new InputReactorNode(this, game);
    }

    public bool AddKeyBinding(Scancode scancode, KeyBindingAction binding)
        => keyBindings.TryAdd(scancode, binding);

    public bool RemoveKeyBinding(Scancode scancode)
        => keyBindings.TryRemove(scancode, out _);

    public class InputReactorNode : Node
    {
        private readonly InputManagerService Manager;

        internal InputReactorNode(InputManagerService manager, Game game) : base(game)
        {
            Manager = manager;
        }

        protected override async ValueTask<bool> Updating(TimeSpan delta)
        {
            if (Manager.keySemaphore.Wait(50) is false)
                await Manager.keySemaphore.WaitAsync();
            try
            {
                foreach (var (k, a) in Manager.keyBindings)
                {
                    var ks = Keyboard.KeyStates[k];
                    if (ks.IsPressed)
                        await a(k);
                }

                return true;
            }
            finally
            {
                Manager.keySemaphore.Release();
            }
        }
    }
}
