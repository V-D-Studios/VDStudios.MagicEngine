using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.Internal;
internal sealed class UpdateBatchCollection
{
    public const int BatchCount = (int)UpdateBatch.Last + 1;

    private readonly UpdateSynchronicityBatch[] Batches = new UpdateSynchronicityBatch[BatchCount];

    public Dictionary<int, Node>.ValueCollection? this[UpdateBatch batch, AsynchronousTendency tendency]
    {
        get
        {
            if (batch is (UpdateBatch)(-1))
                throw new InvalidOperationException("Indexed batch is set to -1, which means it's unset. This is likely a library bug.");

            lock (Batches)
                return Batches[(int)batch] is UpdateSynchronicityBatch usb ? usb[tendency] : null;
        }
    }

    public void Add(Node node, UpdateBatch batch, AsynchronousTendency tendency)
    {
        if (batch is (UpdateBatch)(-1))
            throw new InvalidOperationException("Node assignation is set to -1, which means it's unset. This is likely a library bug.");
        lock (Batches)
        {
            var _batch = Batches[(int)batch];
            if (_batch is null) Batches[(int)tendency] = _batch = new();
            _batch.Add(node, tendency);
        }
    }

    public void Remove(Node node, UpdateBatch batch, AsynchronousTendency tendency)
    {
        if (batch is (UpdateBatch)(-1))
            throw new InvalidOperationException("Node assignation is set to -1, which means it's unset. This is likely a library bug.");
        lock (Batches)
        {
            var _batch = Batches[(int)batch];
            if (_batch is null)
                throw new InvalidOperationException($"Batch \"{batch}\" is empty and cannot have any nodes removed from it. This is likely a library bug.");
            _batch.Remove(node, tendency);
        }
    }
}
