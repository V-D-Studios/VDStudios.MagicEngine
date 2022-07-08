namespace VDStudios.MagicEngine.Utility;

/// <summary>
/// Maintains a list of data and presents an arithmetic mean average of the stored data
/// </summary>
public sealed class FloatAverageKeeper
{
    private readonly float[] data;
    private readonly int Size;
    private int Fill;
    private int Index;

    /// <summary>
    /// Instantiates a new object of type <see cref="FloatAverageKeeper"/>
    /// </summary>
    /// <param name="size">The amount of data to keep an average of</param>
    public FloatAverageKeeper(int size)
    {
        if (size <= 0)
            throw new ArgumentException("size must be larger than 0", nameof(size));
        data = new float[size];
        Size = size;
    }

    private bool cacheValid;
    private float cache;
    /// <summary>
    /// The calculated average based on stored data
    /// </summary>
    public float Average
    {
        get
        {
            int fill = Fill;
            if (fill is 0)
                return 0;
            if (!cacheValid)
            {
                float dat = 0;
                int i = 0;
                for (; i < fill - 2; i += 3)
                {
                    float x = data[i];
                    float y = data[i + 1];
                    float z = data[i + 2];
                    dat += x + y + z;
                }
                for (; i < fill - 1; i += 2)
                {
                    float x = data[i];
                    float y = data[i];
                    dat += x + y;
                }
                while (i < fill)
                    dat += data[i++];
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
    public void Push(float value)
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
