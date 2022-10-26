namespace VDStudios.MagicEngine.Demo.SpaceInvaders.Scenes;

public class CoreGameScene : Scene
{
    protected override ValueTask Beginning()
    {
        Game.SetScene(Program.MainMenuScene);
        return base.Beginning();
    }
}