using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDL2.NET;
using VDStudios.MagicEngine.Graphics.SDL;
using VDStudios.MagicEngine.Graphics.SDL.DrawOperations;
using VDStudios.MagicEngine.SDL.Demo.Scenes;
using VDStudios.MagicEngine.Demo.Common.Services;

namespace VDStudios.MagicEngine.SDL.Demo.Nodes;
public class TerrainNode : Node
{
    public TextureOperation TerrainDrawing { get; }

    public TerrainNode(TextureOperation terrain) : base(terrain.Game)
    {
        TerrainDrawing = terrain ?? throw new ArgumentNullException(nameof(terrain));
    }

    protected override async ValueTask<bool> Updating(TimeSpan delta)
    {
        await base.Updating(delta);

        var p = ParentScene.Services.GetService<GameState>().PlayerNode;
        if (p is null) return false;

        Debug.Assert(TerrainDrawing.Manager is not null, "TerrainDrawing.Manager is unexpectedly null at the time of updating");
        TerrainDrawing.View = new Rectangle(TerrainDrawing.TextureSize, (int)p.Position.X, (int)p.Position.Y);

        return true;
    }

    protected override async ValueTask Attaching(Scene scene)
    {
        if (scene.GetDrawOperationManager<SDLGraphicsContext>(out var dopm))
            await dopm.AddDrawOperation(TerrainDrawing, RenderTargetList.Terrain);
    }
}
