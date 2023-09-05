using System.Numerics;
using VDStudios.MagicEngine.World2D;

namespace VDStudios.MagicEngine.SDL.Demo.Nodes;

public abstract class EntityNode : Node, IWorldObject2D
{
    public EntityNode(Game game) : base(game) { }

    public Vector2 Position { get; set; } = default;
    public Vector2 Size { get; set; } = new(32, 32);
    public Vector2 Direction { get; set; }
}
