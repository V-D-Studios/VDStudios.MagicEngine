using System.Diagnostics.CodeAnalysis;

namespace VDStudios.MagicEngine.Demo.SpaceInvaders;

/// <summary>
/// A non-rendered node, represents the enemy faction as a whole
/// </summary>
public class MothershipNode : Node
{
    protected override bool FilterChildNode(Node child, [NotNullWhen(false)] out string? reasonForDenial)
    {
        if (child is not InvaderNode)
        {
            reasonForDenial = "Cannot attach a node that is not an invader to a Mothership node";
            return false;
        }

        reasonForDenial = null;
        return true;
    }
}
