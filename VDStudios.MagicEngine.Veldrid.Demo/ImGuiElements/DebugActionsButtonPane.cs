using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using VDStudios.MagicEngine.Extensions.ImGuiExtension;
using VDStudios.MagicEngine.Graphics;
using VDStudios.MagicEngine.Graphics.Veldrid;

namespace VDStudios.MagicEngine.Veldrid.Demo.ImGuiElements;

public class DebugActionsButtonPane : ImGUIElement
{
    private readonly Dictionary<string, Action<VeldridGraphicsManager>> Actions = new();

    public DebugActionsButtonPane(Game game) : base(game)
    {
        var methods = typeof(DebugActions).GetMethods();
        for (int i = 0; i < methods.Length; i++)
        {
            var m = methods[i];
            if (m.ContainsGenericParameters) continue;
            if (m.ReturnType != typeof(void)) continue;
            var param = m.GetParameters();
            if (param.Length is 1 && param[0].ParameterType.IsAssignableTo(typeof(VeldridGraphicsManager)))
                Actions.Add(m.Name, m.CreateDelegate<Action<VeldridGraphicsManager>>());
        }
    }

    protected override void SubmitUI(TimeSpan delta, GraphicsManager graphicsManager)
    {
        if (ImGui.Begin("Debug Actions"))
        {
            if (Actions.Count == 0)
                ImGui.Text("No Debug Actions Registered");
            else if (graphicsManager is not VeldridGraphicsManager vgm)
                ImGui.Text("Attached to a GraphicsManager that is not a VeldridGraphicsManager");
            else
            {
                foreach (var (name, action) in Actions)
                    if (ImGui.Button(name))
                        action(vgm);
            }
        }
        ImGui.End();
    }
}
