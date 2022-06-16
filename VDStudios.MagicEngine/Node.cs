namespace VDStudios.MagicEngine;

public class Node : GameObject
{

    public event NodeFunctionalComponentAttachmentEvent? FunctionalComponentAttached;
    public event NodeFunctionalComponentAttachmentEvent? FunctionalComponentDetached;
}
