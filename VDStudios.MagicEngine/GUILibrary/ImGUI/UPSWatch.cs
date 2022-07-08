using ImGuiNET;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace VDStudios.MagicEngine.GUILibrary.ImGUI;

/// <summary>
/// An ImGUI window that shows update related metrics about the <see cref="Game"/>
/// </summary>
public sealed class UPSWatch : GUIElement
{
    private static readonly ConcurrentDictionary<long, string> tickStrings;
    private static readonly ConcurrentDictionary<double, string> msStrings;
    private static readonly ConcurrentDictionary<double, string> upsStrings;

    static UPSWatch()
    {
        tickStrings = new(5, 100);
        tickStrings[1] = "1 tick";

        msStrings = new(5, 100);
        msStrings[1] = "1ms";

        upsStrings = new(5, 100);
        upsStrings[1] = "1 update per second";
    }

    /// <inheritdoc/>
    protected override void SubmitUI(TimeSpan delta, IReadOnlyCollection<GUIElement> subElements)
    {
        var tpu = Game.AverageDelta.Ticks;
        var mspu = Game.AverageDelta.TotalMilliseconds;
        var ups = Math.Truncate(1 / Game.AverageDelta.TotalSeconds);

        ImGui.Begin("Update metrics");
        ImGui.Text(tickStrings.GetOrAdd(tpu, GenStr, " ticks"));
        ImGui.Text(msStrings.GetOrAdd(mspu, GenStr, "ms"));
        ImGui.Text(upsStrings.GetOrAdd(ups, GenStr, " updates per second"));
        ImGui.End();
    }

    private string GenStr<TInt>(TInt fps, string metric) => $"{fps:#.##}{metric}";
}
