using System;
using Serilog;
using VDStudios.MagicEngine.Demo.Common.Services;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Graphics.Veldrid;
using VDStudios.MagicEngine.Input;

namespace VDStudios.MagicEngine.Veldrid.Demo;

public static class InputActions
{
    public static void Check(GraphicsManager manager, InputSnapshot inputSnapshot, TimeSpan timestamp)
    {
        ClearTexturedShape2DRendererGraphicsPipeline(manager, inputSnapshot, timestamp);
        RegenerateShapeVertices(manager, inputSnapshot, timestamp);
    }

    private static void RegenerateShapeVertices(GraphicsManager manager, InputSnapshot snapshot, TimeSpan stamp)
    {
        if (manager is VeldridGraphicsManager vgc &&
            snapshot.KeyEventDictionary.TryGetValue(Input.Scancode.G, out var g) &&
            snapshot.KeyEventDictionary.TryGetValue(Input.Scancode.S, out var t) &&
            snapshot.KeyEventDictionary.TryGetValue(Input.Scancode.LeftCtrl, out var ctrl) &&
            (g.FrameSnap.Elapsed is <= 1 || t.FrameSnap.Elapsed is <= 1 || ctrl.FrameSnap.Elapsed is <= 1))
        {
            DebugActions.RegenerateShapeVertices(vgc);
        }
    }

    private static void ClearTexturedShape2DRendererGraphicsPipeline(GraphicsManager manager, InputSnapshot inputSnapshot, TimeSpan timestamp)
    {
        if (manager is VeldridGraphicsManager vgc &&
            inputSnapshot.KeyEventDictionary.TryGetValue(Input.Scancode.G, out var g) &&
            inputSnapshot.KeyEventDictionary.TryGetValue(Input.Scancode.T, out var t) &&
            inputSnapshot.KeyEventDictionary.TryGetValue(Input.Scancode.LeftCtrl, out var ctrl) &&
            (g.FrameSnap.Elapsed is <= 1 || t.FrameSnap.Elapsed is <= 1 || ctrl.FrameSnap.Elapsed is <= 1)) 
        {
            DebugActions.ClearTexturedShape2DRendererGraphicsPipeline(vgc);
        }
    }
}
