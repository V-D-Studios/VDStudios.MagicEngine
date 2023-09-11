using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using SDL2.NET.Input;
using Scancode = SDL2.NET.Scancode;

namespace VDStudios.MagicEngine.Demo.Common.Services;

public class InputManagerService
{
    public delegate ValueTask KeyBindingAction(Scancode scancode, int repeat);

    private readonly ConcurrentDictionary<Scancode, KeyBindingAction> keyBindings = new();
    private readonly Dictionary<Scancode, int> keyRepeats = new();

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
            Debug.Assert(ParentScene is not null);

            if (Manager.keySemaphore.Wait(50) is false)
                await Manager.keySemaphore.WaitAsync();
            try
            {
                foreach (var (k, a) in Manager.keyBindings)
                {
                    var ks = Keyboard.KeyStates[k];
                    if (ks.IsPressed)
                    {
                        if (Manager.keyRepeats.TryGetValue(k, out int reps) is false)
                            reps = 0;
                        await a(k, reps);
                        Manager.keyRepeats[k] = reps + 1;
                    }
                    else
                        Manager.keyRepeats[k] = 0;
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
