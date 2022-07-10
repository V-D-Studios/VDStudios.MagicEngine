namespace VDStudios.MagicEngine.Utility;

/// <summary>
/// Represents context of Transformation parameters for drawing. This class cannot be inherited or instantiated by user code, see <see cref="DataDependencySource{T}.InmutableData"/>
/// </summary>
/// <remarks>
/// This class provides a read-only view of data set by the <see cref="DataDependencySource{T}"/> that owns this object
/// </remarks>
public sealed class DataDependency<T> where T : notnull
{
    private readonly DataDependencySource<T> Source;

    internal DataDependency(DataDependencySource<T> source)
    {
        Source = source;
    }

    /// <summary>
    /// The data held by this <see cref="DataDependency{T}"/>'s <see cref="DataDependencySource{T}"/>
    /// </summary>
    public T Data => Source.Data;

    /// <summary>
    /// The data held by this <see cref="DataDependency{T}"/>'s <see cref="DataDependencySource{T}"/>
    /// </summary>
    /// <remarks>
    /// <see cref="DataDependencySource{T}.ConcurrentData"/> and <see cref="ConcurrentData"/> lock from each other, but this does not affect <see cref="DataDependencySource{T}.DataRef"/>, <see cref="DataDependencySource{T}.Data"/>, or <see cref="DataDependencySource{T}.Data"/>
    /// </remarks>
    public T ConcurrentData => Source.ConcurrentData;

    /// <summary>
    /// Fired when the <see cref="DataDependencySource{T}"/> that owns this object changes
    /// </summary>
    public event DataChangedEvent? DataChanged;
    internal void TriggerDataChanged() => DataChanged?.Invoke(this);

    /// <summary>
    /// Represents an event that fires when the <see cref="DataDependencySource{T}"/> that owns a given <see cref="DataDependency{T}"/> changes
    /// </summary>
    /// <param name="data">The data that is now reflecting the change</param>
    public delegate void DataChangedEvent(DataDependency<T> data);
}