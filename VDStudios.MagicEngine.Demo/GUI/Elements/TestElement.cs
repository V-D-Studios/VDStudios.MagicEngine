using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.Demo.GUI.Elements;
public class TestElement : GUIElement
{
    int clicks = 0;
    uint cid;

    protected override void SubmitUI(TimeSpan delta, IReadOnlyCollection<GUIElement> subElements)
    {
        cid = 0;
        if (Parent is null)
            ImGui.Begin("Test Window");
        ImGui.Text("This is some generic text!");
        if (ImGui.Button("Click me!"))
            clicks++;
        ImGui.Text($"The above button has been clicked {(clicks == 1 ? "1 time" : $"{clicks} times")}");

        if (subElements.Count > 0)
        {
            ImGui.TreePush();
            foreach (var el in subElements)
            {
                ImGui.TreeNode($"Sub Element #{cid++}");
                ImGui.BeginChildFrame(cid++, new(100, 30));
                Submit(delta, el);
                ImGui.EndChildFrame();
            }
            ImGui.TreePop();
        }
        if (Parent is null)
            ImGui.End();
    }
}
