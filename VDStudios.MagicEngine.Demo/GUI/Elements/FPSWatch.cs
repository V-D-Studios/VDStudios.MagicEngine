using ImGuiNET;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.Demo.GUI.Elements;

public sealed class FPSWatch : GUIElement
{
    private static readonly ConcurrentDictionary<long, string> strings;

    static FPSWatch()
    {
        strings = new(5, 100);
        strings[1] = "1 frame per second";
    }

    protected override void SubmitUI(TimeSpan delta, IReadOnlyCollection<GUIElement> subElements)
    {
        var fps = (long)Manager!.FramesPerSecond;
        ImGui.Begin("Frames Per Second");
        ImGui.Text(strings.GetOrAdd(fps, GenStr));
        ImGui.End();
    }

    private string GenStr(long fps) => $"{fps} frames per second";
}