using SDL2.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using VDStudios.MagicEngine.Demo.Components;

namespace VDStudios.MagicEngine.Demo.Nodes;
public class CharacterNode : Node
{
    /// <summary>
    /// The Name of the player
    /// </summary>
    public string? PlayerName { get; set; }

    public KeyboardInputComponent<Vector2> KeyboardInputComponent { get; }

    public CharacterNode()
    {
        KeyboardInputComponent = Install<KeyboardInputComponent<Vector2>>()
            .MapKey(Scancode.Up, new Vector2(0, 1))
            .AddKeySynonym(Scancode.W, Scancode.Up)

            .MapKey(Scancode.Down, new Vector2(0, -1))
            .AddKeySynonym(Scancode.S, Scancode.Down)

            .MapKey(Scancode.Left, new Vector2(-1, 0))
            .AddKeySynonym(Scancode.A, Scancode.Left)

            .MapKey(Scancode.Right, new Vector2(1, 0))
            .AddKeySynonym(Scancode.D, Scancode.Right);
    }
}
