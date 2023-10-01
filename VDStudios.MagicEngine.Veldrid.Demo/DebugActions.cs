using System.Reflection;
using VDStudios.MagicEngine.Graphics.Veldrid;
using VDStudios.MagicEngine.Graphics.Veldrid.DrawOperations;
using VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes;
using VDStudios.MagicEngine.Veldrid.Demo.Services;

namespace VDStudios.MagicEngine.Veldrid.Demo;

public static class DebugActions
{
    public static void RegenerateShapeVertices(VeldridGraphicsManager manager)
    {
        var vgs = manager.Game.GameServices.GetService<VeldridGameState>();
        foreach (var x in vgs.Shapes)
            x.RegenVertices();

        foreach (var x in vgs.DrawOperations)
        {
            if (x.GetType().GetMethod("NotifyPendingVertexRegeneration") is MethodInfo mthd)
                mthd.Invoke(x, null);
        }

        manager.Game.GetLogger("Debug", "Input", typeof(InputActions)).Information("Regenerated vertices of all registered shapes");
    }

    public static void ClearTexturedShape2DRendererGraphicsPipeline(VeldridGraphicsManager manager)
    {
        var resources = manager.Resources;
        resources.RemovePipeline<TexturedShape2DRenderer<Vertex2D, TextureCoordinate2D>>(out _);
        resources.ShaderCache.RemoveResource<TexturedShape2DRenderer<Vertex2D, TextureCoordinate2D>>(out _);
        manager.Game.GetLogger("Debug", "Input", typeof(InputActions)).Information("Cleared Default Pipeline and ShaderSet for TexturedShape2DRenderer<Vertex2D, TextureCoordinate2D>");
    }
}
