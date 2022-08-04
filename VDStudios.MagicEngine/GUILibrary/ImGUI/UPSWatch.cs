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
        tickStrings[1] = "000001 tick";

        msStrings = new(5, 100);
        msStrings[1] = "001.00 ms";

        upsStrings = new(5, 100);
        upsStrings[1] = "0001 update per second";
    }

    /// <inheritdoc/>
    protected override void SubmitUI(TimeSpan delta, IReadOnlyCollection<GUIElement> subElements)
    {
        var tpu = Game.AverageDelta.Ticks;
        var mspu = Game.AverageDelta.TotalMilliseconds;
        var ups = Math.Truncate(1 / Game.AverageDelta.TotalSeconds);

        ImGui.Begin("Update metrics");
        ImGui.Text(tickStrings.GetOrAdd(tpu, GenStrTicks));
        ImGui.Text(msStrings.GetOrAdd(mspu, GenStrMs));
        ImGui.Text(upsStrings.GetOrAdd(ups, GenStrUPS));
        ImGui.End();
    }
    private string GenStrTicks(long tpu) => tpu.ToString("000000");
    private string GenStrMs(double msu) => msu.ToString("000.00");
    private string GenStrUPS(double fps) => fps.ToString("0000");
}
