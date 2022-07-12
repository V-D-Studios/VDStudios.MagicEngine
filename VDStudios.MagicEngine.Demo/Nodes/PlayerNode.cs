using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Veldrid;
using VDStudios.MagicEngine.Demo.Services;
using SDL2.NET;
using Texture = Veldrid.Texture;
using VDStudios.MagicEngine.Demo.ResourceExtensions;
using SDL2.NET.Input;
using VDStudios.MagicEngine.DrawLibrary;

namespace VDStudios.MagicEngine.Demo.Nodes;
public class PlayerNode : Node, IDrawableNode
{
    #region Node Data

    /// <summary>
    /// The Name of the player
    /// </summary>
    public string? PlayerName { get; set; }

    protected Vector2 Position;
    protected Vector2 Speed = new(1000);
    protected Vector2 Move;
    protected Dictionary<Vector2, TimedSequence<Viewport>> Animations;
    protected TextureDrawing DrawOp;

    const float AnimFps = 9;

    public PlayerNode()
    {
        int size = 32;
        int xoff = size * 4;
        int yoff = 0;

        Animations = new(9)
        {
            {
                Vector2.Zero,
                new(AnimFps, new Viewport[]
                {
                    new(size, size, xoff + size * 0, yoff + size * 0, 0, 0),
                    new(size, size, xoff + size * 1, yoff + size * 0, 0, 0),
                    new(size, size, xoff + size * 2, yoff + size * 0, 0, 0),
                    new(size, size, xoff + size * 3, yoff + size * 0, 0, 0)
                })
            },
            {
                Directions.Up,
                new(AnimFps, new Viewport[]
                {
                    new(size, size, xoff + size * 0, yoff + size * 5, 0, 0),
                    new(size, size, xoff + size * 1, yoff + size * 5, 0, 0),
                    new(size, size, xoff + size * 2, yoff + size * 5, 0, 0),
                    new(size, size, xoff + size * 3, yoff + size * 5, 0, 0)
                })
            },
            {
                Directions.Down,
                new(AnimFps, new Viewport[]
                {
                    new(size, size, xoff + size * 0, yoff + size * 4, 0, 0),
                    new(size, size, xoff + size * 1, yoff + size * 4, 0, 0),
                    new(size, size, xoff + size * 2, yoff + size * 4, 0, 0),
                    new(size, size, xoff + size * 3, yoff + size * 4, 0, 0)
                })
            },
            {
                Directions.Left,
                new(AnimFps, new Viewport[]
                {
                    new(size, size, xoff + size * 0, yoff + size * 2, 0, 0),
                    new(size, size, xoff + size * 1, yoff + size * 2, 0, 0),
                    new(size, size, xoff + size * 2, yoff + size * 2, 0, 0),
                    new(size, size, xoff + size * 3, yoff + size * 2, 0, 0)
                })
            },
            {
                Directions.Right,
                new(AnimFps, new Viewport[]
                {
                    new(size, size, xoff + size * 0, yoff + size * 3, 0, 0),
                    new(size, size, xoff + size * 1, yoff + size * 3, 0, 0),
                    new(size, size, xoff + size * 2, yoff + size * 3, 0, 0),
                    new(size, size, xoff + size * 3, yoff + size * 3, 0, 0)
                })
            },
            {
                Directions.UpRight,
                new(AnimFps, new Viewport[]
                {
                    new(size, size, xoff + size * 0, yoff + size * 9, 0, 0),
                    new(size, size, xoff + size * 1, yoff + size * 9, 0, 0),
                    new(size, size, xoff + size * 2, yoff + size * 9, 0, 0),
                    new(size, size, xoff + size * 3, yoff + size * 9, 0, 0)
                })
            },
            {
                Directions.UpLeft,
                new(AnimFps, new Viewport[]
                {
                    new(size, size, xoff + size * 0, yoff + size * 8, 0, 0),
                    new(size, size, xoff + size * 1, yoff + size * 8, 0, 0),
                    new(size, size, xoff + size * 2, yoff + size * 8, 0, 0),
                    new(size, size, xoff + size * 3, yoff + size * 8, 0, 0)
                })
            },
            {
                Directions.DownLeft,
                new(AnimFps, new Viewport[]
                {
                    new(size, size, xoff + size * 0, yoff + size * 7, 0, 0),
                    new(size, size, xoff + size * 1, yoff + size * 7, 0, 0),
                    new(size, size, xoff + size * 2, yoff + size * 7, 0, 0),
                    new(size, size, xoff + size * 3, yoff + size * 7, 0, 0)
                })
            },
            {
                Directions.DownRight,
                new(AnimFps, new Viewport[]
                {
                    new(size, size, xoff + size * 0, yoff + size * 6, 0, 0),
                    new(size, size, xoff + size * 1, yoff + size * 6, 0, 0),
                    new(size, size, xoff + size * 2, yoff + size * 6, 0, 0),
                    new(size, size, xoff + size * 3, yoff + size * 6, 0, 0)
                })
            }
        };
        DrawOperationManager = new(this);
        DrawOp = DrawOperationManager.AddDrawOperation(new TextureDrawing(ImageTextures.RobinSpriteSheet));
    }

    protected override ValueTask<bool> Updating(TimeSpan delta)
    {
        Vector2 move = default;

        foreach (var key in Keyboard.KeyStates)
            move += key.IsPressed ? key.Scancode switch
            {
                Scancode.W or Scancode.Up => Directions.Up,
                Scancode.A or Scancode.Left => Directions.Left,
                Scancode.S or Scancode.Down => Directions.Down,
                Scancode.D or Scancode.Right => Directions.Right,
                _ => Vector2.Zero
            } : Vector2.Zero;

        Move = move = Vector2.Clamp(move, -Vector2.One, Vector2.One);

        Position += Speed * move * (float)delta.TotalSeconds;

        //DrawState.SetState(Animations[prevDir].CurrentElement, new(96, 96, Position.X, Position.Y, 0, 0));

        return ValueTask.FromResult(true);
    }

    #endregion

    #region IDrawable

    public DrawOperationManager DrawOperationManager { get; }
    public bool SkipDrawPropagation { get; }

    #endregion
}
