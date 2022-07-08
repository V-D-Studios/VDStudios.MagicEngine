using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.Internal;

internal sealed class LongAverageKeeper
{
    private readonly long[] data;
    private readonly int Size;
    private int Fill;
    private int Index;

    public LongAverageKeeper(int size)
    {
        if (size <= 0)
            throw new ArgumentException("size must be larger than 0", nameof(size));
        data = new long[size];
        Size = size;
    }

    private bool cacheValid;
    private long cache;
    public long Average
    {
        get
        {
            if (Fill is 0)
                return 0;
            if (!cacheValid)
            {
                int fill = Fill;
                long dat = 0;
                int i = 0;
                for (; i < fill - 2; i += 3)
                {
                    long x = data[i];
                    long y = data[i + 1];
                    long z = data[i + 2];
                    dat += x + y + z;
                }
                while (i < fill)
                    dat += data[i++];
                cacheValid = true;
                cache = dat / fill;
            }
            return cache;
        }
    }

    public void Push(long value)
    {
        cacheValid = false;
        data[Index++] = value;
        if (Index >= Size)
            Index = 0;
        if (Fill < Size) Fill++;
    }
}
