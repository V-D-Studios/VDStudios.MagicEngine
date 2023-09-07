using System.Diagnostics;
using System.Numerics;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Graphics.SDL;
using VDStudios.MagicEngine.SDL.Demo.Services;
using VDStudios.MagicEngine.World2D;

namespace VDStudios.MagicEngine.SDL.Demo.Nodes;

public abstract class EntityNode : Node, IWorldObject2D
{
    public EntityNode(Game game) : base(game) { }

    private bool edoDisabled;
    protected readonly List<DrawOperation<SDLGraphicsContext>> EntityDrawOperations = new();

    public Vector2 Position { get; set; } = default;
    public Vector2 Size { get; set; } = new(32, 32);
    public Vector2 Direction { get; set; }

    /// <summary>
    /// Performs an update sequence specific for this <see cref="EntityNode"/>
    /// </summary>
    /// <remarks>
    /// <see cref="EntityNode"/> performs important work on <see cref="Node.Updating(TimeSpan)"/> and, since it's a returning method, its preferable that most work is conducted here
    /// </remarks>
    /// <returns></returns>
    protected virtual ValueTask EntityUpdating(TimeSpan delta)
        => ValueTask.CompletedTask;

    protected override async ValueTask<bool> Updating(TimeSpan delta)
    {
        Debug.Assert(ParentScene is not null);
        var settings = ParentScene.Services.GetService<GameSettings>();
        var state = ParentScene.Services.GetService<GameState>();

        if (state.PlayerNode is IWorldMobile2D player && settings.EntityUpdateCutoffDistance > this.GetDistance(player))
        {
            await EntityUpdating(delta);
            
            if (edoDisabled is true)
            {
                for (int i = 0; i < EntityDrawOperations.Count; i++)
                    EntityDrawOperations[i].IsActive = true;
                edoDisabled = false;
            }

            return false;
        }

        if (edoDisabled is false)
        {
            for (int i = 0; i < EntityDrawOperations.Count; i++)
                EntityDrawOperations[i].IsActive = false;
            edoDisabled = true;
        }

        return true;
    }
}
