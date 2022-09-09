using Newtonsoft.Json.Linq;

namespace VDStudios.MagicEngine.Utility;

/// <summary>
/// Maintains a list of data and presents an arithmetic mean average of the stored data
/// </summary>
public sealed class LongAverageKeeper
{
    private readonly long[] data;
    private readonly int Size;
    private int Fill;
    private int Index;

    /// <summary>
    /// Instantiates a new object of type <see cref="LongAverageKeeper"/>
    /// </summary>
    /// <param name="size">The amount of data to keep an average of</param>
    public LongAverageKeeper(int size)
    {
        if (size <= 0)
            throw new ArgumentException("size must be larger than 0", nameof(size));
        data = new long[size];
        Size = size;
    }

    private bool cacheValid;
    private long cache;
    /// <summary>
    /// The calculated average based on stored data
    /// </summary>
    public long Average
    {
        get
        {
            int fill = Fill;
            if (fill is 0)
                return 0;
            if (!cacheValid)
            {
                long dat = 0;
                int i = 0;
                for (; i < fill - 3; i += 4)
                {
                    long x = data[i];
                    long y = data[i + 1];
                    long z = data[i + 2];
                    long w = data[i + 3];
                    dat += x + y + z + w;
                }
                for (; i < fill - 2; i += 3)
                {
                    long x = data[i];
                    long y = data[i + 1];
                    long z = data[i + 2];
                    dat += x + y + z;
                }
                for (; i < fill - 1; i += 2)
                {
                    long x = data[i];
                    long y = data[i + 1];
                    dat += x + y;
                }
                for (; i < fill; i++)
                    dat += data[i];
                cacheValid = true;
                cache = dat / fill;
            }
            return cache;
        }
    }

    /// <summary>
    /// Pushes a new value into the data list, replacing the oldest value if the list is full
    /// </summary>
    /// <remarks>
    /// This method makes no attempt to check for modality. If you wish to keep outliers out, do so yourself
    /// </remarks>
    /// <param name="value">The value to push</param>
    public void Push(long value)
    {
        cacheValid = false;
        data[Index++] = value;
        if (Index >= Size)
            Index = 0;
        if (Fill < Size) Fill++;
    }

    /// <summary>
    /// Clears all data from the list
    /// </summary>
    public void Clear()
    {
        Fill = 0;
        Index = 0;
    }
}
