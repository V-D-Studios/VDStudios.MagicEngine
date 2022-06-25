using SDL2.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using VDStudios.MagicEngine.Demo.Services;

namespace VDStudios.MagicEngine.Demo.Nodes;
public class PlayerNode : Node, IDrawableNode
{
    protected class PlayerDrawState : IDrawOperation
    {
        private Rectangle Source;
        private Rectangle Destination;
        private Texture Texture;

        public PlayerDrawState(Texture texture)
        {
            Texture = texture;
        }

        public void SetState(Rectangle source, Rectangle destination)
        {
            Source = source;
            Destination = destination;
        }

        public void SetState(Rectangle source, Rectangle destination, Texture texture)
        {
            Source = source;
            Destination = destination;
            Texture = texture;
        }

        public void Draw(Vector2 offset, Renderer renderer)
        {
            var (w, h, x, y) = Destination;
            Texture.Render(Source, new FRectangle(w, h, offset.X + x, offset.Y + y));
        }
    }

    /// <summary>
    /// The Name of the player
    /// </summary>
    public string? PlayerName { get; set; }

    public KeyboardInputMapper<Vector2> KInMapper { get; }

    protected Vector2 Dir;
    protected Vector2 Position;
    protected Vector2 Speed = new(1000);
    protected Vector2 Move;
    protected Texture AnimSprite;
    protected Dictionary<Vector2, TimedSequence<Rectangle>> Animations;
    protected PlayerDrawState DrawState;

    const float AnimFps = 9;

    public PlayerNode()
    {
        AnimSprite = StaticResources.Graphics.Animations.Robin;
        DrawState = new(AnimSprite);

        Size asz = new(32, 32);
        int size = 32;
        int xoff = size * 4;
        int yoff = 0;

        Animations = new(9)
        {
            {
                Vector2.Zero,
                new(AnimFps, new Rectangle[]
                {
                    new(asz, xoff + size * 0, yoff + size * 0),
                    new(asz, xoff + size * 1, yoff + size * 0),
                    new(asz, xoff + size * 2, yoff + size * 0),
                    new(asz, xoff + size * 3, yoff + size * 0)
                })
            },
            {
                Directions.Up,
                new(AnimFps, new Rectangle[]
                {
                    new(asz, xoff + size * 0, yoff + size * 5),
                    new(asz, xoff + size * 1, yoff + size * 5),
                    new(asz, xoff + size * 2, yoff + size * 5),
                    new(asz, xoff + size * 3, yoff + size * 5)
                })
            },
            {
                Directions.Down,
                new(AnimFps, new Rectangle[]
                {
                    new(asz, xoff + size * 0, yoff + size * 4),
                    new(asz, xoff + size * 1, yoff + size * 4),
                    new(asz, xoff + size * 2, yoff + size * 4),
                    new(asz, xoff + size * 3, yoff + size * 4)
                })
            },
            {
                Directions.Left,
                new(AnimFps, new Rectangle[]
                {
                    new(asz, xoff + size * 0, yoff + size * 2),
                    new(asz, xoff + size * 1, yoff + size * 2),
                    new(asz, xoff + size * 2, yoff + size * 2),
                    new(asz, xoff + size * 3, yoff + size * 2)
                })
            },
            {
                Directions.Right,
                new(AnimFps, new Rectangle[]
                {
                    new(asz, xoff + size * 0, yoff + size * 3),
                    new(asz, xoff + size * 1, yoff + size * 3),
                    new(asz, xoff + size * 2, yoff + size * 3),
                    new(asz, xoff + size * 3, yoff + size * 3)
                })
            },
            {
                Directions.UpRight,
                new(AnimFps, new Rectangle[]
                {
                    new(asz, xoff + size * 0, yoff + size * 9),
                    new(asz, xoff + size * 1, yoff + size * 9),
                    new(asz, xoff + size * 2, yoff + size * 9),
                    new(asz, xoff + size * 3, yoff + size * 9)
                })
            },
            {
                Directions.UpLeft,
                new(AnimFps, new Rectangle[]
                {
                    new(asz, xoff + size * 0, yoff + size * 8),
                    new(asz, xoff + size * 1, yoff + size * 8),
                    new(asz, xoff + size * 2, yoff + size * 8),
                    new(asz, xoff + size * 3, yoff + size * 8)
                })
            },
            {
                Directions.DownLeft,
                new(AnimFps, new Rectangle[]
                {
                    new(asz, xoff + size * 0, yoff + size * 7),
                    new(asz, xoff + size * 1, yoff + size * 7),
                    new(asz, xoff + size * 2, yoff + size * 7),
                    new(asz, xoff + size * 3, yoff + size * 7)
                })
            },
            {
                Directions.DownRight,
                new(AnimFps, new Rectangle[]
                {
                    new(asz, xoff + size * 0, yoff + size * 6),
                    new(asz, xoff + size * 1, yoff + size * 6),
                    new(asz, xoff + size * 2, yoff + size * 6),
                    new(asz, xoff + size * 3, yoff + size * 6)
                })
            }
        };

        KInMapper = new KeyboardInputMapper<Vector2>()
            .MapKey(Scancode.Up, Directions.Up)
            .AddKeySynonym(Scancode.Up, Scancode.W)

            .MapKey(Scancode.Down, Directions.Down)
            .AddKeySynonym(Scancode.Down, Scancode.S)

            .MapKey(Scancode.Left, Directions.Left)
            .AddKeySynonym(Scancode.Left, Scancode.A)

            .MapKey(Scancode.Right, Directions.Right)
            .AddKeySynonym(Scancode.Right, Scancode.D);
    }

    protected override ValueTask<bool> Updating(TimeSpan delta)
    {
        Vector2 prevDir = Dir;
        Vector2 dir;

        Vector2 move = Move;

        while (KInMapper.NextKeyPress(out var kp))
            move += kp.Value;
        while (KInMapper.NextKeyRaise(out var kr))
            move -= kr.Value;

        Move = move = Vector2.Clamp(move, -Vector2.One, Vector2.One);

        Dir = dir = move;
        Position += Speed * dir * (float)delta.TotalSeconds;

        DrawState.SetState(Animations[prevDir].CurrentElement, new(96, 96, (int)Position.X, (int)Position.Y));

        return ValueTask.FromResult(true);
    }

    public UpdateBatch UpdateBatch { get; }

    public ValueTask<bool> AddToDrawQueue(IDrawQueue queue)
    {
        queue.Enqueue(DrawState, -1);
        return ValueTask.FromResult(true);
    }
}
