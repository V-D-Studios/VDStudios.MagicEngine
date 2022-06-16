using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.Internal;

internal sealed class DrawQueue : IDrawQueue
{
    #region Fields

    private readonly PriorityQueue<IDrawing, float> _queue = new(20, new PriorityComparer());
    private readonly AsyncLock _lock = new();

    #endregion
    #region Comparer

    private class PriorityComparer : IComparer<float>
    {
        public int Compare(float x, float y) => x > y ? 1 : -1;
    }
    #endregion
}
