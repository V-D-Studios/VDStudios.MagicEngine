using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDStudios.MagicEngine.Demo.Common.Services;
using VDStudios.MagicEngine.Geometry;
using VDStudios.MagicEngine.Graphics.Veldrid.DrawOperations;
using VDStudios.MagicEngine.Services;
using VDStudios.MagicEngine.World2D;

namespace VDStudios.MagicEngine.Veldrid.Demo.Services;

public class VeldridGameState : GameObject
{
    public List<ShapeDefinition2D> Shapes { get; } = new();
    public List<VeldridDrawOperation> DrawOperations { get; } = new();

    public VeldridGameState(ServiceCollection services) : base(services.Game, "Global", "State")
    {
    }

    public IWorldMobile2D? PlayerNode { get; set; }
}
