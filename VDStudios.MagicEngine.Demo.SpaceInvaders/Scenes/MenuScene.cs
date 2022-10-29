using ImGuiNET;
using Veldrid;

namespace VDStudios.MagicEngine.Demo.SpaceInvaders.Scenes;

public class MenuScene : Scene
{
    private MenuImGuiWindow? Menu;

    protected override ValueTask Beginning()
    {
        Menu = new();
        Game.MainGraphicsManager.AddElement(Menu);
        Game.MainGraphicsManager.BackgroundColor = RgbaFloat.Black;
        Menu.MainWindowOpen = true;
        Menu.WaitUntilReady();
        return base.Beginning();
    }

    protected override ValueTask Ending()
    {
        Menu?.Dispose();
        return base.Ending();
    }
}

public class MenuImGuiWindow : ImGuiElement
{
    public bool MainWindowOpen;

    protected override void SubmitUI(TimeSpan delta, IReadOnlyCollection<ImGuiElement> subElements)
    {
        ImGui.Begin("Main Menu", ref MainWindowOpen, ImGuiWindowFlags.DockNodeHost);
        if (ImGui.Button("View High Scores"))
            Game.SetScene(Program.HighScoreScene);
        if (ImGui.Button("Start Game"))
            Game.SetScene(Program.GameScene);
        ImGui.End();
    }
}