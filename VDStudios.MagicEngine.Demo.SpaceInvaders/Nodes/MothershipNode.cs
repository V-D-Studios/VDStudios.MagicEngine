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

    public async ValueTask GenerateInvaders(int columns = 11, int rows = 5, Func<int, int, int, InvaderType, InvaderNode>? factory = null)
    {
        if (columns is not > 0)
            throw new ArgumentOutOfRangeException(nameof(columns), columns, "The amount of columns must be larger than 0");
        if (rows is not > 0)
            throw new ArgumentOutOfRangeException(nameof(rows), rows, "The amount of rows must be larger than 0");

        factory ??= defaultNodeFactory;

        var (splitThr, q) = int.DivRem(rows, 3);
        splitThr += q > 0 ? 1 : 0; // The split threshold to shift the invader type

        for (int col = 0; col < columns; col++)
        {
            var type = InvaderType.Squid; // The currently selected type
            var thr = 1; // The threshold counter, to know when to shift the invader type
            for (int row = 0; row < rows; row++)
            {
                if (thr >= splitThr)
                {
                    thr = 0;
                    type++;
                }
                else
                    thr++;

                var inv = factory(col, row, type switch
                {
                    InvaderType.Squid => 30,
                    InvaderType.Crab => 20,
                    InvaderType.Octopus => 10,
                    _ => 
                    throw new InvalidProgramException("The internal code that generates invaders overshot the invader type, this is a library bug"),
                }, type);

                await Attach(inv);
            }
        }
    }

    private static readonly Func<int, int, int, InvaderType, InvaderNode> defaultNodeFactory = (c, r, s, t) => new InvaderNode(c, r, s, t);
    public async IAsyncEnumerable<InvaderNode> GenerateAndEnumerateInvaders(int columns = 11, int rows = 5, Func<int, int, int, InvaderType, InvaderNode>? factory = null)
    {
        if (columns is not > 0)
            throw new ArgumentOutOfRangeException(nameof(columns), columns, "The amount of columns must be larger than 0");
        if (rows is not > 0)
            throw new ArgumentOutOfRangeException(nameof(rows), rows, "The amount of rows must be larger than 0");

        factory ??= defaultNodeFactory;

        var (splitThr, q) = int.DivRem(rows, 3);
        splitThr += q > 0 ? 1 : 0; // The split threshold to shift the invader type

        for (int col = 0; col < columns; col++)
        {
            var thr = 1; // The threshold counter, to know when to shift the invader type
            var type = InvaderType.Squid; // The currently selected type
            for (int row = 0; row < rows; row++)
            {
                if (thr > splitThr)
                {
                    thr = 0;
                    type++;
                }
                else
                    thr++;

                var inv = factory(col, row, type switch
                {
                    InvaderType.Squid => 30,
                    InvaderType.Crab => 20,
                    InvaderType.Octopus => 10,
                    _ => throw new InvalidProgramException("The internal code that generates invaders overshot the invader type, this is a library bug"),
                }, type);

                await Attach(inv);
                yield return inv;
            }
        }
    }
}
