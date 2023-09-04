using System.Numerics;
using VDStudios.MagicEngine.World2D;

namespace VDStudios.MagicEngine.SDL.Demo.Nodes;

public abstract class EntityNode : Node, IWorldMobile2D
{
    public EntityNode(Game game) : base(game) { }

    public float Speed { get; } = .3f;
    public Vector2 Direction { get; protected set; } = Vector2.Zero;
    public Vector2 Position { get; protected set; } = default;
    public Vector2 Size { get; protected set; } = new(32, 32);
}
