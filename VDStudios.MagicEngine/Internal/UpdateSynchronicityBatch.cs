using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.Internal;
internal sealed class UpdateSynchronicityBatch
{
    public const int BatchCount = (int)AsynchronousTendency.AlwaysAsynchronous + 1;

    private readonly Dictionary<int, Node>?[] Batches = new Dictionary<int, Node>?[BatchCount];

    public Dictionary<int, Node>.ValueCollection? this[AsynchronousTendency tendency]
    {
        get
        {
            if (tendency is (AsynchronousTendency)(-1))
                throw new InvalidOperationException("Indexed tendency is set to -1, which means it's unset. This is likely a library bug.");

            lock (Batches)
                return Batches[(int)tendency]?.Values;
        }
    }

    public void Add(Node node, AsynchronousTendency tendency)
    {
        if (tendency is (AsynchronousTendency)(-1))
            throw new InvalidOperationException("Node tendency is set to -1, which means it's unset. This is likely a library bug.");
        lock (Batches)
        {
            var batch = Batches[(int)tendency];
            if (batch is null)
                Batches[(int)tendency] = batch = new(3);
            batch.Add(node.Id, node);
        }
    }

    public void Remove(Node node, AsynchronousTendency tendency)
    {
        if (tendency is (AsynchronousTendency)(-1))
            throw new InvalidOperationException("Node tendency is set to -1, which means it's unset. This is likely a library bug.");
        lock (Batches)
        {
            var batch = Batches[(int)tendency];
            if (batch is null)
                throw new InvalidOperationException($"Batch \"{tendency}\" is empty and cannot have any nodes removed from it. This is likely a library bug.");
            if (!batch.Remove(node.Id))
                throw new InvalidOperationException($"Could not remove node of id {node.Id} from batch \"{tendency}\"; is this the correct batch in which it was registered? This is likely a library bug.");
        }
    }
}
