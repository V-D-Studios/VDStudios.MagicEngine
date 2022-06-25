using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.Internal;
internal sealed class UpdateBatchCollection
{
    public const int BatchCount = (int)UpdateBatch.Last + 1;

    private readonly Dictionary<int, Node>?[] Batches = new Dictionary<int, Node>?[BatchCount];

    public Dictionary<int, Node>.ValueCollection? this[UpdateBatch batch]
    {
        get
        {
            if (batch is (UpdateBatch)(-1))
                throw new InvalidOperationException("Indexed batch is set to -1, which means it's unset. This is likely a library bug.");

            lock (Batches)
                return Batches[(int)batch]?.Values;
        }
    }

    public void Add(Node node, UpdateBatch assignation)
    {
        if (assignation is (UpdateBatch)(-1))
            throw new InvalidOperationException("Node assignation is set to -1, which means it's unset. This is likely a library bug.");
        lock (Batches)
        {
            var batch = Batches[(int)assignation];
            if (batch is null) Batches[(int)assignation] = batch = new(3);
            batch.Add(node.Id, node);
        }
    }

    public void Remove(Node node, UpdateBatch assignation)
    {
        if (assignation is (UpdateBatch)(-1))
            throw new InvalidOperationException("Node assignation is set to -1, which means it's unset. This is likely a library bug.");
        lock (Batches)
        {
            var batch = Batches[(int)assignation];
            if (batch is null)
                throw new InvalidOperationException($"Batch \"{assignation}\" is empty and cannot have any nodes removed from it. This is likely a library bug.");
            if (!batch.Remove(node.Id))
                throw new InvalidOperationException($"Could not remove node of id {node.Id} from batch \"{assignation}\"; is this the correct batch in which it was registered? This is likely a library bug.");
        }
    }
}
