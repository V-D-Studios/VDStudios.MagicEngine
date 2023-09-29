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
            inputSnapshot.KeyEventDictionary.TryGetValue(Input.Scancode.G, out var g) &&
            inputSnapshot.KeyEventDictionary.TryGetValue(Input.Scancode.T, out var t) &&
            inputSnapshot.KeyEventDictionary.TryGetValue(Input.Scancode.LeftCtrl, out var ctrl) &&
            (g.FrameSnap.Elapsed is <= 1 || t.FrameSnap.Elapsed is <= 1 || ctrl.FrameSnap.Elapsed is <= 1)) 
        {
            var resources = vgc.Resources;
            resources.RemovePipeline<TexturedShape2DRenderer<Vertex2D, TextureCoordinate2D>>(out _);
            resources.ShaderCache.RemoveResource<TexturedShape2DRenderer<Vertex2D, TextureCoordinate2D>>(out _);
            Log.Information("Cleared Default Pipeline and ShaderSet for TexturedShape2DRenderer<Vertex2D, TextureCoordinate2D>");
        }
    }
}
