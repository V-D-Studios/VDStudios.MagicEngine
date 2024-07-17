using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Timing;

namespace VDStudios.MagicEngine.Extensions.ImGuiExtension.Elements;

/// <summary>
/// An ImGui element that keeps track of the FPS of the <see cref="GraphicsManager"/> it belongs to
/// </summary>
public class FPSWatch : ImGUIElement
{
    /// <inheritdoc/>
    public FPSWatch(Game game) : base(game) { }

    private string fpsstring = "Not Measured";
    private GraphicsManagerFrameTimer timer;

    /// <inheritdoc/>
    public override void SubmitUI(TimeSpan delta, GraphicsManager graphicsManager)
    {
        if (timer.IsDefault)
            timer = new GraphicsManagerFrameTimer(graphicsManager, 10);

        if (ImGui.Begin("FPS Watch"))
        {
            if (timer.HasClocked)
            {
                fpsstring = graphicsManager.FramesPerSecond.ToString("0.00");
                timer.Restart();
            }
            ImGui.Text(fpsstring);
        }

        ImGui.End();
    }
}
