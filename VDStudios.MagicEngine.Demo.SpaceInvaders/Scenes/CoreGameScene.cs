namespace VDStudios.MagicEngine.Demo.SpaceInvaders.Scenes;

public class CoreGameScene : Scene
{
    /// <summary>
    /// The <see cref="MothershipNode"/> that controls the invaders for this <see cref="Scene"/>
    /// </summary>
    /// <remarks>
    /// This property will throw an exception if <see cref="Scene.IsBegun"/> is <see langword="false"/>
    /// </remarks>
    public MothershipNode Mothership => _mom ?? throw new InvalidOperationException("This Scene will not have a MothershipNode until it's begun");
    private MothershipNode? _mom;

    protected override async ValueTask Beginning()
    {
        var mom = _mom = new MothershipNode();
        await new InvaderNode(0, 0, 100, InvaderType.Squid).AttachTo(mom);
        await base.Beginning();
    }

    protected override ValueTask Ending()
    {
        Mothership.Dispose();
        return base.Ending();
    }
}