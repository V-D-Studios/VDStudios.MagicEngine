namespace VDStudios.MagicEngine.SDL.Demo.Nodes;

public class InputReactorNode : Node
{
    public InputReactorNode(Game game) : base(game)
    {
        Install(new InputManagerComponent(this));
    }
}
