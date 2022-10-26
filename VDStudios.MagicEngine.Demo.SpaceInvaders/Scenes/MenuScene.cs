using ImGuiNET;

namespace VDStudios.MagicEngine.Demo.SpaceInvaders.Scenes;

public class MenuScene : Scene
{
    private MenuImGuiWindow? Menu;

    protected override ValueTask Beginning()
    {
        Menu = new();
        Game.MainGraphicsManager.AddElement(Menu);
        Menu.MainWindowOpen = true;
        return base.Beginning();
    }

    protected override ValueTask Ending()
    {
        Menu?.Dispose();
        return base.Ending();
    }
}

public class MenuImGuiWindow : GUIElement
{
    public bool MainWindowOpen;

    protected override void SubmitUI(TimeSpan delta, IReadOnlyCollection<GUIElement> subElements)
    {
        ImGui.Begin("Main Menu", ref MainWindowOpen);
        if (ImGui.Button("View High Scores", new(5, 5)))
            Game.SetScene(Program.HighScoreScene);
        if (ImGui.Button("Start Game", new(5, 5)))
            Game.SetScene(Program.GameScene);
        ImGui.End();
    }
}