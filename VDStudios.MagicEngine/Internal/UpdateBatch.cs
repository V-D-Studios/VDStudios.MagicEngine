using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.Internal;
internal sealed class UpdateBatchCollection
{
    private readonly Dictionary<int, Node>?[] Batches = new Dictionary<int, Node>?[(int)UpdateBatch.Last + 1];

    public Dictionary<int, Node>.ValueCollection? this[UpdateBatch batch]
    {
        get
        {
            Dictionary<int, Node>.ValueCollection? b;
            lock (Batches)
                b = Batches[(int)batch]?.Values;
            return b is null or { Count: 0 } ? null : b;
        }
    }

    public void Add(Node node)
    {
        lock (Batches)
        {
            var b = node.UpdateBatch;
            var batch = Batches[(int)b];
            if (batch is null) Batches[(int)b] = batch = new(3);
            batch.Add(node.Id, node);
        }
    }

    public void Remove(Node node)
    {
        lock (Batches)
        {
            var b = node.UpdateBatch;
            var batch = Batches[(int)b];
            if (batch is null)
                throw new InvalidOperationException($"Batch \"{b}\" is empty and cannot have any nodes removed from it. This is likely a library bug.");
            if (!batch.Remove(node.Id))
                throw new InvalidOperationException($"Could not remove node of id {node.Id} from batch \"{b}\"; is this the correct batch in which it was registered? This is likely a library bug.");
        }
    }
}
