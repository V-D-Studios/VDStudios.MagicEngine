namespace VDStudios.MagicEngine.Utility;

/// <summary>
/// Represents context of Transformation parameters for drawing. This class cannot be inherited or instantiated by user code, see <see cref="ReferenceDataSource{T}.InmutableData"/>
/// </summary>
/// <remarks>
/// This class provides a read-only view of data set by the <see cref="ReferenceDataSource{T}"/> that owns this object
/// </remarks>
public sealed class ReferenceData<T> where T : notnull
{
    private readonly ReferenceDataSource<T> Source;

    internal ReferenceData(ReferenceDataSource<T> source)
    {
        Source = source;
    }

    /// <summary>
    /// The data held by this <see cref="ReferenceData{T}"/>'s <see cref="ReferenceDataSource{T}"/>
    /// </summary>
    public T Data => Source.Data;

    /// <summary>
    /// The data held by this <see cref="ReferenceData{T}"/>'s <see cref="ReferenceDataSource{T}"/>
    /// </summary>
    /// <remarks>
    /// <see cref="ReferenceDataSource{T}.ConcurrentData"/> and <see cref="ConcurrentData"/> lock from each other, but this does not affect <see cref="ReferenceDataSource{T}.DataRef"/>, <see cref="ReferenceDataSource{T}.Data"/>, or <see cref="ReferenceDataSource{T}.Data"/>
    /// </remarks>
    public T ConcurrentData => Source.ConcurrentData;

    /// <summary>
    /// Fired when the <see cref="ReferenceDataSource{T}"/> that owns this object changes
    /// </summary>
    public event DataChangedEvent? DataChanged;
    internal void TriggerDataChanged() => DataChanged?.Invoke(this);

    /// <summary>
    /// Represents an event that fires when the <see cref="ReferenceDataSource{T}"/> that owns a given <see cref="ReferenceData{T}"/> changes
    /// </summary>
    /// <param name="data">The data that is now reflecting the change</param>
    public delegate void DataChangedEvent(ReferenceData<T> data);
}