using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using SDL2.NET.Input;
using VDStudios.MagicEngine.SDL.Demo.Nodes;
using Scancode = SDL2.NET.Scancode;

namespace VDStudios.MagicEngine.SDL.Demo.FunctionalComponents;

public class InputManagerComponent : FunctionalComponent
{
    public delegate ValueTask KeyBindingAction(Scancode scancode);

    private readonly ConcurrentDictionary<Scancode, KeyBindingAction> keyBindings = new();
    private readonly SemaphoreSlim keySemaphore = new(1, 1);

    public InputManagerComponent(InputReactorNode node) : base(node)
    {
        if (inst is not null)
            throw new InvalidOperationException("InputManagerComponent already has an active instance attached to an InputReactorNode");
        inst = this;
    }

    public static InputManagerComponent Instance => inst ?? throw new InvalidOperationException("Cannot access InputManagerComponent.Instance before it's created");
    private static InputManagerComponent? inst;

    protected override bool FilterNode(Node node, [NotNullWhen(false)] out string? reasonForRejection)
    {
        if (node is not InputReactorNode)
        {
            reasonForRejection = "Node must be an InputReactorNode";
            return false;
        }

        reasonForRejection = null;
        return true;
    }

    protected override async ValueTask Update(TimeSpan delta, TimeSpan componentDelta)
    {
        if (keySemaphore.Wait(50) is false)
            await keySemaphore.WaitAsync();
        try
        {
            foreach (var (k, a) in keyBindings)
            {
                var ks = Keyboard.KeyStates[k];
                if (ks.IsPressed)
                    await a(k);
            }
        }
        finally
        {
            keySemaphore.Release();
        }
    }

    public bool AddKeyBinding(Scancode scancode, KeyBindingAction binding)
        => keyBindings.TryAdd(scancode, binding);

    public bool RemoveKeyBinding(Scancode scancode)
        => keyBindings.TryRemove(scancode, out _);
}
