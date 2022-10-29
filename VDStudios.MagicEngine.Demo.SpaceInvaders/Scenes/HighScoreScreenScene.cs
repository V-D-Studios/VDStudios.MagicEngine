using ImGuiNET;
using VDStudios.MagicEngine.Demo.SpaceInvaders.Services;

namespace VDStudios.MagicEngine.Demo.SpaceInvaders.Scenes;

public class HighScoreScreenScene : Scene
{
    private HighScoreImGuiWindow? Menu;

    protected override ValueTask Beginning()
    {
        Menu = new(Services.GetService<HighScoreList>());
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

public class HighScoreImGuiWindow : ImGuiElement
{
    public bool MainWindowOpen;
    private readonly HighScoreList HighScores;
    private readonly List<string> Scores;

    public HighScoreImGuiWindow(HighScoreList highsc)
    {
        ArgumentNullException.ThrowIfNull(highsc);
        HighScores = highsc;
        Scores = new(highsc.Count);
        highsc.CollectionChanged += Highsc_CollectionChanged;
        foreach (var sc in highsc)
            Scores.Add($"{sc.Score} @ {sc.TopLevel}; by {sc.Name}");
    }

    private void Highsc_CollectionChanged(HighScoreList sender, Utilities.Collections.CollectionChangedEventArgs<HighScore> eventArgs)
    {
        Scores.Clear();
        foreach (var sc in sender)
            Scores.Add($"{sc.Score} @ {sc.TopLevel}; by {sc.Name}");
    }

    protected override void SubmitUI(TimeSpan delta, IReadOnlyCollection<ImGuiElement> subElements)
    {
        ImGui.Begin("High Scores", ref MainWindowOpen, ImGuiWindowFlags.DockNodeHost);
        foreach (var sc in Scores)
            ImGui.BulletText(sc);
        if (ImGui.Button("Clear"))
            HighScores.Clear();
        ImGui.End();
    }
}