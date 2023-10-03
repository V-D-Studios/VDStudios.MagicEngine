using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using VDStudios.MagicEngine.Demo.Common.Services;
using VDStudios.MagicEngine.Extensions.ImGuiExtension;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Graphics.Veldrid;
using VDStudios.MagicEngine.Timing;
using VDStudios.MagicEngine.Veldrid.Demo.Services;

namespace VDStudios.MagicEngine.Veldrid.Demo.ImGuiElements;
public class PipelineSwitchPane : ImGUIElement
{
    public PipelineSwitchPane(Game game) : base(game) { }

    private readonly Dictionary<DrawOperation<VeldridGraphicsContext>, (Type Category, PropertyInfo PipelineIndex, List<uint> Indices)> DopData = new();
    private GraphicsManagerFrameTimer Timer;
    private bool NoDOPM;

    protected override void SubmitUI(TimeSpan delta, GraphicsManager graphicsManager)
    {
        if (Timer.IsDefault)
            Timer = new(graphicsManager, 30);

        if (ImGui.Begin("Pipeline Switch") is false) return;

        if (graphicsManager is not VeldridGraphicsManager vgm)
            ImGui.Text("The GraphicsManager this widget is attached to is not a VeldridGraphicsManager");
        else
        {
            if (Timer.HasClocked)
            {
                DopData.Clear();
                if (Game.CurrentScene.GetDrawOperationManager<VeldridGraphicsContext>(out var dopm))
                {
                    NoDOPM = false;
                    foreach (var dop in dopm.GetDrawOperations(vgm, 0))
                    {
                        var categoryprop = dop.GetType().GetProperty("PipelineCategory");
                        var indexprop = dop.GetType().GetProperty("PipelineIndex");

                        if (categoryprop is null || indexprop is null) continue;
                        var t = (Type)categoryprop.GetValue(dop)!;

                        DopData.Add(dop, (t, indexprop, vgm.Resources.GetPipelineIndicesFor(t).ToList()));
                    }
                }
                else
                    NoDOPM = true;

                Timer.Restart();
            }

            if (NoDOPM)
                ImGui.Text("The current Game Scene does not have a DrawOperationManager for VeldridGraphicsContext");
            else
            {
                foreach (var (dop, dat) in DopData)
                {
                    if (ImGui.BeginMenu(dop.ToString()))
                    {
                        var (_, indexProp, indices) = dat;
                        var cind = (uint)indexProp.GetValue(dop)!;

                        bool state = dop.IsActive;
                        ImGui.Checkbox("Is Active", ref state);
                        dop.IsActive = state;

                        for (int i = 0; i < indices.Count; i++)
                        {
                            var index = indices[i];
                            var active = indices[i] == cind;
                            if (ImGui.RadioButton($"Pipeline index: {index}", active) && active is false)
                                indexProp.SetValue(dop, index);
                        }
                        ImGui.EndMenu();
                    }
                }
            }
        }

        ImGui.End();
    }
}
