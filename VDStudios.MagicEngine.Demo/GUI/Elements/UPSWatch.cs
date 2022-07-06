using ImGuiNET;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace VDStudios.MagicEngine.Demo.GUI.Elements;

public sealed class UPSWatch : GUIElement
{
    private static readonly ConcurrentDictionary<long, string> strings;

    static UPSWatch()
    {
        strings = new(5, 100);
        strings[1] = "1 tick";
    }

    protected override void SubmitUI(TimeSpan delta, IReadOnlyCollection<GUIElement> subElements)
    {
        var fps = Game.AverageDelta.Ticks;
        ImGui.Begin("Ticks per update");
        ImGui.Text(strings.GetOrAdd(fps, GenStr));
        ImGui.End();
    }

    private string GenStr(long fps) => $"{fps:#.##} ticks";
}
