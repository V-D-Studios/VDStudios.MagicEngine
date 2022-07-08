using ImGuiNET;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.GUILibrary.ImGUI;

/// <summary>
/// An ImGUI window that shows Frame related metrics about the <see cref="GraphicsManager"/> its currently under
/// </summary>
public sealed class FPSWatch : GUIElement
{
    private static readonly WeakReference<ConcurrentDictionary<long, string>> strings = new(CreateDict());
    private static ConcurrentDictionary<long, string> CreateDict()
    {
        ConcurrentDictionary<long, string> dict = new(1, 100);
        dict[1] = "1 frame per second";
        return dict;
    }

    private readonly ConcurrentDictionary<long, string> strs;

    /// <summary>
    /// Instances a new object of type <see cref="FPSWatch"/>
    /// </summary>
    public FPSWatch()
    {
        lock (strings)
            if (!strings.TryGetTarget(out strs!))
            {
                var d = CreateDict();
                strings.SetTarget(d);
                strs = d;
            }
    }

    /// <inheritdoc/>
    protected override void SubmitUI(TimeSpan delta, IReadOnlyCollection<GUIElement> subElements)
    {
        var fps = (long)Manager!.FramesPerSecond;
        ImGui.Begin("Frames Per Second");
        ImGui.Text(strs.GetOrAdd(fps, GenStr));
        ImGui.End();
    }

    private string GenStr(long fps) => $"{fps} frames per second";
}