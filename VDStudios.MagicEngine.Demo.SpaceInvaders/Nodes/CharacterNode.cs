namespace VDStudios.MagicEngine.Demo.SpaceInvaders;

public abstract class CharacterNode : Node, IDrawableNode
{
    public CharacterNode()
    {
        DrawOperationManager = new(this);
    }

    public DrawOperation CharacterSprite { get; protected init; }
    public DrawOperationManager DrawOperationManager { get; }
    public bool SkipDrawPropagation { get; }
}
