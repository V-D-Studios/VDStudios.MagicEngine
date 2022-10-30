using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace VDStudios.MagicEngine.Demo.SpaceInvaders;

/// <summary>
/// Represents a given invader in the level
/// </summary>
public class InvaderNode : CharacterNode
{
    /// <summary>
    /// Constructs a new instance of an Invader
    /// </summary>
    /// <param name="column"></param>
    /// <param name="row"></param>
    /// <param name="scoreReturn"></param>
    /// <param name="type"></param>
    public InvaderNode(int column, int row, int scoreReturn, InvaderType type)
    {
        Column = column;
        Row = row;
        ScoreReturn = scoreReturn;
        Type = type;
    }

    public MothershipNode Mothership => mothership ?? throw new InvalidOperationException("This InvaderNode is not attached to a MothershipNode");
    public InvaderNode PriorInvader => invader ?? throw new InvalidOperationException("This InvaderNode is not attached to an InvaderNode");

    private MothershipNode? mothership;
    private InvaderNode? invader;

    public int Column { get; }

    public int Row { get; }

    public int ScoreReturn { get; }

    public InvaderType Type { get; }

    protected override bool FilterParentNode(Node parent, [NotNullWhen(false)] [MaybeNullWhen(true)] out string? reasonForDenial)
    {
        if (parent is InvaderNode inv)
        {
            invader = inv;
            mothership = inv.Mothership;
            reasonForDenial = null;
            return true;
        }

        if (parent is MothershipNode m)
        {
            mothership = m;
            reasonForDenial = null;
            return true;
        }

        reasonForDenial = "InvaderNodes can only be attached to other InvaderNodes or a MothershipNode";
        return false;
    }

    public static async IAsyncEnumerable<InvaderNode> GenerateInvaders(MothershipNode mothership, int columns = 11, int rows = 5)
    {
        ArgumentNullException.ThrowIfNull(mothership);
        if (columns is not > 0)
            throw new ArgumentOutOfRangeException(nameof(columns), columns, "The amount of columns must be larger than 0");
        if (rows is not > 0)
            throw new ArgumentOutOfRangeException(nameof(rows), rows, "The amount of rows must be larger than 0");

        var (splitThr, q) = int.DivRem(rows, 3);
        splitThr += q > 0 ? 1 : 0; // The split threshold to shift the invader type
        var thr = 1; // The threshold counter, to know when to shift the invader type
        var type = InvaderType.Squid; // The currently selected type
        InvaderNode? prevRow; // The invader node the next invader in the next row

        for (int col = 0; col < columns; col++)
        {
            prevRow = null;
            for (int row = 0; row < rows; row++)
            {
                if (thr > splitThr)
                {
                    thr = 0;
                    type++;
                }
                else
                    thr++;

                var inv = new InvaderNode(col, row, type switch
                {
                    InvaderType.Squid => 30,
                    InvaderType.Crab => 20,
                    InvaderType.Octopus => 10,
                    _ => throw new InvalidProgramException("The internal code that generates invaders overshot the invader type, this is a library bug"),
                }, type);

                await inv.Attach(prevRow ?? (Node)mothership);
                prevRow = inv;
                yield return inv;
            }
        }
    }
}
