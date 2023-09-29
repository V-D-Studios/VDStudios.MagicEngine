using Serilog;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Graphics.Veldrid;
using VDStudios.MagicEngine.Graphics.Veldrid.DrawOperations;
using VDStudios.MagicEngine.Graphics.Veldrid.GPUTypes;
using VDStudios.MagicEngine.Input;

namespace VDStudios.MagicEngine.Veldrid.Demo;

public static class InputActions
{
    public static void Check(GraphicsManager manager, InputSnapshot inputSnapshot, TimeSpan timestamp)
    {
        ClearTexturedShape2DRendererGraphicsPipeline(manager, inputSnapshot, timestamp);
    }

    private static void ClearTexturedShape2DRendererGraphicsPipeline(GraphicsManager manager, InputSnapshot inputSnapshot, TimeSpan timestamp)
    {
        if (manager is VeldridGraphicsManager vgc &&
            (inputSnapshot.ActiveModifiers & Input.KeyModifier.Ctrl) > 0 &&
            inputSnapshot.KeyEventDictionary.TryGetValue(Input.Scancode.G, out var g) && inputSnapshot.KeyEventDictionary.TryGetValue(Input.Scancode.T, out var t) &&
            (g.Repeat is false || t.Repeat is false))
        {
            var resources = vgc.Resources;
            resources.RemovePipeline<TexturedShape2DRenderer<Vertex2D, TextureCoordinate2D>>(out _);
            resources.ShaderCache.RemoveResource<TexturedShape2DRenderer<Vertex2D, TextureCoordinate2D>>(out _);
            Log.Information("Cleared Default Pipeline and ShaderSet for TexturedShape2DRenderer<Vertex2D, TextureCoordinate2D>");
        }
    }
}
