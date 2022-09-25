using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine;
public sealed class DeferredExecutionSchedule
{
    private static readonly DeferredExecutorList Schedule = new();

    // Schedule in the Game thread
    //   In {TimeSpan} time
    //   In n updates
    //   Recurrent?
    //   Returning?

    #region Helpers

    private sealed class DeferredExecutorList
    {
        public DeferredExecutorNode First;
        public DeferredExecutorNode Last;

        public void AddLast(DeferredExecutorNode node)
        {
            lock (this)
                Last.Next = node;
        }

        public void Remove(DeferredExecutorNode node)
        {
            lock (this)
            {
                var next = node.Next;
                var prev = node.Previous;
                if (prev is not null)
                    prev.Next = next;
                if (next is not null)
                    next.Previous = prev;
            }
        }
    }

    private sealed class DeferredExecutorNode
    {
        public readonly Action? Action;
        public readonly TimeSpan Time;
        public readonly int Frames;
        public DeferredExecutorNode? Next;
        public DeferredExecutorNode? Previous;
    }

    #endregion
}
