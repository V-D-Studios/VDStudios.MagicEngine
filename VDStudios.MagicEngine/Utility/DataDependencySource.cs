using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace VDStudios.MagicEngine.Utility;

/// <summary>
/// Represents a source of data for an <see cref="DataDependency{T}"/> object
/// </summary>
/// <remarks>
/// This class is useful to propagate large structs into other objects, or to present mutable data throughout multiple elements without releasing control of it. This object should be stored and mutated as necessary 
/// </remarks>
public sealed class DataDependencySource<T> where T : notnull
{
    private readonly object sync = new();

    /// <summary>
    /// Instances a new object of type <see cref="DataDependencySource{T}"/>
    /// </summary>
    public DataDependencySource(T data)
    {
        InmutableData = new(this);
        _dat = data;
    }

    /// <summary>
    /// A <see cref="DataDependency{T}"/> object that reflects the data held by this object
    /// </summary>
    public DataDependency<T> InmutableData { get; }

    /// <summary>
    /// The data held by this <see cref="DataDependencySource{T}"/>, and that will be reflected on <see cref="InmutableData"/> as a read-only member upon changing
    /// </summary>
    public T Data
    {
        get => _dat;
        set
        {
            _dat = value;
            InmutableData.TriggerDataChanged();
        }
    }

    /// <summary>
    /// The data held by this <see cref="DataDependencySource{T}"/>, and that will be reflected on <see cref="InmutableData"/> as a read-only member upon changing
    /// </summary>
    /// <remarks>
    /// <see cref="ConcurrentData"/> and <see cref="DataDependency{T}.ConcurrentData"/> lock from each other, but this does not affect <see cref="DataRef"/>, <see cref="Data"/>, or <see cref="DataDependency{T}.Data"/>
    /// </remarks>
    public T ConcurrentData
    {
        get
        {
            lock (sync)
                return _dat;
        }
        set
        {
            lock (sync)
                _dat = value;
            InmutableData.TriggerDataChanged();
        }
    }

    /// <summary>
    /// A reference to the storage of the data held by this <see cref="DataDependencySource{T}"/>, and that will be reflected on <see cref="InmutableData"/> as a read-only member upon changing
    /// </summary>
    /// <remarks>
    /// Changing this value will not fire <see cref="DataDependency{T}.DataChanged"/>
    /// </remarks>
    public ref T DataRef => ref _dat;

    private T _dat;
}
